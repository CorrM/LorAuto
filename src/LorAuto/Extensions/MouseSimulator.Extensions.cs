using GregsStack.InputSimulatorStandard;

namespace LorAuto.Extensions;

/// <summary>
/// Provides extension methods for simulating mouse actions using the <see cref="IMouseSimulator"/> interface.
/// </summary>
public static class MouseSimulatorExtensions
{
    /// <summary>
    /// Moves the mouse smoothly from the current position to the specified absolute coordinates.
    /// </summary>
    /// <param name="mouse">The mouse simulator instance.</param>
    /// <param name="absoluteX">The absolute X-coordinate to move the mouse to.</param>
    /// <param name="absoluteY">The absolute Y-coordinate to move the mouse to.</param>
    /// <param name="smoothFactor">The smooth factor controlling the smoothness of the movement. Default is 40.</param>
    /// <param name="sleepDurationMs">The sleep duration in milliseconds between each step of the movement. Default is 10.</param>
    /// <returns>The mouse simulator instance.</returns>
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
