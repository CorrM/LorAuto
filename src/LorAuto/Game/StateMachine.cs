using System.Diagnostics;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using LorAuto.Card.Model;
using LorAuto.Client;
using LorAuto.Client.Model;
using LorAuto.Extensions;
using LorAuto.Game.Model;
using LorAuto.OCR;
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
    private readonly OcrManager _ocrManager;
    private readonly byte[][][] _manaMasks;
    private readonly int[] _numPxMask;

    private int _nGames;
    private int _prevGameID = -1;

    private GameResultApiResponse? _gameResult;
    private CardPositionsApiResponse? _gameData;

    public IntPtr GameWindowHandle { get; private set; }
    public Point WindowLocation { get; private set; }
    public Size WindowSize { get; private set; }
    public bool GameIsForeground { get; private set; }
    public GameComponentLocator ComponentLocator { get; }
    public int GamesWonCont { get; private set; }
    public EGameState GameState { get; private set; }
    public BoardCards CardsOnBoard { get; }
    public ActiveDeckApiResponse ActiveDeck { get; }
    public int Mana { get; private set; }
    public int SpellMana { get; private set; }

    public StateMachine(CardSetsManager cardSetsManager, GameClientApi gameClientApi, OcrManager ocrManager)
    {
        _cardSetsManager = cardSetsManager;
        _gameClientApi = gameClientApi;
        _ocrManager = ocrManager;
        _manaMasks = new byte[][][]
        {
            Constants.Zero, Constants.One, Constants.Two, Constants.Three, Constants.Four,
            Constants.Five, Constants.Six, Constants.Seven, Constants.Eight, Constants.Nine, Constants.Ten
        };
        _numPxMask = _manaMasks.Select(mask => mask.SelectMany(line => line).Sum(b => b)).ToArray();

        ComponentLocator = new GameComponentLocator(this);
        CardsOnBoard = new BoardCards();
        ActiveDeck = new ActiveDeckApiResponse() { DeckCode = null, CardsInDeck = new Dictionary<string, int>() };
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

    private async Task<GameResultApiResponse?> GetGameResultAsync(CancellationToken ct = default)
    {
        (GameResultApiResponse? response, Exception? exception) = await _gameClientApi.GetGameResultAsync(ct).ConfigureAwait(false);
        if (exception is not null || response is null)
            return null;

        return response;
    }

    private async Task<CardPositionsApiResponse?> GetGameDataAsync(CancellationToken ct = default)
    {
        (CardPositionsApiResponse? response, Exception? exception) = await _gameClientApi.GetCardPositionsAsync(ct).ConfigureAwait(false);
        if (exception is not null || response is null)
            return null;

        return response;
    }

    private async Task UpdateActiveDeckAsync(CancellationToken ct = default)
    {
        ActiveDeck.CardsInDeck.Clear();

        (ActiveDeckApiResponse? newActiveDeck, Exception? exception) = await _gameClientApi.GetActiveDeckAsync(ct).ConfigureAwait(false);
        if (exception is not null || newActiveDeck is null)
        {
            ActiveDeck.DeckCode = null;
            return;
        }

        ActiveDeck.DeckCode = newActiveDeck.DeckCode;

        foreach ((string cardCode, int cardCount) in newActiveDeck.CardsInDeck)
            ActiveDeck.CardsInDeck.Add(cardCode, cardCount);
    }

    private EInGameCardPosition GetCardPosition(InGameCard card, GameClientRectangle rectCard)
    {
        var cardPosition = EInGameCardPosition.None;
        float yRatio = card.Position.Y / (float)WindowSize.Height;
        if (yRatio > 0.275f && Math.Abs(rectCard.TopLeftY - (WindowSize.Height * 0.6759)) < 0.05) // cardHeightRatio((float)rectCard.Height / WindowSize.Height) > .3f
            cardPosition = EInGameCardPosition.Mulligan;

        if (cardPosition == EInGameCardPosition.None)
        {
            cardPosition = yRatio switch
            {
                > 0.92f => EInGameCardPosition.Hand,
                > 0.75f => EInGameCardPosition.Board,
                > 0.58f => EInGameCardPosition.AttackOrBlock,
                > 0.44f => EInGameCardPosition.SpellStack,
                > 0.265f => EInGameCardPosition.OpponentAttackOrBlock,
                > 0.09f => EInGameCardPosition.OpponentBoard,
                _ => EInGameCardPosition.OpponentHand
            };
        }

        return cardPosition;
    }

    private void StoreCard(InGameCard card, EInGameCardPosition cardPosition)
    {
        // Store cards
        switch (cardPosition)
        {
            case EInGameCardPosition.Mulligan:
                CardsOnBoard.CardsMulligan.Add(card);
                break;

            case EInGameCardPosition.Hand:
                CardsOnBoard.CardsHand.Add(card);
                break;

            case EInGameCardPosition.Board:
                CardsOnBoard.CardsBoard.Add(card);
                break;

            case EInGameCardPosition.AttackOrBlock:
                CardsOnBoard.CardsAttackOrBlock.Add(card);
                break;

            case EInGameCardPosition.SpellStack:
                CardsOnBoard.SpellStack.Add(card);
                break;

            case EInGameCardPosition.OpponentAttackOrBlock:
                CardsOnBoard.OpponentCardsAttackOrBlock.Add(card);
                break;

            case EInGameCardPosition.OpponentBoard:
                CardsOnBoard.OpponentCardsBoard.Add(card);
                break;

            case EInGameCardPosition.OpponentHand:
                CardsOnBoard.OpponentCardsHand.Add(card);
                break;

            case EInGameCardPosition.None:
            default:
                throw new UnreachableException();
        }

        CardsOnBoard.AllCards.Add(card);
    }

    private void UpdatePlayableCardAttackHealth(InGameCard card, Image<Bgr, byte>[] frames)
    {
        var colors = new (Hsv Lower, Hsv Higher)[]
        {
            (new Hsv(0, 0, 230), new Hsv(0, 0, 255)), // White
            (new Hsv(30, 80, 80), new Hsv(180, 255, 255)), // Green
            (new Hsv(0, 250, 200), new Hsv(0, 255, 255)), // Light red
            (new Hsv(0, 0, 255), new Hsv(180, 128, 255)), // Elusive
            // (new Hsv(0, 0, 0), new Hsv(0, 0, 0)), // TODO: Tough
        };
        
        (int Number, float Confidence) ReadNumberFromImage(Image<Bgr, byte> img)
        {
            var ret = new List<(int Num, float Confidence)>();
            
            foreach ((Hsv lower, Hsv higher) in colors)
            {
                using Image<Gray, byte> inRangeImg = img.InHsvRange(lower, higher);
                int countNonZero = CvInvoke.CountNonZero(inRangeImg);
                if (countNonZero == 0)
                    continue;

                using Image<Gray, byte> resizeImg = inRangeImg.Resize(120, 120, Inter.Linear);
                (int number, float confidence) = _ocrManager.ReadNumber(resizeImg);
                
                if (number == -1)
                    continue;

                ret.Add((number, confidence));
            }
            
            return ret.Count > 0 ? ret.MaxBy(x => x.Confidence) : (-1, 0);
        }

        Image<Bgr, byte> image = frames.First();
        (Rectangle attackRect, Rectangle healthRect) = ComponentLocator.GetCardAttackAndHealthRect(card);

        // Attack
        using Image<Bgr, byte> attackCropImg = image.Crop(attackRect);
        (int attackNumber, float attackConfidence) = ReadNumberFromImage(attackCropImg);
        
        Console.WriteLine($"Card ({card.Name}), ATTACK: {attackNumber}, Confidence: {attackConfidence}");
        
        if (attackNumber == -1)
        {
            // CvInvoke.Imshow("attackNumber", attackRangeImg);
            // CvInvoke.WaitKey();
            // CvInvoke.DestroyAllWindows();
            return;
        }

        // Health
        using Image<Bgr, byte> hpCropImg = image.Crop(healthRect);
        (int hpNumber, float hpConfidence) = ReadNumberFromImage(hpCropImg);
        
        Console.WriteLine($"Card ({card.Name}), HEALTH: {hpNumber}, Confidence: {hpConfidence}");
        
        if (hpNumber == -1) // Number 5 not work
        {
            // CvInvoke.Imshow("attackNumber", hpCropImg);
            // CvInvoke.WaitKey();
            // CvInvoke.DestroyAllWindows();
            return;
        }

        card.UpdateAttackHealth(attackNumber, hpNumber);
    }

    private async Task UpdateCardsOnBoardAsync(Image<Bgr, byte>[] frames, CancellationToken ct = default)
    {
        // Store cards references so we can update the card data but in same card instance
        List<InGameCard> previousCards = CardsOnBoard.AllCards.ToList();

        // Clear board state before update
        CardsOnBoard.Clear();

        // !Keep in mind game client api not reveal card current status, like if card is damaged or get new keyword etc.
        (CardPositionsApiResponse? cardPositions, Exception? exception) = await _gameClientApi.GetCardPositionsAsync(ct).ConfigureAwait(false);
        if (exception is not null || cardPositions is null)
            return;

        foreach (GameClientRectangle rectCard in cardPositions.Rectangles.Where(rectCard => rectCard.CardCode != "face"))
        {
            InGameCard inGameCard;
            InGameCard? toUpdate = previousCards.FirstOrDefault(c => c.CardID == rectCard.CardID);
            if (toUpdate is not null)
            {
                toUpdate.UpdatePosition(rectCard, WindowSize);
                inGameCard = toUpdate;
            }
            else
            {
                GameCardSet? gameCardSet = _cardSetsManager.CardSets.FirstOrDefault(cs => cs.Value.Cards.ContainsKey(rectCard.CardCode)).Value;
                if (gameCardSet is null)
                {
                    _cardSetsManager.DeleteCardSets();
                    throw new Exception($"Card set that contains card with key({rectCard.CardCode}) not found. (Delete card sets folder may solve the problem)");
                }

                GameCard cardSetCard = gameCardSet.Cards[rectCard.CardCode];
                inGameCard = new InGameCard(cardSetCard, rectCard, WindowSize);
            }

            EInGameCardPosition cardPosition = GetCardPosition(inGameCard, rectCard);
            StoreCard(inGameCard, cardPosition);

            if (cardPosition is EInGameCardPosition.Board or EInGameCardPosition.AttackOrBlock or EInGameCardPosition.OpponentBoard or EInGameCardPosition.OpponentAttackOrBlock)
                UpdatePlayableCardAttackHealth(inGameCard, frames);

            // Update hand card cost
            if (cardPosition == EInGameCardPosition.Hand)
            {
                // TODO: Update hand card cost
            }
        }

        // Some times opponent attacking cards are right to left
        CardsOnBoard.Sort();
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
        if (_gameResult is null || _gameData is null)
            throw new UnreachableException();

        // # Menus
        Image<Bgr, byte> lastFrame = frames.First();
        if (_gameData.GameState == "Menus")
        {
            // # Menus deck selected
            Rectangle menusEditDeckButtonRect = ComponentLocator.GetMenusEditDeckButtonRect();

            using Image<Bgr, byte> menusEditDeckButtonSubImg = lastFrame.Crop(menusEditDeckButtonRect);
            int menusEditDeckButtonPx = menusEditDeckButtonSubImg.CountNonZeroInHsvRange(new Hsv(10, 70, 140), new Hsv(20, 180, 255));
            if (menusEditDeckButtonPx > 700)
                return EGameState.MenusDeckSelected;

            if (_gameResult.GameID > _prevGameID)
            {
                if (_gameResult.LocalPlayerWon)
                    GamesWonCont += 1;

                _nGames += 1;
                _prevGameID = _gameResult.GameID;

                return EGameState.End;
            }

            // TODO: Check for SearchGame here
            //return EGameState.SearchGame;

            return EGameState.Menus;
        }

        // # User interact not ready
        Rectangle roundsLogRect = ComponentLocator.GetRoundsLogRect();

        using Image<Bgr, byte> roundsLogSubImg = lastFrame.Crop(roundsLogRect);
        int roundsLogPx = roundsLogSubImg.CountNonZeroInHsvRange(new Hsv(20, 80, 130), new Hsv(30, 150, 180));
        Console.WriteLine($"roundsLogPx: {roundsLogPx}");

        bool inAction = roundsLogPx > 110 && roundsLogPx < 140; // When block or attack image go darker
        if (!inAction && roundsLogPx < 590)
            return EGameState.UserInteractNotReady;

        // # Mulligan check
        // TODO: Could be just `CardsOnBoard.CardsMulligan.Count > 0`
        GameClientRectangle[] localCards = _gameData.Rectangles.Where(card => card.CardCode != "face" && card.LocalPlayer).ToArray();
        if (localCards.Length > 0 && localCards.Count(card => Math.Abs(card.TopLeftY - (WindowSize.Height * 0.6759)) < 0.05) == localCards.Length)
            return EGameState.Mulligan;

        // TODO: Maybe need some more conditions as the python bot are using sleep a lot, so it just maybe that's why blocking state are accurate
        //       anyway ("Check if card is already blocked") check in `Bot::Block` method can handle it
        if (CardsOnBoard.OpponentCardsAttackOrBlock.Count > 0)
            return EGameState.Blocking;

        // # Check if it's our turn
        Rectangle turnButtonRect = ComponentLocator.GetTurnButtonRect();
        using Image<Bgr, byte> turnBtnSubImg = lastFrame.Crop(turnButtonRect);
        int numBluePx = turnBtnSubImg.CountNonZeroInHsvRange(new Hsv(5, 200, 200), new Hsv(260, 255, 255)); // Blue color space
        if (numBluePx < 100) // End turn button is GRAY
            return EGameState.OpponentTurn;

        // # Check if local_player has the attack token
        Rectangle attackRect = ComponentLocator.GetAttackTokenRect();

        using Image<Bgr, byte> attackTokenSubImg = lastFrame.Crop(attackRect);
        int numOrangePx = attackTokenSubImg.CountNonZeroInHsvRange(new Hsv(5, 120, 224), new Hsv(25, 255, 255)); // Orange color space
        if (numOrangePx > 1000) // Not enough orange pixels for attack token
            return EGameState.AttackTurn;

        numOrangePx = attackTokenSubImg.CountNonZeroInHsvRange(new Hsv(10, 120, 245), new Hsv(30, 225, 255)); // Orange color space
        if (numOrangePx > 1000) // Not enough orange pixels for attack token
            return EGameState.AttackTurn;

        return EGameState.DefendTurn;
    }

    private int GetMana(Image<Bgr, byte>[] frames, int maxRetry = 2)
    {
        /*
         This code iterates over the frames list and MANA_MASKS array,
         calculates the sum of the edge values based on the mask, and checks if the average exceeds the threshold.
         The indices that satisfy the condition are added to the manaVals list.
         */

        Rectangle manaRect = ComponentLocator.GetManaRect();

        var manaVals = new List<(int Number, double Ratio)>();
        for (int retryCount = 0; retryCount < maxRetry; retryCount++)
        {
            foreach (Image<Bgr, byte> frame in frames)
            {
                using Image<Bgr, byte> image = frame.Crop(manaRect);

                for (int i = 0; i < _manaMasks.Length; i++)
                {
                    byte[][] mask = _manaMasks[i];

                    using Image<Gray, byte> grayImage = image.Convert<Gray, byte>();
                    using Image<Gray, byte> cannyImage = grayImage.Canny(100, 100);

                    double sum = 0;
                    int count = 0;
                    for (int y = 0; y < cannyImage.Height; y++)
                    {
                        for (int x = 0; x < cannyImage.Width; x++)
                        {
                            if (mask[y][x] == 0)
                                continue;

                            sum += cannyImage.Data[y, x, 0];
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

    private int GetSpellMana(Image<Bgr, byte>[] frames)
    {
        Image<Bgr, byte> lastFrame = frames.First();
        Rectangle[] spellManaRect = ComponentLocator.GetSpellManaRect();

        int spellMana = 0;
        foreach (Rectangle curManaRect in spellManaRect)
        {
            using Image<Bgr, byte> turnBtnSubImg = lastFrame.Crop(curManaRect);
            int numBluePx = turnBtnSubImg.CountNonZeroInHsvRange(new Hsv(5, 200, 200), new Hsv(260, 255, 255)); // Blue color space
            if (numBluePx <= 40)
                break;

            ++spellMana;
        }

        return Math.Min(3, spellMana);
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

        Image<Bgr, byte>[] frames = GetFrames();

        // Client API
        _gameResult = await GetGameResultAsync(ct).ConfigureAwait(false); // TODO: Make it update function to not instantiate new instance every time
        _gameData = await GetGameDataAsync(ct).ConfigureAwait(false); // TODO: Make it update function to not instantiate new instance every time
        await UpdateActiveDeckAsync(ct).ConfigureAwait(false);
        await UpdateCardsOnBoardAsync(frames, ct).ConfigureAwait(false); // must to be called before 'GetGameState'

        // Game
        GameState = GetGameState(frames);
        Mana = GetMana(frames);
        SpellMana = GetSpellMana(frames);

        // Clean
        foreach (Image<Bgr, byte> frame in frames)
            frame.Dispose();

        if (GameState != EGameState.End)
            return;

        CardsOnBoard.Clear();
        Mana = 0;
        SpellMana = 0;
        // prev_mana = 0;
        // turn = 0;
    }
}
