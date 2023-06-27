using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using LorAuto.Card;
using LorAuto.Client;
using LorAuto.Client.Model;
using PInvoke;
using Constants = LorAuto.GameState.Model.Constants;
using Image = System.Drawing.Image;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using Range = Emgu.CV.Structure.Range;

namespace LorAuto.GameState;

public enum GameState
{
    None,
    Menu,
}

/// <summary>
/// Determines the game state and cards on board by using the LoR API and cv2 functionality
/// </summary>
public sealed class StateMachine : IDisposable
{
    private readonly CardSetsManager _cardSetsManager;
    private readonly GameClientApi _gameClientApi;
    private readonly byte[][][] _manaMasks;
    private readonly int[] _numPxMask;

    private bool _work; 
    private bool _busy;
    
    public IntPtr GameWindowHandle { get; private set; }
    public GameState GameState { get; private set; }
    public bool GameIsForeground { get; private set; }
    public Point WindowLocation { get; private set; }
    public Size WindowSize { get; private set; }
    
    public StateMachine(CardSetsManager cardSetsManager, GameClientApi gameClientApi)
    {
        _cardSetsManager = cardSetsManager;
        _gameClientApi = gameClientApi;
        _manaMasks = new byte[][][]
        {
            Constants.Zero, Constants.One, Constants.Two, Constants.Three, Constants.Four,
            Constants.Five, Constants.Six, Constants.Seven, Constants.Eight, Constants.Nine, Constants.Ten
        };
        _numPxMask = _manaMasks.Select(mask => mask.SelectMany(line => line).Sum(b => b)).ToArray();

        User32.SetProcessDPIAware();
    }
    
    private IntPtr GetWindowHandle()
    {
        IntPtr targetHandler = IntPtr.Zero;
        User32.EnumWindows((handle, _) =>
        {
            int windowTextLength = User32.GetWindowTextLength(handle) + 1;
            Span<char> windowName = stackalloc char[windowTextLength];

            User32.GetWindowText(handle, windowName);
            windowName = windowName[0..windowName.IndexOf('\0')];

            if (!MemoryExtensions.Equals(windowName, "Legends of Runeterra", StringComparison.Ordinal))
                return true;

            targetHandler = handle;
            return false;
        }, IntPtr.Zero);

        return targetHandler;
    }

    private (Point, Size) GetWindowRectInfo()
    {
        if (GameWindowHandle == IntPtr.Zero)
            return (new Point(), new Size());

        if (!User32.GetWindowRect(GameWindowHandle, out RECT targetRect))
            return (new Point(), new Size());

        var loc = new Point(targetRect.top, targetRect.left);
        var size = new Size(targetRect.right - targetRect.top, targetRect.bottom - targetRect.left);

        return (loc, size);
    }
    
    private GameState GetGameState()
    {
        return GameState.None;
    }

    private bool GetGameIsForeground()
    {
        IntPtr hWindow = User32.GetForegroundWindow();
        
        return hWindow == GameWindowHandle;
    }
    
    private Image<Bgr, byte>[] GetFrames()
    {
        const int framesCount = 4;
        (Point loc, Size size) = GetWindowRectInfo();
        var frames = new Image<Bgr, byte>[framesCount];

        using User32.SafeDCHandle hdcScreen = User32.GetDC(IntPtr.Zero);
        using User32.SafeDCHandle hdc = Gdi32.CreateCompatibleDC(hdcScreen);

        IntPtr hBmp = Gdi32.CreateCompatibleBitmap(hdcScreen, size.Width, size.Height);
        Gdi32.SelectObject(hdc, hBmp);

        for (int i = 0; i < framesCount; i++)
        {
            Gdi32.BitBlt(hdc, 0, 0, size.Width, size.Height, hdcScreen, loc.X, loc.Y, 0xCC0020);

            using Bitmap bitmap = Image.FromHbitmap(hBmp);
            frames[i] = bitmap.ToImage<Bgr, byte>();

            Thread.Sleep(8);
        }

        Gdi32.DeleteObject(hBmp);

        return frames;
    }
    
    private void WorkLoop()
    {
        _busy = true;

        while (_work)
        {
            GameWindowHandle = GetWindowHandle();
            GameState = GetGameState();
            (WindowLocation, WindowSize) = GetWindowRectInfo();
            GameIsForeground = GetGameIsForeground();
            
            Thread.Sleep(1000);
        }
        
        _busy = false;
    }
    
