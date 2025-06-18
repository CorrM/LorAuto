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

namespace LorAuto;

/// <summary>
/// Represents a user simulator for simulating user interactions in the game.
/// </summary>
internal sealed class UserSimulator
{
    private readonly GameWindow _gameWindow;
    private readonly InputSimulator _input;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSimulator"/> class.
    /// </summary>
    /// <param name="gameWindow">The game window.</param>
    public UserSimulator(GameWindow gameWindow)
    {
        _gameWindow = gameWindow;
        _input = new InputSimulator();
    }

    /// <summary>
    /// Sets the game window to the foreground if the game is not already in the foreground.
    /// </summary>
    private void ForegroundIfGameNot()
    {
        _gameWindow.UpdateClientInfo();
        if (_gameWindow.GameIsForeground)
            return;

        _gameWindow.SetGameForeground();
    }

    /// <summary>
    /// Simulates the selection of a deck based on the game rotation and PvP mode.
    /// </summary>
    /// <param name="gameRotation">The game rotation.</param>
    /// <param name="isPvp">Specifies whether it is PvP mode.</param>
    public void SelectGameRotation(GameRotation gameRotation, bool isPvp)
    {
        ForegroundIfGameNot();

        // I use playMenuItem multiple times so if there is any UI open it's still clicking it
        (double xRatio, double yRatio) playMenuItem = (0.04721, 0.33454);

        Span<(double xRatio, double yRatio)> selectGameAiPosRatio =
            stackalloc (double, double)[] { playMenuItem, playMenuItem, playMenuItem, (0.15738, 0.434) };
        Span<(double xRatio, double yRatio)> selectGamePvpPosRatio =
            stackalloc (double, double)[] { playMenuItem, playMenuItem, playMenuItem, (0.15738, 0.358), (0, 0) };

        foreach ((double xRatio, double yRatio) in isPvp ? selectGamePvpPosRatio : selectGameAiPosRatio)
        {
            double xr;
            double yr;

            if (xRatio == 0 && yRatio == 0 && isPvp)
            {
                (xr, yr) = gameRotation switch
                {
                    GameRotation.Standard => (0.70989, 0.05),
                    GameRotation.Eternal => (0.81970, 0.05),
                    _ => throw new UnreachableException(),
                };
            }
            else if (xRatio == 0 && yRatio == 0)
            {
                // vs AI there is no game rotation
                continue;
            }
            else
            {
                xr = xRatio;
                yr = yRatio;
            }

            (double x, double y) = (_gameWindow.WindowLocation.X + (xr * _gameWindow.WindowSize.Width),
                _gameWindow.WindowLocation.Y + (yr * _gameWindow.WindowSize.Height));
            _input.Mouse.MoveMouseSmooth(x, y).LeftButtonClick().Sleep(Random.Shared.Next(700, 1000));
        }
    }

    /// <summary>
    /// Simulates the selection of a deck based on the game rotation and PvP mode.
    /// </summary>
    /// <param name="deckIndex">The index of the deck to select.</param>
    public void SelectDeck(int deckIndex)
    {
        ForegroundIfGameNot();

        // TODO: Use deckIndex to select deck
        Span<(double xRatio, double yRatio)> selectDeckPosRatio = [(0.33180, 0.30779), (0.83213, 0.89538)];
        foreach ((double xRatio, double yRatio) in selectDeckPosRatio)
        {
            (double x, double y) = (_gameWindow.WindowLocation.X + (xRatio * _gameWindow.WindowSize.Width),
                _gameWindow.WindowLocation.Y + (yRatio * _gameWindow.WindowSize.Height));
            _input.Mouse.MoveMouseSmooth(x, y).LeftButtonClick().Sleep(Random.Shared.Next(700, 1000));
        }

        // Handle "Matchmaking has failed" error
        Thread.Sleep(5000);
        (double, double) okButtonPos = (_gameWindow.WindowLocation.X + (0.5 * _gameWindow.WindowSize.Width),
            _gameWindow.WindowLocation.Y + (0.546 * _gameWindow.WindowSize.Height));
        _input.Mouse.MoveMouseSmooth((int)okButtonPos.Item1, (int)okButtonPos.Item2).LeftButtonClick();
    }

