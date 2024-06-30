using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using PInvoke;

namespace LorAuto.Client;

internal class GameWindow
{
    /// <summary>
    /// Gets the handle of the game window.
    /// </summary>
    public nint GameWindowHandle { get; private set; }

    /// <summary>
    /// Gets the location of the game window.
    /// </summary>
    public Point WindowLocation { get; private set; }

    /// <summary>
    /// Gets the size of the game window.
    /// </summary>
    public Size WindowSize { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the game is in the foreground.
    /// </summary>
    public bool GameIsForeground { get; private set; }

    /// <summary>
    /// Gets the component locator used for locating various game components.
    /// </summary>
    public GameComponentLocator ComponentLocator { get; }

    public event EventHandler<GameWindow>? UpdateClient;

    public GameWindow()
    {
        ComponentLocator = new GameComponentLocator();
        //User32.SetProcessDPIAware();
    }

    /// <summary>
    /// Gets the handle of the game window.
    /// </summary>
    /// <returns>The handle of the game window.</returns>
    private nint GetWindowHandle()
    {
        nint targetHandler = nint.Zero;
        User32.EnumWindows(
            (handle, _) =>
            {
                int windowTextLength = User32.GetWindowTextLength(handle) + 1;
                Span<char> windowName = stackalloc char[windowTextLength];

                User32.GetWindowText(handle, windowName);
                windowName = windowName[..windowName.IndexOf('\0')];

                if (!MemoryExtensions.Equals(windowName, "Legends of Runeterra", StringComparison.Ordinal))
                    return true;

                targetHandler = handle;
                return false;
            },
            nint.Zero
        );

        return targetHandler;
    }

    /// <summary>
    /// Gets the location and size of the game window.
    /// </summary>
    /// <returns>A tuple containing the window location and size.</returns>
    private (Point Position, Size Size) GetWindowRectInfo()
    {
        if (GameWindowHandle == nint.Zero)
            return (new Point(), new Size());

        if (!User32.GetWindowRect(GameWindowHandle, out RECT targetRect))
            return (new Point(), new Size());

        var loc = new Point(targetRect.left, targetRect.top);
        var size = new Size(targetRect.right - targetRect.left, targetRect.bottom - targetRect.top);

        return (loc, size);
    }

    /// <summary>
    /// Checks if the game is in the foreground.
    /// </summary>
    /// <returns><c>true</c> if the game is in the foreground; otherwise, <c>false</c>.</returns>
    private bool GetGameIsForeground()
    {
        nint hWindow = User32.GetForegroundWindow();
        return hWindow == GameWindowHandle;
    }

    /// <summary>
    /// Sets the game window to be the foreground window.
    /// </summary>
    public void SetGameForeground()
    {
        User32.SetForegroundWindow(GameWindowHandle);
    }

    /// <summary>
    /// Updates the client information.
    /// </summary>
    public void UpdateClientInfo()
    {
        GameWindowHandle = GetWindowHandle();
        if (GameWindowHandle is 0 or -1)
        {
            (WindowLocation, WindowSize) = (new Point(), new Size());
            GameIsForeground = false;
            return;
        }

        (WindowLocation, WindowSize) = GetWindowRectInfo();
        GameIsForeground = GetGameIsForeground();

        ComponentLocator.UpdateWindowSize(WindowSize);
        UpdateClient?.Invoke(this, this);
    }

    /// <summary>
    /// Gets the frames of the game window.
    /// </summary>
    /// <param name="framesCount">The number of frames to capture.</param>
    /// <param name="delay">The delay between frames in milliseconds.</param>
    /// <returns>An array of BGR images representing the frames.</returns>
    public Image<Bgr, byte>[] GetFrames(int framesCount, int delay)
    {
        (Point loc, Size size) = GetWindowRectInfo();
        var frames = new Image<Bgr, byte>[framesCount];

        using User32.SafeDCHandle hdcScreen = User32.GetDC(nint.Zero);
        using User32.SafeDCHandle hdc = Gdi32.CreateCompatibleDC(hdcScreen);

        nint hBitmap = Gdi32.CreateCompatibleBitmap(hdcScreen, size.Width, size.Height);
        Gdi32.SelectObject(hdc, hBitmap);

        for (int i = 0; i < framesCount; i++)
        {
            Gdi32.BitBlt(
                hdc,
                0,
                0,
                size.Width,
                size.Height,
                hdcScreen,
                loc.X,
                loc.Y,
                0xCC0020
            );

            using Bitmap bitmap = Image.FromHbitmap(hBitmap);
            frames[i] = bitmap.ToImage<Bgr, byte>();

            Thread.Sleep(delay);
        }

        Gdi32.DeleteObject(hBitmap);

        return frames;
    }
}
