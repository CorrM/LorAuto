using System.Diagnostics;
using LorAuto.Bot.Model;
using LorAuto.Card.Model;
using LorAuto.Client;
using LorAuto.Client.Model;
using LorAuto.Strategies;
using LorAuto.Strategies.Model;
using Microsoft.Extensions.Logging;

namespace LorAuto.Bot;

// TODO: Get raid of all sleep, replace it with image processing

/// <summary>
/// Plays the game, responsible for executing commands from <see cref="Strategy"/>
/// </summary>
public sealed class LorBot
{
    /*
     NOTES:
     - Every method that effect or interact with cards must to update StateMachine before return
    */
    private readonly StateMachine _stateMachine;
    private readonly Strategy _strategy;
    private readonly EGameRotation _gameRotation;
    private readonly bool _isPvp;
    private readonly ILogger? _logger;
    private readonly UserSimulator _userSimulator;

    public LorBot(StateMachine stateMachine, Strategy strategy, EGameRotation gameRotation, bool isPvp, ILogger? logger)
    {
        _stateMachine = stateMachine;
        _strategy = strategy;
        _gameRotation = gameRotation;
        _isPvp = isPvp;
        _logger = logger;

        _userSimulator = new UserSimulator(_stateMachine);
    }

    private async Task MulliganAsync(CancellationToken ct = default)
    {
        // Wait before do any action
        while (_stateMachine.CardsOnBoard.CardsMulligan.Count != 4)
        {
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
            Thread.Sleep(1500);
        }

        IEnumerable<InGameCard> cardsToReplace = _strategy.Mulligan(_stateMachine.CardsOnBoard.CardsMulligan);
        foreach (InGameCard card in cardsToReplace)
        {
            if (!_stateMachine.CardsOnBoard.CardsMulligan.Contains(card))
                continue;

            _userSimulator.ClickCard(card);
            Thread.Sleep(Random.Shared.Next(300, 500));
        }

        _userSimulator.CommitOrPassOrSkipTurn();
        _userSimulator.ResetMousePosition();

        // Wait mulligan
        for (int i = 0; _stateMachine.CardsOnBoard.CardsMulligan.Count != 0 || i >= 10; ++i)
        {
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
            await Task.Delay(1000, ct).ConfigureAwait(false);
        }
    }

    private async Task BlockAsync(CancellationToken ct = default)
    {
        Dictionary<InGameCard, IEnumerable<InGameCard>?>? spellsToUse;
        IEnumerable<InGameCard>? abilitiesToUse;
        Dictionary<InGameCard, InGameCard> blockCards = _strategy.Block(_stateMachine.CardsOnBoard, out spellsToUse, out abilitiesToUse);

        // TODO: Use `spellsToUse` and `abilitiesToUse`
        foreach ((InGameCard? myCard, InGameCard? opponentCard) in blockCards)
        {
            // Update before block
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);

            // Check if card is already blocked
            bool isBlockable = true;
            foreach (InGameCard allyCard in _stateMachine.CardsOnBoard.CardsAttackOrBlock)
            {
                if (Math.Abs(allyCard.TopCenterPos.X - opponentCard.TopCenterPos.X) >= 10)
                    continue;

                isBlockable = false;
                break;
            }

            if (!isBlockable)
                continue;

            _userSimulator.BlockCard(myCard, opponentCard);

            await Task.Delay(Random.Shared.Next(400, 600), ct).ConfigureAwait(false);
        }

        _userSimulator.CommitOrPassOrSkipTurn();
        _userSimulator.ResetMousePosition();

        await Task.Delay(10000, ct).ConfigureAwait(false);

