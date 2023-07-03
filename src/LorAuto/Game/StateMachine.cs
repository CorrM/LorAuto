using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using LorAuto.Card.Model;
using LorAuto.Client;
using LorAuto.Client.Model;
using LorAuto.Extensions;
using PInvoke;
using Constants = LorAuto.Game.Model.Constants;
using Image = System.Drawing.Image;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace LorAuto.Game;

/// <summary>
/// Determines the game state and cards on board by using the LoR API and cv2 functionality
/// </summary>
public sealed class StateMachine
{
    private readonly CardSetsManager _cardSetsManager;
    private readonly GameClientApi _gameClientApi;
    private readonly byte[][][] _manaMasks;
    private readonly int[] _numPxMask;
    private readonly (double, double)[] _attackTokenBounds;

    private int _nGames;
    private int _prevGameID;

    private GameResultApiResponse? _gameResult;
    private CardPositionsApiResponse? _gameData;

    public IntPtr GameWindowHandle { get; private set; }
    public Point WindowLocation { get; private set; }
    public Size WindowSize { get; private set; }
    public bool GameIsForeground { get; private set; }
    public int GamesWonCont { get; private set; }
    public EGameState GameState { get; private set; }
    public BoardCards CardsOnBoard { get; private set; }
    public int Mana { get; private set; }
    public int SpellMana { get; private set; }

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
        _attackTokenBounds = new[] { (0.80, 0.6), (0.9, 0.78) };

        CardsOnBoard = new BoardCards();
        //User32.SetProcessDPIAware();
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

        var loc = new Point(targetRect.left, targetRect.top);
        var size = new Size(targetRect.right - targetRect.left, targetRect.bottom - targetRect.top);

