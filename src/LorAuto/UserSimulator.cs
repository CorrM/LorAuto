using System.Diagnostics;
using System.Drawing;
using GregsStack.InputSimulatorStandard;
using GregsStack.InputSimulatorStandard.Native;
using LorAuto.Bot.Model;
using LorAuto.Card;
using LorAuto.Card.Model;
using LorAuto.Client;
using LorAuto.Client.Model;
using LorAuto.Extensions;
using PInvoke;

namespace LorAuto;

/// <summary>
/// Represents a user simulator for simulating user interactions in the game.
/// </summary>
internal sealed class UserSimulator
{
    private readonly StateMachine _stateMachine;
    private readonly InputSimulator _input;
    private readonly (double, double)[] _selectDeckAi;
    private readonly (double, double)[] _selectDeckPvp;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSimulator"/> class.
    /// </summary>
    /// <param name="stateMachine">The state machine used for game state tracking.</param>
    public UserSimulator(StateMachine stateMachine)
    {
        _stateMachine = stateMachine;
        _input = new InputSimulator();

        _selectDeckAi = new (double, double)[] { (0.04721, 0.33454), (0.15738, 0.33401), (0, 0), (0.33180, 0.30779), (0.83213, 0.89538) };
        _selectDeckPvp = new (double, double)[] { (0.04721, 0.33454), (0.15738, 0.25), (0, 0), (0.33180, 0.30779), (0.83213, 0.89538) };
    }

    /// <summary>
    /// Sets the game window to the foreground if the game is not already in the foreground.
    /// </summary>
    private void ForegroundIfGameNot()
    {
        _stateMachine.UpdateClientInfo();

        if (_stateMachine.GameIsForeground)
            return;

        User32.SetForegroundWindow(_stateMachine.GameWindowHandle);
    }