        // Update cards rectangles as it changed after move card to block
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
    }

    private async Task<bool> PlayCardFromHandAsync(List<InGameCard> playableCards, CancellationToken ct = default)
    {
        // Update cards
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);

        // Play cards from hand
        if (playableCards.Count == 0)
            return false;

        (InGameCard HandCard, List<InGameCard?>? Targets)? playHandCard = _strategy.PlayHandCard(
            _stateMachine.CardsOnBoard,
            _stateMachine.GameState,
            _stateMachine.Mana,
            _stateMachine.SpellMana,
            playableCards);

        if (playHandCard is null)
            return false;

        // TODO: use playHandCard.Targets
        _userSimulator.PlayCardFromHand(playHandCard.Value.HandCard /*, playHandCard.Value.Targets*/);

        _userSimulator.ResetMousePosition();

        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
        return true;
    }

    private async Task<bool> PreDefendOrAttackAsync(EGameState gameState, CancellationToken ct = default)
    {
        // Update cards
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);

        // TODO: Add spell counter to strategy, as this condition is just skip opponent spells 
        if (_stateMachine.CardsOnBoard.SpellStack.Count != 0 && _stateMachine.CardsOnBoard.SpellStack.All(card => card.Type is EGameCardType.Spell or EGameCardType.Ability))
        {
            _userSimulator.CommitOrPassOrSkipTurn();
            await Task.Delay(_stateMachine.CardsOnBoard.SpellStack.Count * 2500, ct).ConfigureAwait(false);

            return true;
        }

        // Determinate what to do
        EGamePlayAction gamePlayAction = gameState == EGameState.DefendTurn
            ? _strategy.RespondToOpponentAction(_stateMachine.CardsOnBoard, _stateMachine.GameState, _stateMachine.Mana, _stateMachine.SpellMana)
            : _strategy.AttackTokenUsage(_stateMachine.CardsOnBoard, _stateMachine.Mana, _stateMachine.SpellMana);
        if (gamePlayAction == EGamePlayAction.Skip)
        {
            _userSimulator.CommitOrPassOrSkipTurn();

            // Update cards
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
            return true;
        }

        List<InGameCard> playableCards = _stateMachine.CardsOnBoard.CardsHand
            .Where(card => card.Cost <= _stateMachine.Mana || (card.Type == EGameCardType.Spell && card.Cost <= _stateMachine.Mana + _stateMachine.SpellMana))
            .OrderByDescending(card => card.Cost)
            .ToList();

        // Play card from hand
        if (playableCards.Count <= 0 || gamePlayAction != EGamePlayAction.PlayCards)
            return false;

        bool thereCardPlayed = await PlayCardFromHandAsync(playableCards, ct).ConfigureAwait(false);
        if (!thereCardPlayed)
            return false;

        // Update cards
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);

        return true;
    }

    private async Task DefendAsync(CancellationToken ct = default)
    {
        bool actionHandled = await PreDefendOrAttackAsync(EGameState.DefendTurn, ct).ConfigureAwait(false);
        if (actionHandled)
        {
            _userSimulator.ResetMousePosition();
            return;
        }

        // Any other 'gamePlayAction' or there is no playable cards
        _userSimulator.CommitOrPassOrSkipTurn();

        _userSimulator.ResetMousePosition();
        await Task.Delay(4000, ct).ConfigureAwait(false);
    }

    private async Task AttackAsync(CancellationToken ct = default)
    {
        bool actionHandled = await PreDefendOrAttackAsync(EGameState.Attacking, ct).ConfigureAwait(false);
        if (actionHandled)
        {
            _userSimulator.ResetMousePosition();
            return;
        }

        // Attack
        List<InGameCard> cardsToAttack = _strategy.Attack(_stateMachine.CardsOnBoard, _stateMachine.CardsOnBoard.CardsBoard);
        foreach (InGameCard atkCard in cardsToAttack)
        {
            _userSimulator.PlayBoardCard(atkCard);

            await Task.Delay(Random.Shared.Next(800, 1250), ct).ConfigureAwait(false);

            // Update cards
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
        }

        // Wait for any card effect
        await Task.Delay(1000, ct).ConfigureAwait(false);

        // Submit attack
        _userSimulator.CommitOrPassOrSkipTurn();
        _userSimulator.ResetMousePosition();

        await Task.Delay(4000, ct).ConfigureAwait(false);

        // Update cards
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
    }

    public async Task ProcessAsync(CancellationToken ct = default)
    {
        // TODO: When attack then opponent set block cards then you have spell cards
        //       That will count as '_stateMachine.GameState' == 'EGameState.Blocking'
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);

        _logger?.LogInformation("Current game state: {GameState}", _stateMachine.GameState);

        switch (_stateMachine.GameState)
        {
            case EGameState.None:
                return;

            case EGameState.Hold:
                await Task.Delay(3000, ct).ConfigureAwait(false);
                return;

            case EGameState.Menus:
                _userSimulator.SelectDeck(_gameRotation, _isPvp);
                // TODO: Add a way to detect 'EGameState.SearchGame' so this statement doesnt get called more than once
                break;

            case EGameState.MenusDeckSelected:
                _userSimulator.SelectDeck(_gameRotation, _isPvp);
                break;

            case EGameState.SearchGame:
                break;

            case EGameState.UserInteractNotReady:
                await Task.Delay(3000, ct).ConfigureAwait(false);
                return;

            case EGameState.Mulligan:
                await MulliganAsync().ConfigureAwait(false);
                break;

            case EGameState.OpponentTurn:
                await Task.Delay(3000, ct).ConfigureAwait(false);
                return;

            case EGameState.DefendTurn:
                await DefendAsync(ct).ConfigureAwait(false);
                break;

            case EGameState.AttackTurn:
                await AttackAsync(ct).ConfigureAwait(false);
                break;

            case EGameState.Attacking:
                break;

            case EGameState.Blocking:
                await BlockAsync(ct).ConfigureAwait(false);
                break;

            case EGameState.End:
                _userSimulator.GameEndContinueAndReplay();
                break;

            default:
                throw new UnreachableException();
        }

        // Move mouse to center after each play 
        _userSimulator.ResetMousePosition();
    }
}
