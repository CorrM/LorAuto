using GameOverlay.Drawing;
using GameOverlay.Windows;
using LorAuto.Card.Model;
using LorAuto.Client.Model;
using Rectangle = System.Drawing.Rectangle;

namespace LorAuto.Client;

internal sealed class DebugOverlay : IDisposable
{
    private readonly GameWindow _gameWindow;
    private readonly StateMachine _stateMachine;
    private readonly Graphics _windowGfx;
    private StickyWindow? _window;

    public DebugOverlay(GameWindow gameWindow, StateMachine stateMachine)
    {
        _gameWindow = gameWindow;
        _stateMachine = stateMachine;

        _windowGfx = new Graphics()
        {
            MeasureFPS = true,
            PerPrimitiveAntiAliasing = true,
            TextAntiAliasing = true,
        };

        _gameWindow.UpdateClient += (_, window) =>
        {
            if (_window?.ParentWindowHandle == window.GameWindowHandle)
            {
                return;
            }

            _window?.Dispose();
            _window = new StickyWindow(window.GameWindowHandle, _windowGfx)
            {
                FPS = 8, // 8 is more than enough
                IsTopmost = true,
                IsVisible = true,
                BypassTopmost = true,
            };

            //_window.SetupGraphics += Window_SetupGraphics;
            _window.DrawGraphics += Window_DrawGraphics;
            //_window.DestroyGraphics += Window_DestroyGraphics;

            _window.Create();
        };
    }

    private static GameOverlay.Drawing.Rectangle ToGRect(Rectangle rect)
    {
        return new GameOverlay.Drawing.Rectangle(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
    }

    private void DrawSpellMana(SolidBrush gBrush)
    {
        Rectangle[] spellManaRect = _gameWindow.ComponentLocator.GetSpellManaRect();
        foreach (Rectangle sRect in spellManaRect)
            _windowGfx.DrawRectangle(gBrush, ToGRect(sRect), 1.0f);
    }

    private void DrawCard(SolidBrush gBrush, InGameCard card)
    {
        if (card.Type is EGameCardType.Spell or EGameCardType.Ability)
            return;

        using SolidBrush rBrush = _windowGfx.CreateSolidBrush(255, 0, 0);
        using SolidBrush bBrush = _windowGfx.CreateSolidBrush(0, 0, 255);

        int x = card.Position.X;
        int y = card.Position.Y;

        _windowGfx.DrawRectangle(
            gBrush,
            x,
            y,
            x + card.Size.Width,
            y + card.Size.Height,
            2.0f
        );

        (Rectangle power, Rectangle health) = _gameWindow.ComponentLocator.GetCardAttackAndHealthRect(card);
        _windowGfx.DrawRectangle(bBrush, ToGRect(power), 1.0f);
        _windowGfx.DrawRectangle(bBrush, ToGRect(health), 1.0f);
    }

    private void Window_DrawGraphics(object? sender, DrawGraphicsEventArgs e)
    {
        _windowGfx.ClearScene();

        using SolidBrush gBrush = _windowGfx.CreateSolidBrush(0, 255, 0);

        if (_stateMachine.BoardDate.GameState is not (GameState.Menus or GameState.MenusDeckSelected or GameState.End))
        {
            DrawSpellMana(gBrush);

            for (int i = 0; i < _stateMachine.BoardDate.Cards.AllCards.Count; i++)
            {
                try
                {
                    InGameCard card = _stateMachine.BoardDate.Cards.AllCards[i];
                    DrawCard(gBrush, card);
                }
                catch
                {
                    // ignored
                }
            }

            Rectangle roundsLogRect = _gameWindow.ComponentLocator.GetRoundsLogRect();
            _windowGfx.DrawRectangle(gBrush, ToGRect(roundsLogRect), 1.0f);
        }
        else
        {
            Rectangle roundsLogRect = _gameWindow.ComponentLocator.GetMenusEditDeckButtonRect();
            _windowGfx.DrawRectangle(gBrush, ToGRect(roundsLogRect), 1.0f);
        }
    }

    public void Dispose()
    {
        _window?.Dispose();
        _windowGfx.Dispose();
    }
}