        return (loc, size);
    }

    private bool GetGameIsForeground()
    {
        IntPtr hWindow = User32.GetForegroundWindow();
        return hWindow == GameWindowHandle;
    }

    private async Task<GameResultApiResponse?> GetGameResultAsync()
    {
        (GameResultApiResponse? response, Exception? exception) = await _gameClientApi.GetGameResultAsync().ConfigureAwait(false);
        if (exception is not null || response is null)
            return null;

        return response;
    }

    private async Task<CardPositionsApiResponse?> GetGameDataAsync()
    {
        (CardPositionsApiResponse? response, Exception? exception) = await _gameClientApi.GetCardPositionsAsync().ConfigureAwait(false);
        if (exception is not null || response is null)
            return null;

        return response;
    }

    private Image<Bgr, byte>[] GetFrames()
    {
        const int framesCount = 4;
        (Point loc, Size size) = GetWindowRectInfo();
        var frames = new Image<Bgr, byte>[framesCount];
        
        using User32.SafeDCHandle hdcScreen = User32.GetDC(IntPtr.Zero);
        using User32.SafeDCHandle hdc = Gdi32.CreateCompatibleDC(hdcScreen);

        IntPtr hBitmap = Gdi32.CreateCompatibleBitmap(hdcScreen, size.Width, size.Height);
        Gdi32.SelectObject(hdc, hBitmap);

        for (int i = 0; i < framesCount; i++)
        {
            Gdi32.BitBlt(hdc, 0, 0, size.Width, size.Height, hdcScreen, loc.X, loc.Y, 0xCC0020);

            using Bitmap bitmap = Image.FromHbitmap(hBitmap);
            frames[i] = bitmap.ToImage<Bgr, byte>();

            Thread.Sleep(8);
        }

        Gdi32.DeleteObject(hBitmap);

        return frames;
    }

    private EGameState GetGameState(Image<Bgr, byte>[] frames)
    {
        // TODO: Give user control to pause bot
        //if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        //    return GameState.Hold;

        if (_gameResult is not null)
        {
            if (_gameResult.GameID > _prevGameID)
            {
                if (_gameResult.LocalPlayerWon)
                    GamesWonCont += 1;

                _nGames += 1;
                _prevGameID = _gameResult.GameID;

                return EGameState.End;
            }
        }

        Image<Bgr, byte> lastFrame = frames.Last();
        if (_gameData is null || _gameData.GameState == "Menus")
        {
            // TODO: Check for SearchGame here
            //return EGameState.SearchGame;

            return EGameState.Menus;
        }

        // Mulligan check
        // TODO: Could be just `CardsOnBoard.CardsMulligan.Count > 0`
        GameClientRectangle[] localCards = _gameData.Rectangles.Where(card => card.CardCode != "face" && card.LocalPlayer).ToArray();
        if (localCards.Length > 0 && localCards.Count(card => Math.Abs(card.TopLeftY - (WindowSize.Height * 0.6759)) < 0.05) == localCards.Length)
            return EGameState.Mulligan;

        // TODO: Maybe need some more conditions as the python bot are using sleep a lot, so it just maybe thats why blocking state are accurate
        //       Anyway ("Check if card is already blocked") check in `Bot::Block` method can handle it
        if (CardsOnBoard.OpponentCardsAttackOrBlock.Count > 0)
            return EGameState.Blocking;

        // Check if it's our turn
        using Image<Bgr, byte> turnBtnSubImg = lastFrame.Crop((int)(WindowSize.Width * 0.77), (int)(WindowSize.Height * 0.42), (int)(WindowSize.Width * 0.93) - (int)(WindowSize.Width * 0.77), (int)(WindowSize.Height * 0.58) - (int)(WindowSize.Height * 0.42));
        using Image<Hsv, byte>? hsv = turnBtnSubImg.Convert<Hsv, byte>();
        using Image<Gray, byte>? mask = hsv.InRange(new Hsv(5, 200, 200), new Hsv(260, 255, 255)); // Blue color space
        using Image<Bgr, byte>? targetAndMask = turnBtnSubImg.And(turnBtnSubImg, mask);
        using Image<Gray, byte>? btnTargetAndMask = targetAndMask.Convert<Gray, byte>();

        int numBluePx = CvInvoke.CountNonZero(btnTargetAndMask);
        if (numBluePx < 100) // End turn button is GRAY
            return EGameState.OpponentTurn;

        // Check if local_player has the attack token
        int attackTokenBoundLx = (int)(WindowSize.Width * _attackTokenBounds[0].Item1);
        int attackTokenBoundLy = (int)(WindowSize.Height * _attackTokenBounds[0].Item2);
        int attackTokenBoundRx = ((int)(WindowSize.Width * _attackTokenBounds[1].Item1)) - attackTokenBoundLx;
        int attackTokenBoundRy = ((int)(WindowSize.Height * _attackTokenBounds[1].Item2)) - attackTokenBoundLy;

        //foreach (Image<Bgr, byte> img in frames)
        {
            using Image<Bgr, byte> attackTokenSubImg = lastFrame.Crop(attackTokenBoundLx, attackTokenBoundLy, attackTokenBoundRx, attackTokenBoundRy);
            using Image<Hsv, byte>? attackTokenHsv = attackTokenSubImg.Convert<Hsv, byte>();

            using Image<Gray, byte>? attackTokenMask1 = attackTokenHsv.InRange(new Hsv(5, 120, 224), new Hsv(25, 255, 255)); // Orange
            using Image<Bgr, byte>? attackTokenTarget1 = attackTokenSubImg.And(attackTokenSubImg, attackTokenMask1);
            using Image<Gray, byte>? attackTokenTargetGray1 = attackTokenTarget1.Convert<Gray, byte>();

            int numOrangePx = CvInvoke.CountNonZero(attackTokenTargetGray1);
            if (numOrangePx > 1000) // Not enough orange pixels for attack token
                return EGameState.AttackTurn;

            using Image<Gray, byte>? attackTokenMask2 = attackTokenHsv.InRange(new Hsv(10, 120, 245), new Hsv(30, 225, 255)); // Orange
            using Image<Bgr, byte>? attackTokenTarget2 = attackTokenSubImg.And(attackTokenSubImg, attackTokenMask2);
            using Image<Gray, byte>? attackTokenTargetGray2 = attackTokenTarget2.Convert<Gray, byte>();

            numOrangePx = CvInvoke.CountNonZero(attackTokenTargetGray1);
            if (numOrangePx > 1000) // Not enough orange pixels for attack token
                return EGameState.AttackTurn;
        }

        return EGameState.DefendTurn;
    }

    private int GetMana(Image<Bgr, byte>[] frames, int maxRetry = 2)
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
            foreach (Image<Bgr, byte> frame in frames)
            {
                using Image<Bgr, byte> image = frame.Crop(posX, posY, w, h);

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
    
    private async Task UpdateCardsOnBoardAsync()
    {
        // TODO: BUG, if two copy of card in the board strategy attack/block can't add them in dictionary because they are equal an dictionary need unique key

        // Store cards references so we can update the card data but in same card instance
        List<InGameCard> previousCards = CardsOnBoard.AllCards.ToList();
        
        // Clear board state before update
        CardsOnBoard.Clear();

        // NOTE: Keep in mind game client api not reveal card current status, like if card is damaged or get new keyword etc.
        (CardPositionsApiResponse? cardPositions, Exception? exception) = await _gameClientApi.GetCardPositionsAsync().ConfigureAwait(false);
        if (exception is not null || cardPositions is null)
            return;

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

            // TODO: Better way to make update for card, instead of make a new instance
            var inGameCard = new InGameCard(gameCardSet.Cards[cardCode], rectCard.TopLeftX, rectCard.TopLeftY, rectCard.Width, rectCard.Height, rectCard.LocalPlayer);
            InGameCard? toUpdate = previousCards.FirstOrDefault(c => c == inGameCard);
            if (toUpdate is not null)
            {
                toUpdate.Update(inGameCard);
                inGameCard = toUpdate;
            }

            CardsOnBoard.AllCards.Add(inGameCard);

            int cardY = WindowSize.Height - inGameCard.TopCenterPos.Y;
            float yRatio = (float)cardY / WindowSize.Height;

            if (yRatio > 0.275f && Math.Abs(rectCard.TopLeftY - (WindowSize.Height * 0.6759)) < 0.05) // cardRatio((float)rectCard.Height / WindowSize.Height) > .3f
            {
                CardsOnBoard.CardsMulligan.Add(inGameCard);
                continue;
            }

            switch (yRatio)
            {
                case > 0.97f:
                    CardsOnBoard.CardsHand.Add(inGameCard);
                    break;

                case > 0.75f:
                    CardsOnBoard.CardsBoard.Add(inGameCard);
                    break;

                case > 0.6f:
                    CardsOnBoard.CardsAttackOrBlock.Add(inGameCard);
                    break;

                case > 0.45f:
                    CardsOnBoard.SpellStack.Add(inGameCard);
                    break;

                case > 0.275f:
                    CardsOnBoard.OpponentCardsAttackOrBlock.Add(inGameCard);
                    break;

                case > 0.1f:
                    CardsOnBoard.OpponentCardsBoard.Add(inGameCard);
                    break;

                default:
                    CardsOnBoard.OpponentCardsHand.Add(inGameCard);
                    break;
            }
        }
    }
    
    public void UpdateClientInfo()
    {
        GameWindowHandle = GetWindowHandle();
        (WindowLocation, WindowSize) = GetWindowRectInfo();
        GameIsForeground = GetGameIsForeground();
    }
    
    public async Task UpdateGameDataAsync(CancellationToken ct = default)
    {
        if (GameWindowHandle == IntPtr.Zero)
            throw new Exception($"'{nameof(UpdateClientInfo)}' must to be called at least once before calling this function.");
        
        // Client API
        _gameResult = await GetGameResultAsync().ConfigureAwait(false); // TODO: Make it update function to not instantiate new instance every time
        _gameData = await GetGameDataAsync().ConfigureAwait(false); // TODO: Make it update function to not instantiate new instance every time
        await UpdateCardsOnBoardAsync().ConfigureAwait(false); // must to be called before 'GetGameState'

        // Game
        Image<Bgr, byte>[] frames = GetFrames();
        GameState = GetGameState(frames);
        Mana = GetMana(frames);

        // Clean
        foreach (Image<Bgr, byte> frame in frames)
            frame.Dispose();

        if (GameState != EGameState.End)
            return;

        Mana = 0;
        SpellMana = 0;
        // prev_mana = 0;
        // turn = 0;
    }
}
