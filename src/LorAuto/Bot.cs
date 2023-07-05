using LorAuto.Card.Model;
using LorAuto.Client.Model;
using LorAuto.Game;
using LorAuto.Strategies;
using LorAuto.Strategies.Model;
using Microsoft.Extensions.Logging;

namespace LorAuto;

public enum GameRotationType
{
    Standard,
    Eternal
}

internal class BotCurrentGameState
{
    public bool Mulligan { get; set; }
    public bool FirstPassBlocking { get; set; }

    public void Reset()
    {
        Mulligan = false;
        FirstPassBlocking = false;
    }
}

// TODO: Sometimes attack or block starts early than it should be, it could attack before pull new card that you get every round

/// <summary>
/// Plays the game, responsible for executing commands from <see cref="Strategy"/>
/// </summary>
public sealed class Bot
{
    /*
     NOTES:
     - Every method that effect or interact with cards must to update StateMachine before return
    */
    private readonly StateMachine _stateMachine;
    private readonly Strategy _strategy;
    private readonly GameRotationType _gameRotationType;
    private readonly bool _isPvp;
    private readonly ILogger? _logger;
    private readonly BotCurrentGameState _currentGameState;
    private readonly UserSimulator _userSimulator;

    public Bot(StateMachine stateMachine, Strategy strategy, GameRotationType gameRotationType, bool isPvp, ILogger? logger)
    {
        _stateMachine = stateMachine;
        _strategy = strategy;
        _gameRotationType = gameRotationType;
        _isPvp = isPvp;
        _logger = logger;

        _currentGameState = new BotCurrentGameState();
        _userSimulator = new UserSimulator(_stateMachine);
    }

    private void Mulligan()
    {
        if (_currentGameState.Mulligan)
        {
            Thread.Sleep(1500);
            return;
        }
        
        _currentGameState.Mulligan = true;

        // Wait before do any action
        Thread.Sleep(Random.Shared.Next(1000, 3000));

        IEnumerable<InGameCard> cardsToReplace = _strategy.Mulligan(_stateMachine.CardsOnBoard.CardsMulligan);
        foreach (InGameCard card in cardsToReplace)
        {
            if (!_stateMachine.CardsOnBoard.CardsMulligan.Contains(card))
                continue;

            _userSimulator.ClickCard(card);

            Thread.Sleep(Random.Shared.Next(300, 600));
        }

        _userSimulator.CommitOrPassOrSkipTurn();
    }

    private async Task BlockAsync(CancellationToken ct = default)
    {
        // Double check to avoid False Positives (card draw animation, card play animation...)
        if (!_currentGameState.FirstPassBlocking)
        {
            _currentGameState.FirstPassBlocking = true;
            _logger?.LogInformation("First blocking pass...");
            await Task.Delay(6000, ct).ConfigureAwait(false);
            return;
        }

        Dictionary<InGameCard, IEnumerable<InGameCard>?>? spellsToUse;
        IEnumerable<InGameCard>? abilitiesToUse;
        Dictionary<InGameCard, InGameCard> blockCards = _strategy.Block(_stateMachine.CardsOnBoard, out spellsToUse, out abilitiesToUse);
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
            
            await Task.Delay(Random.Shared.Next(800, 1000), ct).ConfigureAwait(false);
        }

        _userSimulator.CommitOrPassOrSkipTurn();
        await Task.Delay(10000, ct).ConfigureAwait(false);
        
