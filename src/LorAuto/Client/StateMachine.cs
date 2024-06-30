using System.Diagnostics;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using LorAuto.Card;
using LorAuto.Card.Model;
using LorAuto.Client.Model;
using LorAuto.Extensions;
using LorAuto.OCR;
using Microsoft.Extensions.Logging;
using PInvoke;
using Constants = LorAuto.Client.Model.Constants;

namespace LorAuto.Client;

// TODO: Move this class from this namespace

/// <summary>
/// Determines the game state and cards on board by using the LoR API and cv2 functionality
/// </summary>
internal sealed class StateMachine : IDisposable
{
    private readonly ILogger? _logger;
    private readonly GameWindow _gameWindow;
    private readonly CardSetsManager _cardSetsManager;
    private readonly OcrHelper _ocrHelper;
    private readonly GameClientApi _gameClientApi;
    private readonly byte[][][] _manaMasks;
    private readonly int[] _numPxMask;

    private int _nGames;
    private int _prevGameId;

    private GameResultApiResponse? _gameResult;
    private CardPositionsApiResponse? _gameData;
    private readonly (Hsv Lower, Hsv Higher)[] _recognizeColors;

    /// <summary>
    /// Gets the number of games won continuously.
    /// </summary>
    public int GamesWonCont { get; private set; }

    /// <summary>
    /// Gets the current game state.
    /// </summary>
    public GameState GameState { get; private set; }

    /// <summary>
    /// Gets the game board data.
    /// </summary>
    public GameBoardData BoardDate { get; }