    public bool Start()
    {
        if (_work)
            return false;
        _work = true;
        
        new Thread(WorkLoop).Start();
        return true;
    }

    public void Stop()
    {
        _work = false;

        while (_busy)
            Thread.Sleep(8);
    }

    public bool IsReady()
    {
        return GameWindowHandle != IntPtr.Zero;
    }
    
    public int GetMana(int maxRetry = 2)
    {
        /*
         When attack and points are 3 maybe show as 1
         6 Doesnt work
         */
        int posX = WindowSize.Width - (int)(WindowSize.Width / 5.73134f); // 1585
        int posY = WindowSize.Height - (int)(WindowSize.Height / 2.4434f); // 638
        const int w = 50;
        const int h = 37;

        /*
         This code iterates over the frames list and MANA_MASKS array,
         calculates the sum of the edge values based on the mask, and checks if the average exceeds the threshold.
         The indices that satisfy the condition are added to the manaVals list.
         */
        
        var manaVals = new List<(int Number, double Ratio)>();
        for (int retryCount = 0; retryCount < maxRetry; retryCount++)
        {
            Image<Bgr, byte>[] frames = GetFrames();
            foreach (Image<Bgr, byte> frame in frames)
            {
                using Image<Bgr, byte>? image = new Mat(frame.Mat, new Range(posY, posY + h), new Range(posX, posX + w)).ToImage<Bgr, byte>();
    
                for (int i = 0; i < _manaMasks.Length; i++)
                {
                    byte[][] mask = _manaMasks[i];

                    using Image<Gray, byte> grayImage = image.Convert<Gray, byte>()
                        .Canny(100, 100);

                    double sum = 0;
                    int count = 0;
                    for (int y = 0; y < grayImage.Height; y++)
                    {
                        for (int x = 0; x < grayImage.Width; x++)
                        {
                            if (mask[y][x] == 0)
                                continue;
                        
                            sum += grayImage.Data[y, x, 0];
                            count++;
                        }
                    }

                    double average = (count > 0) ? sum / count : 0;
                    double ratio = average / _numPxMask[i];
                    if (ratio > 0.95)
                        manaVals.Add((i, ratio));
                }
            }

            if (manaVals.Count > 0)
                return manaVals.MaxBy(t => t.Ratio).Number;
        }

        return -1;
    }

    public async Task<BoardState?> GetBoardState()
    {
        (CardPositionsApiRequest? cardPositions, Exception? exception) = await _gameClientApi.GetCardPositionsAsync().ConfigureAwait(false);
        if (exception is not null || cardPositions is null)
            return null;

        var boardState = new BoardState();
        foreach (GameClientRectangle rectCard in cardPositions.Rectangles)
        {
            string cardCode = rectCard.CardCode;
            if (cardCode == "face")
                continue;

            GameCardSet? gameCardSet = _cardSetsManager.CardSets.FirstOrDefault(cs => cs.Value.Cards.ContainsKey(cardCode)).Value;
            if (gameCardSet is null)
            {
                // TODO: dont use console
                Console.WriteLine($"Warning: card set that contains card with key({cardCode}) not found.");
                continue;
            }

            GameCard card = gameCardSet.Cards[cardCode];
            var inGameCard = new InGameCard(card, rectCard.TopLeftX, rectCard.TopLeftY, rectCard.Width, rectCard.Height, rectCard.LocalPlayer);

            int cardY = WindowSize.Height - inGameCard.TopCenterPos.Y;
            float yRatio = (float)cardY / WindowSize.Height;
            float cardRatio = (float)rectCard.Height / WindowSize.Height;

            if (yRatio > 0.275f && cardRatio > .3f)
            {
                boardState.CardsMulligan.Add(inGameCard);
                continue;
            }

            switch (yRatio)
            {
                case > 0.97f:
                    boardState.CardsHand.Add(inGameCard);
                    break;

                case > 0.75f:
                    boardState.CardsBoard.Add(inGameCard);
                    break;

                case > 0.6f:
                    boardState.CardsAttack.Add(inGameCard);
                    break;

                case > 0.45f:
                    boardState.SpellStack.Add(inGameCard);
                    break;

                case > 0.275f:
                    boardState.OpponentCardsAttack.Add(inGameCard);
                    break;

                case > 0.1f:
                    boardState.OpponentCardsBoard.Add(inGameCard);
                    break;

                default:
                    boardState.OpponentCardsHand.Add(inGameCard);
                    break;
            }
        }

        return boardState;
    }

    public void Dispose()
    {
        Stop();
    }
}