        // Update cards rectangles as it changed after move card to block
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
    }

    private async Task<bool> PlayCardsFromHandAsync(List<InGameCard> playableCards, CancellationToken ct = default)
    {
        // Play cards from hand
        if (playableCards.Count == 0)
        {
            // Update cards
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
            return false;
        }
        
        // Update cards
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);

        (InGameCard HandCard, List<InGameCard?>? Targets)? playHandCard = _strategy.PlayHandCard(
            _stateMachine.CardsOnBoard,
            _stateMachine.GameState,
            _stateMachine.Mana,
            _stateMachine.SpellMana,
            playableCards);
        
        if (playHandCard is null)
            return false;

        // TODO: use playHandCard.Targets
        _userSimulator.PlayCard(playHandCard.Value.HandCard);

        await Task.Delay(4000, ct).ConfigureAwait(false);
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
        
        return true;
    }

    private async Task<bool> PreDefendOrAttackAsync(EGameState gameState, CancellationToken ct = default)
    {
        // TODO: Add spell counter to strategy, as this condition is just skip opponent spells 
        if (_stateMachine.CardsOnBoard.SpellStack.Count != 0 && _stateMachine.CardsOnBoard.SpellStack.All(card => card.Type is GameCardType.Spell or GameCardType.Ability))
        {
            // Double check to avoid False Positives
            if (!_currentGameState.FirstPassBlocking)
            {
                _currentGameState.FirstPassBlocking = true;
                _logger?.LogInformation("First blocking pass...");
                await Task.Delay(6000, ct).ConfigureAwait(false);
                
                // Update cards
                await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
                return true;
            }

            _userSimulator.CommitOrPassOrSkipTurn();
            await Task.Delay(_stateMachine.CardsOnBoard.SpellStack.Count * 2500, ct).ConfigureAwait(false);
            
            // Update cards
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
            return true;
        }

        // Update game data
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
        
        // Determinate what to do with attack token
        EGamePlayAction gamePlayAction = gameState == EGameState.DefendTurn
            ? _strategy.RespondToOpponentAction(_stateMachine.CardsOnBoard, _stateMachine.GameState, _stateMachine.Mana, _stateMachine.SpellMana)
            : _strategy.AttackTokenUsage(_stateMachine.CardsOnBoard, _stateMachine.Mana, _stateMachine.SpellMana);
        if (gamePlayAction == EGamePlayAction.Skip)
        {
            _userSimulator.CommitOrPassOrSkipTurn();
            await Task.Delay(4000, ct).ConfigureAwait(false);
            
            // Update cards
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
            return true;
        }

        List<InGameCard> playableCards = _stateMachine.CardsOnBoard.CardsHand
            .Where(card => card.Cost <= _stateMachine.Mana || (card.Type == GameCardType.Spell && card.Cost <= _stateMachine.Mana + _stateMachine.SpellMana))
            .OrderByDescending(card => card.Cost)
            .ToList();

        // Play cards from hand
        if (playableCards.Count <= 0 || gamePlayAction != EGamePlayAction.PlayCards)
            return false;
        
        bool thereCardPlayed = await PlayCardsFromHandAsync(playableCards, ct).ConfigureAwait(false);
        if (!thereCardPlayed)
            return false;

        await Task.Delay(4000, ct).ConfigureAwait(false);
        
        // Update cards
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
        
        return true;
    }

    private async Task DefendAsync(CancellationToken ct = default)
    {
        bool actionHandled = await PreDefendOrAttackAsync(EGameState.DefendTurn, ct).ConfigureAwait(false);
        if (actionHandled)
            return;
        
        // Any other 'gamePlayAction' or there is no playable cards
        _userSimulator.CommitOrPassOrSkipTurn();

        await Task.Delay(4000, ct).ConfigureAwait(false);
    }

    private async Task AttackAsync(CancellationToken ct = default)
    {
        bool actionHandled = await PreDefendOrAttackAsync(EGameState.Attacking, ct).ConfigureAwait(false);
        if (actionHandled)
            return;

        // Attack
        List<InGameCard> cardsToAttack = _strategy.Attack(_stateMachine.CardsOnBoard, _stateMachine.CardsOnBoard.CardsBoard);
        foreach (InGameCard atkCard in cardsToAttack)
        {
            _userSimulator.PlayCard(atkCard);

            await Task.Delay(Random.Shared.Next(800, 1250), ct).ConfigureAwait(false);
            
            // Update cards
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
        }

        // Wait for any card effect
        await Task.Delay(1000, ct).ConfigureAwait(false);

        // Submit attack
        _userSimulator.CommitOrPassOrSkipTurn();
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
                break;
            
            case EGameState.Menus:
                _userSimulator.SelectDeck(_gameRotationType, _isPvp);
                // TODO: Add a way to detect 'EGameState.SearchGame' so this statement doesnt get called more than once
                break;
            
            case EGameState.MenusDeckSelected:
                _userSimulator.SelectDeck(_gameRotationType, _isPvp);
                break;

            case EGameState.SearchGame:
                break;

            case EGameState.UserInteractNotReady:
                await Task.Delay(3000, ct).ConfigureAwait(false);
                break;
            
            case EGameState.Mulligan:
                Mulligan();
                break;

            case EGameState.OpponentTurn:
                await Task.Delay(3000, ct).ConfigureAwait(false);
                break;

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

            case EGameState.RoundEnd:
                break;

            case EGameState.Pass:
                break;

            case EGameState.End:
                _currentGameState.Reset();
                _userSimulator.GameEndContinueAndReplay();
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }

        Thread.Sleep(1000);
        
        // Move mouse to center after each play 
        _userSimulator.ResetMousePosition();
    }
}