    /// <summary>
    /// Simulates a click on a card in the game.
    /// </summary>
    /// <param name="card">The card to click.</param>
    public void ClickCard(InGameCard card)
    {
        ForegroundIfGameNot();

        int cx = _gameWindow.WindowLocation.X + card.TopCenterPos.X;
        int cy = _gameWindow.WindowLocation.Y + card.TopCenterPos.Y;

        _input.Mouse.MoveMouseSmooth(cx, cy).LeftButtonClick();
    }

    /// <summary>
    /// Simulates clicking on the nexus in the game.
    /// </summary>
    /// <param name="opponentNexus">Specifies whether the opponent's nexus should be clicked.</param>
    public void ClickNexus(bool opponentNexus)
    {
        ForegroundIfGameNot();

        (Point player, Point opponent) = _gameWindow.ComponentLocator.GetNexusPosition();
        Point target = opponentNexus ? opponent : player;

        int cx = _gameWindow.WindowLocation.X + target.X;
        int cy = _gameWindow.WindowLocation.Y + target.Y;

        _input.Mouse.MoveMouseSmooth(cx, cy).LeftButtonClick();
    }

    /// <summary>
    /// Simulates playing a card from the hand in the game.
    /// </summary>
    /// <param name="handCard">The card to play from the hand.</param>
    /// <param name="target">The target for the card's effect, if applicable.</param>
    public void PlayCardFromHand(InGameCard handCard, CardTargetSelector? target)
    {
        ForegroundIfGameNot();

        int x = _gameWindow.WindowLocation.X + handCard.TopCenterPos.X;
        int y = _gameWindow.WindowLocation.Y + handCard.TopCenterPos.Y;

        _input.Mouse.MoveMouseSmooth(x, y).Sleep(40).LeftButtonDown();

        int newY = y - 3 * _gameWindow.WindowSize.Height / 7;
        _input.Mouse.MoveMouseSmooth(x, newY).Sleep(40).LeftButtonUp();

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
                            throw new InvalidOperationException(
                                "Selector that targeting a card should have an 'effectTarget'."
                            );

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

        int x = _gameWindow.WindowLocation.X + boardCard.TopCenterPos.X;
        int y = _gameWindow.WindowLocation.Y + boardCard.TopCenterPos.Y;

        _input.Mouse.MoveMouseSmooth(x, y).Sleep(40).LeftButtonDown();

        int newY = y - 3 * _gameWindow.WindowSize.Height / 7;
        _input.Mouse.MoveMouseSmooth(x, newY).Sleep(40).LeftButtonUp();
    }

    /// <summary>
    /// Simulates blocking a card with another card in the game.
    /// </summary>
    /// <param name="card">The card to block.</param>
    /// <param name="opponentBlocked">The opponent's card to block with.</param>
    public void BlockCard(InGameCard card, InGameCard opponentBlocked)
    {
        (int, int) posSrc = (_gameWindow.WindowLocation.X + card.TopCenterPos.X,
            _gameWindow.WindowLocation.Y + card.TopCenterPos.Y);
        (int, int) posDest = (_gameWindow.WindowLocation.X + opponentBlocked.TopCenterPos.X,
            _gameWindow.WindowLocation.Y + opponentBlocked.TopCenterPos.Y);

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
    public void GameEndContinueAndReplay(StateMachine stateMachine)
    {
        ForegroundIfGameNot();

        double continueBtnPosX = _gameWindow.WindowLocation.X + (_gameWindow.WindowSize.Width * 0.700);
        double continueBtnPosY = _gameWindow.WindowLocation.Y + (_gameWindow.WindowSize.Height * 0.915);

        for (int i = 0; i < 16; i++)
        {
            _input.Mouse.MoveMouseSmooth(continueBtnPosX, continueBtnPosY).LeftButtonClick();

            Thread.Sleep(1000);

            // Update game status. otherwise game status will be 'Menus'.
            stateMachine.UpdateGameDataAsync().GetAwaiter().GetResult();
            if (stateMachine.BoardDate.GameState is GameState.MenusDeckSelected)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Resets the mouse position to a default position.
    /// </summary>
    public void ResetMousePosition()
    {
        double mouseX = _gameWindow.WindowLocation.X + (_gameWindow.WindowSize.Width * 0.1041);
        double mouseY = _gameWindow.WindowLocation.Y + (_gameWindow.WindowSize.Height * 0.7592);

        _input.Mouse.MoveMouseSmooth(mouseX, mouseY);
    }
}
