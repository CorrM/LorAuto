using System.Drawing;

namespace LorAuto.Game;

public sealed class GameComponentLocator
{
    private readonly StateMachine _stateMachine;

    public GameComponentLocator(StateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public (Point Left, Point Right) GetAttackTokenBound()
    {
        int attackTokenBoundLx = (int)Math.Ceiling(_stateMachine.WindowSize.Width * 0.80f);
        int attackTokenBoundLy = (int)Math.Ceiling(_stateMachine.WindowSize.Height * 0.6f);
        int attackTokenBoundRx = ((int)Math.Ceiling(_stateMachine.WindowSize.Width * 0.9f)) - attackTokenBoundLx;
        int attackTokenBoundRy = ((int)Math.Ceiling(_stateMachine.WindowSize.Height * 0.78f)) - attackTokenBoundLy;
        
        return (new Point(attackTokenBoundLx, attackTokenBoundLy), new Point(attackTokenBoundRx, attackTokenBoundRy));
    }

    public Rectangle GetManaRect()
    {
        // This numbers are critical
        int x = (int)Math.Ceiling(_stateMachine.WindowSize.Width * 0.8255f); // 1585
        int y = (int)Math.Ceiling(_stateMachine.WindowSize.Height * 0.5907f); // 638
        const int w = 50; // TODO: Should be ratio
        const int h = 37; // TODO: Should be ratio

        return new Rectangle(x, y, w, h);
    }
}
