using GregsStack.InputSimulatorStandard;

namespace LorAuto.Extensions;

public static class MouseSimulator_Extensions
{
    public static IMouseSimulator MoveMouseSmooth(this IMouseSimulator mouse, double absoluteX, double absoluteY, int smoothFactor = 40, int sleepDurationMs = 10)
    {
        float EaseInOutQuad(float t) => t < 0.5f ? 2 * t * t : 1 - (float)Math.Pow(-2 * t + 2, 2) / 2;

        int x0 = mouse.Position.X;
        int y0 = mouse.Position.Y;
        int dx = (int)absoluteX - x0;
        int dy = (int)absoluteY - y0;

        for (int i = 0; i < smoothFactor; i++)
        {
            float t = EaseInOutQuad((float)i / smoothFactor);
            mouse.MoveMouseTo((int)Math.Ceiling(x0 + dx * t), (int)Math.Ceiling(y0 + dy * t));
            Thread.Sleep(sleepDurationMs);
        }
        
        return mouse;
    }
}
