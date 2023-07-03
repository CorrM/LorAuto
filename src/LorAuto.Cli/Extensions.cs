namespace LorAuto.Cli;

public static class Extensions
{
    public static GameOverlay.Drawing.Rectangle ToGRect(this System.Drawing.Rectangle rect)
    {
        return new GameOverlay.Drawing.Rectangle(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
    }
}
