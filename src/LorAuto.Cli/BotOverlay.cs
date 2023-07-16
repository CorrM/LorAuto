using GameOverlay.Drawing;
using GameOverlay.Windows;
using LorAuto.Card.Model;
using LorAuto.Client;
using LorAuto.Client.Model;
using Rectangle = System.Drawing.Rectangle;

namespace LorAuto.Cli;

public sealed class BotOverlay : IDisposable
{
    private readonly StateMachine _stateMachine;
    private readonly GraphicsWindow _window;
    private readonly Graphics _windowGfx;
    private readonly Dictionary<string, SolidBrush> _brushes;
    private readonly Dictionary<string, Font> _fonts;
    private readonly Dictionary<string, Image> _images;

    public BotOverlay(StateMachine stateMachine)
    {
        _stateMachine = stateMachine;
        _brushes = new Dictionary<string, SolidBrush>();
        _fonts = new Dictionary<string, Font>();
        _images = new Dictionary<string, Image>();

        _windowGfx = new Graphics()
        {
            MeasureFPS = true,
            PerPrimitiveAntiAliasing = true,
            TextAntiAliasing = true
        };

        _window = new StickyWindow(stateMachine.GameWindowHandle, _windowGfx)
        {
            FPS = 60,
            IsTopmost = true,
            IsVisible = true,
            BypassTopmost = true
        };

        //_window.SetupGraphics += Window_SetupGraphics;
        _window.DrawGraphics += Window_DrawGraphics;
        //_window.DestroyGraphics += Window_DestroyGraphics;
    }

    private void DrawSpellMana(SolidBrush gBrush)
    {
        Rectangle[] spellManaRect = _stateMachine.ComponentLocator.GetSpellManaRect();
        foreach (Rectangle sRect in spellManaRect)
            _windowGfx.DrawRectangle(gBrush, sRect.ToGRect(), 1.0f);
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
            2.0f);

        (Rectangle power, Rectangle health) = _stateMachine.ComponentLocator.GetCardAttackAndHealthRect(card);
        _windowGfx.DrawRectangle(bBrush, power.ToGRect(), 1.0f);
        _windowGfx.DrawRectangle(bBrush, health.ToGRect(), 1.0f);
    }

    private void Window_DrawGraphics(object? sender, DrawGraphicsEventArgs e)
    {
        _windowGfx.ClearScene();

        using SolidBrush gBrush = _windowGfx.CreateSolidBrush(0, 255, 0);

        if (_stateMachine.GameState is not (EGameState.Menus or EGameState.MenusDeckSelected or EGameState.End))
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

            Rectangle roundsLogRect = _stateMachine.ComponentLocator.GetRoundsLogRect();
            _windowGfx.DrawRectangle(gBrush, roundsLogRect.ToGRect(), 1.0f);
        }
        else
        {
            Rectangle roundsLogRect = _stateMachine.ComponentLocator.GetMenusEditDeckButtonRect();
            _windowGfx.DrawRectangle(gBrush, roundsLogRect.ToGRect(), 1.0f);
        }
    }

    public void Start()
    {
        _window.Create();
    }

    public void Wait()
    {
        _window.Join();
    }

    public void Dispose()
    {
        _window.Dispose();
        _windowGfx.Dispose();
    }
}
