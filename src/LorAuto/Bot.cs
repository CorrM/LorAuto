using System.Diagnostics;
using LorAuto.Card.Model;
using LorAuto.Client.Model;
using LorAuto.Game;
using LorAuto.Strategies;
using LorAuto.Strategies.Model;

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

/// <summary>
/// Plays the game, responsible for executing commands from <see cref="Strategy"/>
/// </summary>
public sealed class Bot
{
    private readonly StateMachine _stateMachine;
    private readonly Strategy _strategy;
    private readonly GameRotationType _gameRotationType;
    private readonly bool _isPvp;
    private readonly BotCurrentGameState _currentGameState;
    private readonly UserSimulator _userSimulator;

    public Bot(StateMachine stateMachine, Strategy strategy, GameRotationType gameRotationType, bool isPvp)
    {
        _stateMachine = stateMachine;
        _strategy = strategy;
        _gameRotationType = gameRotationType;
        _isPvp = isPvp;

        _currentGameState = new BotCurrentGameState();
        _userSimulator = new UserSimulator(_stateMachine);
    }

    private void Mulligan()
    {
        if (_currentGameState.Mulligan)
            return;
        _currentGameState.Mulligan = true;

        // Wait before do any action
        Thread.Sleep(Random.Shared.Next(6000, 10000));

        BoardCards? boardState = _stateMachine.CardsOnBoard;
        if (boardState is null)
            throw new UnreachableException();

        IEnumerable<InGameCard> cardsToReplace = _strategy.Mulligan(boardState.CardsMulligan);
        foreach (InGameCard card in cardsToReplace)
        {
            if (!boardState.CardsMulligan.Contains(card))
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
            Console.WriteLine("first blocking pass...");
            await Task.Delay(6000, ct).ConfigureAwait(false);
            return;
        }

        Dictionary<InGameCard, IEnumerable<InGameCard>?>? spellsToUse;
        IEnumerable<InGameCard>? abilitiesToUse;
        Dictionary<InGameCard, InGameCard> blockCards = _strategy.Block(_stateMachine.CardsOnBoard, out spellsToUse, out abilitiesToUse);

        // TODO: blockCards will be not valid after just one card change, because other cards date are now old as 'blockCards' cards
        //       Are totally different than cards in '_stateMachine.CardsOnBoard' so mostly mouse cord will be wrong
        
        foreach ((InGameCard? myCard, InGameCard? opponentCard) in blockCards)
        {
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

            // Update state machine as cards rectangles will change after move
            // card to block
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);

            await Task.Delay(Random.Shared.Next(300, 600), ct).ConfigureAwait(false);
        }

        _userSimulator.CommitOrPassOrSkipTurn();
        await Task.Delay(10000, ct).ConfigureAwait(false);
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
        
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false); // Update cards positions

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

        //// its owner refills 1 spell mana
        //if (handCard.Keywords.Contains(GameCardKeyword.Attune))
        //    this.spellMana = Math.Min(3, this.spellMana + 1);

        //// Calculate spell mana if necessary
        //if (handCard.Type == GameCardType.Spell)
        //    this.spellMana = Math.Max(0, this.spellMana - handCard.Cost);

        //// Get new mana
        //await Task.Delay(1250, ct).ConfigureAwait(false);
        //while (true)
        //{
        //    await _stateMachine.UpdateAsync(ct).ConfigureAwait(false);

        //    if (_stateMachine.Mana != -1)
        //        break;
        //}

        //this.prevMana = this.mana;

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
                Console.WriteLine("first spell pass...");
                Thread.Sleep(6000);
                return true;
            }

            _userSimulator.CommitOrPassOrSkipTurn();
            await Task.Delay(4000, ct).ConfigureAwait(false);
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
            return true;
        }

        List<InGameCard> playableCards = _stateMachine.CardsOnBoard.CardsHand
            .Where(card => card.Cost <= _stateMachine.Mana || (card.Type == GameCardType.Spell && card.Cost <= _stateMachine.Mana + _stateMachine.SpellMana))
            .OrderByDescending(card => card.Cost)
            .ToList();

        // Play cards from hand
        if (playableCards.Count <= 0 || gamePlayAction != EGamePlayAction.PlayCards)
            return true;
        
        bool thereCardPlayed = await PlayCardsFromHandAsync(playableCards, ct).ConfigureAwait(false);
        if (!thereCardPlayed)
            return true;

        await Task.Delay(4000, ct).ConfigureAwait(false);
        
        // Update cards
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
        
        return false;
    }

    private async Task DefendAsync(CancellationToken ct = default)
    {
        bool actionHandled = await PreDefendOrAttackAsync(EGameState.DefendTurn, ct).ConfigureAwait(false);
        if (actionHandled)
            return;
        
        // Any other 'gamePlayAction' or there is no playable cards
        _userSimulator.CommitOrPassOrSkipTurn();

        await Task.Delay(Random.Shared.Next(1000, 4000), ct).ConfigureAwait(false);
    }

    private async Task AttackAsync(CancellationToken ct = default)
    {
        bool actionHandled = await PreDefendOrAttackAsync(EGameState.Attacking, ct).ConfigureAwait(false);
        if (actionHandled)
            return;

        // Attack
        List<InGameCard> cardsToAttack = _strategy.Attack(_stateMachine.CardsOnBoard, _stateMachine.CardsOnBoard.CardsBoard);
        // TODO: cardsToAttack will be not valid after just one card change, because other cards date are now old as 'cardsToAttack' cards
        //       Are totally different than cards in '_stateMachine.CardsOnBoard' so mostly mouse cord will be wrong
        
        foreach (InGameCard atkCard in cardsToAttack)
        {
            _userSimulator.PlayCard(atkCard);

            // Update cards
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);

            await Task.Delay(Random.Shared.Next(800, 1250), ct).ConfigureAwait(false);
        }

        // Wait for any card effect
        await Task.Delay(1000, ct).ConfigureAwait(false);

        // Submit attack
        _userSimulator.CommitOrPassOrSkipTurn();
        await Task.Delay(4000, ct).ConfigureAwait(false);
    }

    public async Task ProcessAsync(CancellationToken ct = default)
    {
        // TODO: When attack then opponent set the block cards then you have spell cards
        //       That will make '_stateMachine.GameState' ==  'EGameState.Blocking'
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);

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

            case EGameState.SearchGame:
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

        // Move mouse to center after each play 
        //int mouseX = _stateMachine.WindowLocation.X + (_stateMachine.WindowSize.Width / 2);
        //int mouseY = _stateMachine.WindowLocation.Y + (_stateMachine.WindowSize.Height / 2);
        //_input.Mouse.MoveMouseSmooth(mouseX, mouseY);
    }
}
