using System.Diagnostics;
using GregsStack.InputSimulatorStandard;
using GregsStack.InputSimulatorStandard.Native;
using LorAuto.Card;
using LorAuto.Client.Model;
using LorAuto.Extensions;
using LorAuto.Game;
using LorAuto.Strategies;

namespace LorAuto;

public enum GameStyleType
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
    private readonly GameStyleType _gameStyleType;
    private readonly bool _isPvp;
    private readonly InputSimulator _input;

    private readonly BotCurrentGameState _currentGameState;
    private readonly (double, double)[] _selectDeckAi;
    private readonly (double, double)[] _selectDeckPvp;
    
    public Bot(StateMachine stateMachine, Strategy strategy, GameStyleType gameStyleType, bool isPvp)
    {
        _stateMachine = stateMachine;
        _strategy = strategy;
        _gameStyleType = gameStyleType;
        _isPvp = isPvp;
        _input = new InputSimulator();

        _currentGameState = new BotCurrentGameState();
        _selectDeckAi = new (double, double)[] { (0.04721, 0.33454), (0.15738, 0.33401), (0, 0), (0.33180, 0.30779), (0.83213, 0.89538) };
        _selectDeckPvp = new (double, double)[] { (0.04721, 0.33454), (0.15738, 0.25), (0, 0), (0.33180, 0.30779), (0.83213, 0.89538) };
    }
    
    private void PlayCard(InGameCard card)
    {
        int x = _stateMachine.WindowLocation.X + card.TopCenterPos.X;
        int y = _stateMachine.WindowLocation.Y + _stateMachine.WindowSize.Height - card.TopCenterPos.Y;
    
        _input.Mouse.MoveMouseSmooth(x, y);
        Thread.Sleep(500); // Wait for the card maximize animation
    
        _input.Mouse.MoveMouseSmooth(x, y)
            .LeftButtonDown();
    
        int newY = y - 3 * _stateMachine.WindowSize.Height / 7;
        _input.Mouse.MoveMouseSmooth(x, newY);
        Thread.Sleep(300);
    
        _input.Mouse.MoveMouseSmooth(x, newY)
            .LeftButtonUp();
        
        Thread.Sleep(300);
        if (card.Type != GameCardType.Spell)
            return;
        
        Thread.Sleep(1000);
        _input.Keyboard.KeyPress(VirtualKeyCode.SPACE);
    }

    private void BlockCard(InGameCard card, InGameCard cardToBeBlocked)
    {
        (int, int) posSrc = (_stateMachine.WindowLocation.X + card.TopCenterPos.X, _stateMachine.WindowLocation.Y + _stateMachine.WindowSize.Height - card.TopCenterPos.Y);
        (int, int) posDest = (_stateMachine.WindowLocation.X + cardToBeBlocked.TopCenterPos.X, _stateMachine.WindowLocation.Y + _stateMachine.WindowSize.Height - cardToBeBlocked.TopCenterPos.Y);

        _input.Mouse.MoveMouseSmooth(posSrc.Item1, posSrc.Item2)
            .LeftButtonDown()
            .MoveMouseSmooth(posDest.Item1, posDest.Item2)
            .LeftButtonUp();
    }
    
    private void SelectDeck(GameStyleType gameStyleType, bool isPvp)
    {
        (double, double) gameTypePos = gameStyleType switch
        {
            GameStyleType.Standard => (0.70989, 0.05),
            GameStyleType.Eternal => (0.81970, 0.05),
            _ => throw new UnreachableException()
        };
        
        foreach ((double xRatio, double yRatio) in isPvp ? _selectDeckPvp : _selectDeckAi)
        {
            double xr;
            double yr;
            
            if (xRatio == 0 && yRatio == 0 && isPvp)
            {
                xr = gameTypePos.Item1;
                yr = gameTypePos.Item2;
            }
            else if (xRatio == 0 && yRatio == 0)
            {
                // vs AI there is not Standard or Eternal
                continue;
            }
            else
            {
                xr = xRatio;
                yr = yRatio;
            }
            
            (double x, double y) = (_stateMachine.WindowLocation.X + (xr * _stateMachine.WindowSize.Width), _stateMachine.WindowLocation.Y + (yr * _stateMachine.WindowSize.Height));
            _input.Mouse.MoveMouseSmooth(x, y)
                .LeftButtonClick()
                .Sleep(Random.Shared.Next(700, 1000));
        }
        
        Thread.Sleep(1000);
        
        // Handle "Matchmaking has failed" error
        (double, double) okButtonPos = (_stateMachine.WindowLocation.X + 0.5 * _stateMachine.WindowSize.Width, _stateMachine.WindowLocation.Y + 0.546 * _stateMachine.WindowSize.Height);
        _input.Mouse.MoveMouseSmooth((int)okButtonPos.Item1, (int)okButtonPos.Item2)
            .LeftButtonClick();
    }
    
    private void Mulligan()
    {
        if (_currentGameState.Mulligan)
            return;
        _currentGameState.Mulligan = true;

        // Wait before do any action
        Thread.Sleep(Random.Shared.Next(6000, 10000));
        
        BoardState? boardState = _stateMachine.CardsOnBoard;
        if (boardState is null)
            throw new UnreachableException();

        IEnumerable<InGameCard> cardsToReplace = _strategy.Mulligan(boardState.CardsMulligan);
        foreach (InGameCard card in cardsToReplace)
        {
            if (!boardState.CardsMulligan.Contains(card))
                continue;

            int cx = _stateMachine.WindowLocation.X + card.TopCenterPos.X;
            int cy = _stateMachine.WindowLocation.Y + _stateMachine.WindowSize.Height - card.TopCenterPos.Y;

            _input.Mouse.MoveMouseSmooth(cx, cy)
                .LeftButtonClick();
            
            Thread.Sleep(Random.Shared.Next(300, 600));
        }

        _input.Keyboard.KeyPress(VirtualKeyCode.SPACE);

    }

    private async Task BlockAsync(CancellationToken ct = default)
    {
        // Double check to avoid False Positives (card draw animation, card play animation...)
        if (!_currentGameState.FirstPassBlocking)
        {
            _currentGameState.FirstPassBlocking = true;
            Console.WriteLine("first blocking pass...");
            await Task.Delay(5000, ct).ConfigureAwait(false);
            return;
        }

        BoardState? boardState = _stateMachine.CardsOnBoard;
        if (boardState is null)
            throw new UnreachableException();

        Dictionary<InGameCard, IEnumerable<InGameCard>?>? spellsToUse;
        IEnumerable<InGameCard>? abilitiesToUse;
        Dictionary<InGameCard,InGameCard> inGameCards = _strategy.Block(boardState, out spellsToUse, out abilitiesToUse);
        
        foreach ((InGameCard? myCard, InGameCard? opponentCard) in inGameCards)
        {
            if (opponentCard.Keywords.Contains(GameCardKeyword.Elusive) && !myCard.Keywords.Contains(GameCardKeyword.Elusive))
                continue;

            if (opponentCard.Keywords.Contains(GameCardKeyword.Fearsome) && myCard.Attack < 3)
                continue;
            
            if (myCard.Keywords.Contains(GameCardKeyword.CantBlock))
                continue;
        
            // TODO: boardState.CardsAttackOrBlock will always be empty
            //       Because statue machine thread have no time to update
            //       board state
            
            // Check if card is already blocked
            bool isBlockable = true;
            foreach (InGameCard allyCard in boardState.CardsAttackOrBlock)
            {
                if (Math.Abs(allyCard.TopCenterPos.X - opponentCard.TopCenterPos.X) >= 10)
                    continue;
                
                isBlockable = false;
                break;
            }

            if (!isBlockable)
                continue;
            
            BlockCard(myCard, opponentCard);
            
            // Update state machine as cards rectangles will change after move
            // card to block
            await _stateMachine.UpdateAsync(ct).ConfigureAwait(false);

            await Task.Delay(Random.Shared.Next(300, 600), ct).ConfigureAwait(false);

        }

        _input.Keyboard.KeyPress(VirtualKeyCode.SPACE);
        await Task.Delay(10000, ct).ConfigureAwait(false);
    }
    
    private void GameEndContinueAndReplay()
    {
        Thread.Sleep(4000);
        double continueBtnPosX = _stateMachine.WindowLocation.X + (_stateMachine.WindowSize.Width * 0.66);
        double continueBtnPosY = _stateMachine.WindowLocation.Y + (_stateMachine.WindowSize.Height * 0.90);
        
        for (int i = 0; i < 16; i++)
        {
            _input.Mouse.MoveMouseSmooth(continueBtnPosX, continueBtnPosY)
                .LeftButtonClick();
            Thread.Sleep(1500);
        }
        
        Thread.Sleep(1000);
    }
    
    public async Task ProcessAsync(CancellationToken ct = default)
    {
        await _stateMachine.UpdateAsync(ct).ConfigureAwait(false);
        
        switch (_stateMachine.GameState)
        {
            case EGameState.None:
                return;
            
            case EGameState.Hold:
                break;
            
            case EGameState.Menus:
                SelectDeck(_gameStyleType, _isPvp);
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
                break;
            
            case EGameState.AttackTurn:
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
                GameEndContinueAndReplay();
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