    /// <summary>
    /// Simulates the selection of a deck based on the game rotation and PvP mode.
    /// </summary>
    /// <param name="gameRotation">The game rotation.</param>
    /// <param name="isPvp">Specifies whether it is PvP mode.</param>
    public void SelectDeck(EGameRotation gameRotation, bool isPvp)
    {
        ForegroundIfGameNot();

        (double, double) gameTypePos = gameRotation switch
        {
            EGameRotation.Standard => (0.70989, 0.05),
            EGameRotation.Eternal => (0.81970, 0.05),
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

    /// <summary>
    /// Simulates a click on a card in the game.
    /// </summary>
    /// <param name="card">The card to click.</param>
    public void ClickCard(InGameCard card)
    {
        ForegroundIfGameNot();

        int cx = _stateMachine.WindowLocation.X + card.TopCenterPos.X;
        int cy = _stateMachine.WindowLocation.Y + card.TopCenterPos.Y;

        _input.Mouse.MoveMouseSmooth(cx, cy)
            .LeftButtonClick();
    }

    /// <summary>
    /// Simulates clicking on the nexus in the game.
    /// </summary>
    /// <param name="opponentNexus">Specifies whether the opponent's nexus should be clicked.</param>
    public void ClickNexus(bool opponentNexus)
    {
        ForegroundIfGameNot();

        (Point player, Point opponent) = _stateMachine.ComponentLocator.GetNexusPossition();
        Point target = opponentNexus ? opponent : player;

        int cx = _stateMachine.WindowLocation.X + target.X;
        int cy = _stateMachine.WindowLocation.Y + target.Y;

        _input.Mouse.MoveMouseSmooth(cx, cy)
            .LeftButtonClick();
    }

    /// <summary>
    /// Simulates playing a card from the hand in the game.
    /// </summary>
    /// <param name="handCard">The card to play from the hand.</param>
    /// <param name="target">The target for the card's effect, if applicable.</param>
    public void PlayCardFromHand(InGameCard handCard, CardTargetSelector? target)
    {
        ForegroundIfGameNot();

        int x = _stateMachine.WindowLocation.X + handCard.TopCenterPos.X;
        int y = _stateMachine.WindowLocation.Y + handCard.TopCenterPos.Y;

        _input.Mouse.MoveMouseSmooth(x, y)
            .Sleep(40)
            .LeftButtonDown();

        int newY = y - 3 * _stateMachine.WindowSize.Height / 7;
        _input.Mouse.MoveMouseSmooth(x, newY)
            .Sleep(40)
            .LeftButtonUp();

        Thread.Sleep(500); // Wait for the card maximize animation

        if (target is not null && target.GetSelectedCard() == handCard)
        {
            List<(ECardTarget, InGameCard?)> targets = target.GetTargets();
            foreach ((ECardTarget targetType, InGameCard? effectTarget) in targets)
            {
                switch (targetType)
                {
                    case ECardTarget.Card:
                    case ECardTarget.HandCard:
                        if (effectTarget is null)
                            throw new InvalidOperationException("Selector that targeting a card should have an 'effectTarget'.");

                        ClickCard(effectTarget);
                        break;

                    case ECardTarget.Nexus:
                        ClickNexus(false);
                        break;

                    case ECardTarget.OpponentNexus:
                        ClickNexus(true);
                        break;

                    default:
                        throw new UnreachableException();
                }
            }
        }

        if (handCard.Type != EGameCardType.Spell)
            return;

        Thread.Sleep(1000);

        _input.Keyboard.KeyPress(VirtualKeyCode.SPACE);
    }

    /// <summary>
    /// Moves a board card to the field on the game board.
    /// </summary>
    /// <param name="boardCard">The board card to move to the field.</param>
    public void MoveBoardCardToField(InGameCard boardCard)
    {
        ForegroundIfGameNot();

        int x = _stateMachine.WindowLocation.X + boardCard.TopCenterPos.X;
        int y = _stateMachine.WindowLocation.Y + boardCard.TopCenterPos.Y;

        _input.Mouse.MoveMouseSmooth(x, y)
            .Sleep(40)
            .LeftButtonDown();

        int newY = y - 3 * _stateMachine.WindowSize.Height / 7;
        _input.Mouse.MoveMouseSmooth(x, newY)
            .Sleep(40)
            .LeftButtonUp();
    }

    /// <summary>
    /// Simulates blocking a card with another card in the game.
    /// </summary>
    /// <param name="card">The card to block.</param>
    /// <param name="opponentBlocked">The opponent's card to block with.</param>
    public void BlockCard(InGameCard card, InGameCard opponentBlocked)
    {
        (int, int) posSrc = (_stateMachine.WindowLocation.X + card.TopCenterPos.X, _stateMachine.WindowLocation.Y + card.TopCenterPos.Y);
        (int, int) posDest = (_stateMachine.WindowLocation.X + opponentBlocked.TopCenterPos.X, _stateMachine.WindowLocation.Y + opponentBlocked.TopCenterPos.Y);

        ForegroundIfGameNot();

        _input.Mouse.MoveMouseSmooth(posSrc.Item1, posSrc.Item2)
            .LeftButtonDown()
            .MoveMouseSmooth(posDest.Item1, posDest.Item2)
            .LeftButtonUp();
    }

    /// <summary>
    /// Simulates committing a turn, passing a turn, or skipping a turn in the game.
    /// </summary>
    public void CommitOrPassOrSkipTurn()
    {
        ForegroundIfGameNot();

        Thread.Sleep(Random.Shared.Next(400, 600));

        _input.Keyboard.KeyPress(VirtualKeyCode.SPACE);
    }

    /// <summary>
    /// Simulates continuing and replaying the game after it ends.
    /// </summary>
    public void GameEndContinueAndReplay()
    {
        double continueBtnPosX = _stateMachine.WindowLocation.X + (_stateMachine.WindowSize.Width * 0.66);
        double continueBtnPosY = _stateMachine.WindowLocation.Y + (_stateMachine.WindowSize.Height * 0.90);

        for (int i = 0; i < 16; i++)
        {
            ForegroundIfGameNot();

            _input.Mouse.MoveMouseSmooth(continueBtnPosX, continueBtnPosY)
                .LeftButtonClick();

            Thread.Sleep(1000);

            // Don't move this code form here, as the current game state will just be 'EGameState.Menus'
            _stateMachine.UpdateGameDataAsync().GetAwaiter().GetResult();
            if (_stateMachine.GameState is EGameState.MenusDeckSelected)
                break;
        }
    }

    /// <summary>
    /// Resets the mouse position to a default position.
    /// </summary>
    public void ResetMousePosition()
    {
        double mouseX = _stateMachine.WindowLocation.X + (_stateMachine.WindowSize.Width * 0.1041);
        double mouseY = _stateMachine.WindowLocation.Y + (_stateMachine.WindowSize.Height * 0.7592);

        _input.Mouse.MoveMouseSmooth(mouseX, mouseY);
    }
}