    /// <summary>
    /// Gets the active deck information.
    /// </summary>
    public ActiveDeckApiResponse ActiveDeck { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StateMachine"/> class with the specified card sets manager and game client port.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="gameWindow">The game window.</param>
    /// <param name="cardSetsManager">The card sets manager.</param>
    /// <param name="gameClientPort">The port number of the game client.</param>
    public StateMachine(ILogger? logger, GameWindow gameWindow, CardSetsManager cardSetsManager, int gameClientPort)
    {
        _logger = logger;
        _gameWindow = gameWindow;
        _cardSetsManager = cardSetsManager;
        _ocrHelper = new OcrHelper("./TessData", "eng");
        _gameClientApi = new GameClientApi(gameClientPort);
        _manaMasks =
        [
            Constants.Zero,
            Constants.One,
            Constants.Two,
            Constants.Three,
            Constants.Four,
            Constants.Five,
            Constants.Six,
            Constants.Seven,
            Constants.Eight,
            Constants.Nine,
            Constants.Ten,
        ];
        _numPxMask = _manaMasks.Select(mask => mask.SelectMany(line => line).Sum(b => b)).ToArray();
        _prevGameId = -1;
        _recognizeColors =
        [
            (new Hsv(0, 0, 230), new Hsv(0, 0, 255)), // White
            (new Hsv(0, 0, 230), new Hsv(180, 70, 255)), // White
            (new Hsv(30, 80, 80), new Hsv(180, 255, 255)), // Green
            (new Hsv(0, 250, 200), new Hsv(0, 255, 255)), // Light red
            (new Hsv(0, 0, 255), new Hsv(180, 128, 255)), // Elusive
            // (new Hsv(0, 0, 0), new Hsv(0, 0, 0)), // TODO: Tough
            // (new Hsv(0, 0, 0), new Hsv(0, 0, 0)), // TODO: Frost
        ];

        BoardDate = new GameBoardData();
        ActiveDeck = new ActiveDeckApiResponse()
        {
            DeckCode = null,
            CardsInDeck = new Dictionary<string, int>(),
        };
    }

    /// <summary>
    /// Determines whether the player can react based on the last captured frame.
    /// </summary>
    /// <param name="frame">The captured frame.</param>
    /// <returns><c>true</c> if the player can react; otherwise, <c>false</c>.</returns>
    private bool PlayerCanInteract(Image<Bgr, byte> frame)
    {
        using Image<Bgr, byte> turnBtnSubImg = frame.Crop(_gameWindow.ComponentLocator.GetTurnButtonRect());
        int mulliganNumBluePx =
            turnBtnSubImg.CountNonZeroInHsvRange(new Hsv(5, 200, 200), new Hsv(260, 255, 255)); // Blue color space

        return mulliganNumBluePx > 100; // End turn button is not GRAY
    }

    /// <summary>
    /// Retrieves the game result asynchronously.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The game result response.</returns>
    private async Task<GameResultApiResponse?> GetGameResultAsync(CancellationToken ct = default)
    {
        (GameResultApiResponse? response, Exception? exception) =
            await _gameClientApi.GetGameResultAsync(ct).ConfigureAwait(false);
        if (exception is not null || response is null)
            return null;

        return response;
    }

    /// <summary>
    /// Retrieves the game data asynchronously.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The card positions response.</returns>
    private async Task<CardPositionsApiResponse?> GetGameDataAsync(CancellationToken ct = default)
    {
        (CardPositionsApiResponse? response, Exception? exception) =
            await _gameClientApi.GetCardPositionsAsync(ct).ConfigureAwait(false);
        if (exception is not null || response is null)
            return null;

        return response;
    }

    /// <summary>
    /// Updates the active deck asynchronously.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    private async Task UpdateActiveDeckAsync(CancellationToken ct = default)
    {
        ActiveDeck.CardsInDeck.Clear();

        (ActiveDeckApiResponse? newActiveDeck, Exception? exception) =
            await _gameClientApi.GetActiveDeckAsync(ct).ConfigureAwait(false);
        if (exception is not null || newActiveDeck is null)
        {
            ActiveDeck.DeckCode = null;
            return;
        }

        ActiveDeck.DeckCode = newActiveDeck.DeckCode;

        foreach ((string cardCode, int cardCount) in newActiveDeck.CardsInDeck)
            ActiveDeck.CardsInDeck.Add(cardCode, cardCount);
    }

    /// <summary>
    /// Gets the card position based on the given game client rectangle.
    /// </summary>
    /// <param name="rectCard">The game client rectangle representing the card.</param>
    /// <returns>The in-game card position.</returns>
    private InGameCardHolder GetCardHolder(GameClientRectangle rectCard)
    {
        var cardPosition = InGameCardHolder.None;
        int y = _gameWindow.WindowSize.Height - rectCard.TopLeftY;
        float yRatio = y / (float)_gameWindow.WindowSize.Height;

        if (yRatio > 0.275f &&
            Math.Abs(rectCard.TopLeftY - (_gameWindow.WindowSize.Height * 0.6759)) <
            0.05) // cardHeightRatio((float)rectCard.Height / WindowSize.Height) > .3f
        {
            cardPosition = InGameCardHolder.Mulligan;
        }

        if (cardPosition == InGameCardHolder.None)
        {
            cardPosition = yRatio switch
            {
                > 0.92f => InGameCardHolder.Hand,
                > 0.75f => InGameCardHolder.Board,
                > 0.58f => InGameCardHolder.AttackOrBlock,
                > 0.44f => InGameCardHolder.SpellStack,
                > 0.265f => InGameCardHolder.OpponentAttackOrBlock,
                > 0.09f => InGameCardHolder.OpponentBoard,
                _ => InGameCardHolder.OpponentHand,
            };
        }

        return cardPosition;
    }

    /// <summary>
    /// Stores the given card in the appropriate card collection based on its position.
    /// </summary>
    /// <param name="card">The in-game card to store.</param>
    /// <param name="cardHolder">The in-game card holder.</param>
    private void StoreCard(InGameCard card, InGameCardHolder cardHolder)
    {
        // Store cards
        switch (cardHolder)
        {
            case InGameCardHolder.Mulligan:
                BoardDate.Cards.CardsMulligan.Add(card);
                break;

            case InGameCardHolder.Hand:
                BoardDate.Cards.CardsHand.Add(card);
                break;

            case InGameCardHolder.Board:
                BoardDate.Cards.CardsBoard.Add(card);
                break;

            case InGameCardHolder.AttackOrBlock:
                BoardDate.Cards.CardsAttackOrBlock.Add(card);
                break;

            case InGameCardHolder.SpellStack:
                BoardDate.Cards.SpellStack.Add(card);
                break;

            case InGameCardHolder.OpponentAttackOrBlock:
                BoardDate.Cards.OpponentCardsAttackOrBlock.Add(card);
                break;

            case InGameCardHolder.OpponentBoard:
                BoardDate.Cards.OpponentCardsBoard.Add(card);
                break;

            case InGameCardHolder.OpponentHand:
                BoardDate.Cards.OpponentCardsHand.Add(card);
                break;

            case InGameCardHolder.None:
            default:
                throw new UnreachableException();
        }

        BoardDate.Cards.AllCards.Add(card);
    }

    /// <summary>
    /// Updates the attack and health values of card in board.
    /// </summary>
    /// <param name="card">The board card.</param>
    /// <param name="frames">The frames of the game window.</param>
    private void UpdateBoardCardAttackHealth(InGameCard card, Image<Bgr, byte>[] frames)
    {
        Image<Bgr, byte> image = frames[0];
        (Rectangle attackRect, Rectangle healthRect) = _gameWindow.ComponentLocator.GetCardAttackAndHealthRect(card);

        // Attack
        using Image<Bgr, byte> attackCropImg = image.Crop(attackRect);
        (int attackNumber, float _) = attackCropImg.ReadNumberFromImage(_ocrHelper, _recognizeColors);

        //Console.WriteLine($"Card ({card.Name}), ATTACK: {attackNumber}, Confidence: {attackConfidence}");

        if (attackNumber == -1)
            return;

        // Health
        using Image<Bgr, byte> hpCropImg = image.Crop(healthRect);
        (int hpNumber, float _) = hpCropImg.ReadNumberFromImage(_ocrHelper, _recognizeColors);

        // Console.WriteLine($"Card ({card.Name}), HEALTH: {hpNumber}, Confidence: {hpConfidence}");

        if (hpNumber == -1)
            return;

        card.UpdateAttackHealth(attackNumber, hpNumber);
    }

    /// <summary>
    /// Updates the cards on the game board asynchronously.
    /// </summary>
    /// <param name="frames">The frames of the game window.</param>
    /// <param name="ct">The cancellation token.</param>
    private async Task UpdateCardsOnBoardAsync(Image<Bgr, byte>[] frames, CancellationToken ct = default)
    {
        // Store cards references so we can update the card data in-place
        List<InGameCard> previousCards = BoardDate.Cards.AllCards.ToList();

        // Clear board state before update
        BoardDate.Cards.Clear();

        // !Keep in mind game client api not reveal card current status, like if card is damaged or get new keyword etc.
        (CardPositionsApiResponse? cardPositions, Exception? exception) =
            await _gameClientApi.GetCardPositionsAsync(ct).ConfigureAwait(false);
        if (exception is not null || cardPositions is null)
            return;

        if (cardPositions.GameState == "Menus")
            return;

        foreach (GameClientRectangle rectCard in
                 cardPositions.Rectangles.Where(rectCard => rectCard.CardCode != "face"))
        {
            InGameCardHolder cardHolder = GetCardHolder(rectCard);

            // Get card
            InGameCard? inGameCard = previousCards.Find(c => c.CardId == rectCard.CardID);
            if (inGameCard is not null)
            {
                inGameCard.UpdatePosition(rectCard, _gameWindow.WindowSize, cardHolder);
            }
            else
            {
                GameCardSet? gameCardSet = _cardSetsManager.CardSets
                    .FirstOrDefault(cs => cs.Value.Cards.ContainsKey(rectCard.CardCode))
                    .Value;
                if (gameCardSet is null)
                {
                    _cardSetsManager.DeleteCardSets();
                    throw new Exception(
                        $"Card set that contains card with key({rectCard.CardCode}) not found. (Delete card sets folder may solve the problem)"
                    );
                }

                inGameCard = new InGameCard(
                    gameCardSet.Cards[rectCard.CardCode],
                    rectCard,
                    _gameWindow.WindowSize,
                    cardHolder
                );
            }

            StoreCard(inGameCard, cardHolder);

            switch (cardHolder)
            {
                case InGameCardHolder.Board
                    or InGameCardHolder.AttackOrBlock
                    or InGameCardHolder.OpponentBoard
                    or InGameCardHolder.OpponentAttackOrBlock:
                    UpdateBoardCardAttackHealth(inGameCard, frames);
                    break;

                case InGameCardHolder.Hand: // Update hand card cost
                    // TODO: Update hand card cost
                    break;
            }
        }

        // Sometimes opponent attacking cards are right to left
        BoardDate.Cards.Sort();
    }

    /// <summary>
    /// Determines the current game state based on various conditions and image analysis.
    /// </summary>
    /// <param name="frames">The frames of the game window.</param>
    /// <returns>The current game state.</returns>
    private GameState GetGameState(Image<Bgr, byte>[] frames)
    {
        if (_gameResult is null || _gameData is null)
        {
            throw new UnreachableException();
        }

        if ((User32.GetAsyncKeyState((int)User32.VirtualKey.VK_LCONTROL) & 0x8000) != 0)
        {
            return GameState.Hold;
        }

        // # Menus
        Image<Bgr, byte> lastFrame = frames[0];
        if (_gameData.GameState == "Menus")
        {
            // # Menus deck selected
            Rectangle menusEditDeckButtonRect = _gameWindow.ComponentLocator.GetMenusEditDeckButtonRect();

            using Image<Bgr, byte> menusEditDeckButtonSubImg = lastFrame.Crop(menusEditDeckButtonRect);
            int menusEditDeckButtonPx =
                menusEditDeckButtonSubImg.CountNonZeroInHsvRange(new Hsv(10, 70, 140), new Hsv(20, 180, 255));
            if (menusEditDeckButtonPx > 700)
            {
                return GameState.MenusDeckSelected;
            }

            // TODO: Check for SearchGame here
            //return EGameState.SearchGame;

            if (_gameResult.GameID <= _prevGameId)
            {
                return GameState.Menus;
            }

            if (_gameResult.LocalPlayerWon)
            {
                GamesWonCont += 1;
            }

            _nGames += 1;
            _prevGameId = _gameResult.GameID;

            return GameState.End;
        }

        // # User interact not ready
        using Image<Bgr, byte> roundsLogSubImg = lastFrame.Crop(_gameWindow.ComponentLocator.GetRoundsLogRect());
        int roundsLogPx = roundsLogSubImg.CountNonZeroInHsvRange(new Hsv(20, 80, 130), new Hsv(30, 150, 180));

        bool inAction = roundsLogPx > 110 && roundsLogPx < 140; // When board is block, attack or spell casting status 
        if (!inAction && roundsLogPx < 430)
        {
            return GameState.UserInteractNotReady;
        }

        // # Mulligan check
        // TODO: Could be just `CardsOnBoard.CardsMulligan.Count > 0`
        GameClientRectangle[] localCards =
            _gameData.Rectangles.Where(card => card.CardCode != "face" && card.LocalPlayer).ToArray();
        if (localCards.Length > 0 &&
            localCards.Count(card => Math.Abs(card.TopLeftY - (_gameWindow.WindowSize.Height * 0.6759)) < 0.05) ==
            localCards.Length)
        {
            return PlayerCanInteract(lastFrame) ? GameState.Mulligan : GameState.UserInteractNotReady;
        }

        // # Block
        if (BoardDate.Cards.OpponentCardsAttackOrBlock.Count > 0)
        {
            return !PlayerCanInteract(lastFrame) ? GameState.UserInteractNotReady : GameState.Blocking;
        }

        // # Check if it's our turn
        if (!PlayerCanInteract(lastFrame))
        {
            return GameState.OpponentTurn;
        }

        // # Check if local_player has the attack token
        using Image<Bgr, byte> attackTokenSubImg = lastFrame.Crop(_gameWindow.ComponentLocator.GetAttackTokenRect());
        int numOrangePx =
            attackTokenSubImg.CountNonZeroInHsvRange(new Hsv(5, 120, 224), new Hsv(25, 255, 255)); // Orange color space
        if (numOrangePx > 1000) // Not enough orange pixels for attack token
        {
            return GameState.AttackTurn;
        }

        numOrangePx =
            attackTokenSubImg.CountNonZeroInHsvRange(
                new Hsv(10, 120, 245),
                new Hsv(30, 225, 255)
            ); // Orange color space
        if (numOrangePx > 1000) // Not enough orange pixels for attack token
        {
            return GameState.AttackTurn;
        }

        return GameState.DefendTurn;
    }

    /// <summary>
    /// Gets the current mana count based on the frames of the game window.
    /// </summary>
    /// <param name="frames">The frames of the game window.</param>
    /// <param name="maxRetry">The maximum number of retries to obtain the mana count.</param>
    /// <returns>The current mana count.</returns>
    private int GetMana(Image<Bgr, byte>[] frames, int maxRetry = 2)
    {
        // TODO: Get raid of that method of getting mana, use same method that used to update in-game card info
        /*
         This code iterates over the frames list and MANA_MASKS array,
         calculates the sum of the edge values based on the mask, and checks if the average exceeds the threshold.
         The indices that satisfy the condition are added to the manaVals list.
         */

        Rectangle manaRect = _gameWindow.ComponentLocator.GetManaRect();

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

    /// <summary>
    /// Gets the current spell mana count based on the frames of the game window.
    /// </summary>
    /// <param name="frames">The frames of the game window.</param>
    /// <returns>The current spell mana count.</returns>
    private int GetSpellMana(Image<Bgr, byte>[] frames)
    {
        Image<Bgr, byte> lastFrame = frames[0];
        Rectangle[] spellManaRect = _gameWindow.ComponentLocator.GetSpellManaRect();

        int spellMana = 0;
        foreach (Rectangle curManaRect in spellManaRect)
        {
            using Image<Bgr, byte> turnBtnSubImg = lastFrame.Crop(curManaRect);
            int numBluePx =
                turnBtnSubImg.CountNonZeroInHsvRange(new Hsv(5, 200, 200), new Hsv(260, 255, 255)); // Blue color space
            if (numBluePx <= 40)
                break;

            ++spellMana;
        }

        return Math.Min(3, spellMana);
    }

    /// <summary>
    /// Gets the current Nexus health values for the player and the opponent from a series of frames.
    /// </summary>
    /// <param name="frames">The array of frames containing the game state.</param>
    /// <returns>A tuple containing the current Nexus health values for the player and the opponent.</returns>
    private (int NexusHealth, int OpponentNexusHealth) GetNexusHealth(Image<Bgr, byte>[] frames)
    {
        var color = new[] { (new Hsv(0, 0, 230), new Hsv(180, 70, 255)) }; // White
        Image<Bgr, byte> image = frames[0];
        (Rectangle player, Rectangle opponent) = _gameWindow.ComponentLocator.GetNexusHealthRect();

        using Image<Bgr, byte> playerCropImg = image.Crop(player);
        (int playerNexus, float _) = playerCropImg.ReadNumberFromImage(_ocrHelper, color);
        if (playerNexus == -1)
        {
            return (-1, -1);
        }

        using Image<Bgr, byte> opponentCropImg = image.Crop(opponent);
        (int opponentNexus, float _) = opponentCropImg.ReadNumberFromImage(_ocrHelper, color);
        if (opponentNexus == -1)
        {
            return (-1, -1);
        }

        return (playerNexus, opponentNexus);
    }

    /// <summary>
    /// Updates the game data asynchronously.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    public async Task UpdateGameDataAsync(CancellationToken ct = default)
    {
        Image<Bgr, byte>[] frames = _gameWindow.GetFrames(framesCount: 4, delay: 8);

        // Client API
        // TODO: Make it update function, not to instantiate new instance every time
        _gameResult = await GetGameResultAsync(ct).ConfigureAwait(false);

        // TODO: Make it update function, not to instantiate new instance every time
        _gameData = await GetGameDataAsync(ct).ConfigureAwait(false);

        await UpdateActiveDeckAsync(ct).ConfigureAwait(false);
        await UpdateCardsOnBoardAsync(frames, ct).ConfigureAwait(false); // Must be called before 'GetGameState'

        // Game
        GameState = GetGameState(frames);
        if (GameState is not GameState.Menus and not GameState.MenusDeckSelected and not GameState.End)
        {
            BoardDate.Mana = GetMana(frames);
            BoardDate.SpellMana = GetSpellMana(frames);
            (BoardDate.NexusHealth, BoardDate.OpponentNexusHealth) = GetNexusHealth(frames);
        }

        // Clean
        foreach (Image<Bgr, byte> frame in frames)
        {
            frame.Dispose();
        }

        bool isMyTurn = GameState is GameState.Attacking
            or GameState.Blocking
            or GameState.AttackTurn
            or GameState.DefendTurn;
        if (isMyTurn && BoardDate.Mana == -1)
        {
            _logger?.LogWarning("Can't recognize mana value.");
        }

        if (GameState != GameState.End)
        {
            return;
        }

        BoardDate.Cards.Clear();
        BoardDate.Mana = 0;
        BoardDate.SpellMana = 0;
    }

    /// <summary>
    /// Disposes the resources used by the state machine.
    /// </summary>
    public void Dispose()
    {
        _gameClientApi.Dispose();
        _ocrHelper.Dispose();
    }
}
