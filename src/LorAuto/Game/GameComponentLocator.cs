using System.Drawing;

namespace LorAuto.Game;

public sealed class GameComponentLocator
{
    private readonly StateMachine _stateMachine;

    public GameComponentLocator(StateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }
    
    public Rectangle GetManaRect()
    {
        // This numbers are critical, any pixel to left or right, 'GetMana' function will not work
        int x = (int)Math.Ceiling(_stateMachine.WindowSize.Width * 0.8255f); // 1585
        int y = (int)Math.Ceiling(_stateMachine.WindowSize.Height * 0.5907f); // 638
        const int w = 50; // TODO: Should be ratio
        const int h = 37; // TODO: Should be ratio

        return new Rectangle(x, y, w, h);
    }

    public Rectangle[] GetSpellManaRect()
    {
        int mW = (int)Math.Ceiling(_stateMachine.WindowSize.Width * 0.00625f);
        int mH = (int)Math.Ceiling(_stateMachine.WindowSize.Height * 0.0111f);

        var ret = new Rectangle[]
        {
            new((int)Math.Ceiling(_stateMachine.WindowSize.Width * 0.8739f), (int)Math.Ceiling(_stateMachine.WindowSize.Height * 0.6314f), mW, mH),
            new((int)Math.Ceiling(_stateMachine.WindowSize.Width * 0.8854f), (int)Math.Ceiling(_stateMachine.WindowSize.Height * 0.6259f), mW, mH),
            new((int)Math.Ceiling(_stateMachine.WindowSize.Width * 0.8958f), (int)Math.Ceiling(_stateMachine.WindowSize.Height * 0.6149f), mW, mH)
        };
        
        return ret;
    }

    public Rectangle GetTurnButtonRect()
    {
        int x = (int)Math.Ceiling(_stateMachine.WindowSize.Width * 0.77);
        int y = (int)Math.Ceiling(_stateMachine.WindowSize.Height * 0.42);
        int w = (int)Math.Ceiling(_stateMachine.WindowSize.Width * 0.93) - x;
        int h = (int)Math.Ceiling(_stateMachine.WindowSize.Height * 0.58) - y;

        return new Rectangle(x, y, w, h);
    }
    
    public Rectangle GetAttackTokenRect()
    {
        int x = (int)Math.Ceiling(_stateMachine.WindowSize.Width * 0.80f);
        int y = (int)Math.Ceiling(_stateMachine.WindowSize.Height * 0.6f);
        int w = (int)Math.Ceiling(_stateMachine.WindowSize.Width * 0.1f);
        int h = (int)Math.Ceiling(_stateMachine.WindowSize.Height * 0.1814f);
        
        return new Rectangle(x, y, w, h);
    }

    public Rectangle GetRoundsLogRect()
    {
        int x = (int)Math.Ceiling(_stateMachine.WindowSize.Width * 0.0156f);
        int y = (int)Math.Ceiling(_stateMachine.WindowSize.Height * 0.4752f);
        int w = (int)Math.Ceiling(_stateMachine.WindowSize.Width * 0.0333f);
        int h = (int)Math.Ceiling(_stateMachine.WindowSize.Height * 0.0555f);
        
        return new Rectangle(x, y, w, h);
    }

    public Rectangle GetMenusEditDeckButtonRect()
    {
        int x = (int)Math.Ceiling(_stateMachine.WindowSize.Width * 0.9447);
        int y = (int)Math.Ceiling(_stateMachine.WindowSize.Height * 0.5518);
        int w = (int)Math.Ceiling(_stateMachine.WindowSize.Width * 0.0353f);
        int h = (int)Math.Ceiling(_stateMachine.WindowSize.Height * 0.0585f);

        return new Rectangle(x, y, w, h);
    }
}
