using System.Diagnostics;
using LorAuto.Bot.Model;
using LorAuto.Card;
using LorAuto.Card.Model;
using LorAuto.Client;
using LorAuto.Client.Model;
using LorAuto.Plugin;
using LorAuto.Plugin.Types;
using Microsoft.Extensions.Logging;

namespace LorAuto.Bot;

// TODO: Get raid of all sleep, replace it with image processing
/*
do
{
    await Task.Delay(Random.Shared.Next(800, 1250), ct).ConfigureAwait(false);
    await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
} while (_stateMachine.GameState == EGameState.UserInteractNotReady);
*/

/// <summary>
/// Plays the game, responsible for executing commands from <see cref="StrategyPlugin"/>
/// </summary>
public sealed class LorBot : IDisposable
{
    private readonly StrategyPlugin _strategy;
    private readonly GameRotation _gameRotation;
    private readonly bool _isPvp;
    private readonly ILogger? _logger;
    private readonly PluginLoader _pluginsLoader;
    private readonly GameWindow _gameWindow;
    private readonly StateMachine _stateMachine;
    private readonly UserSimulator _userSimulator;
    private readonly DebugOverlay? _overlay;


    /// <summary>
    /// Initializes a new instance of the <see cref="LorBot"/> class with the specified dependencies.
    /// </summary>
    public LorBot(LorBotParams options)
    {
        _gameRotation = options.GameRotation;
        _isPvp = options.IsPvp;
        _logger = options.Logger;
        _pluginsLoader = new PluginLoader();
        _strategy = GetStrategy(options.StrategyPluginName);

        _gameWindow = new GameWindow();
        _stateMachine = new StateMachine(_logger, _gameWindow, options.CardSets, options.GamePort);
        _userSimulator = new UserSimulator(_gameWindow);

        if (options.DebugOverlay)
        {
            _overlay = new DebugOverlay(_gameWindow, _stateMachine);
        }
    }

    private StrategyPlugin GetStrategy(string strategyName)
    {
        if (_pluginsLoader.GetPluginInstance(strategyName) is not StrategyPlugin pluginInstance)
            throw new Exception($"Strategy '{strategyName}' not found.");

        _logger?.LogInformation("Bot start using '{Strategy}'", strategyName);
        return pluginInstance;
    }

    /// <summary>
    /// Performs the Mulligan phase of the game.
    /// </summary>
    /// <param name="ct">The cancellation token (optional).</param>
    private async Task MulliganAsync(CancellationToken ct = default)
    {
        // Wait before do any action
        while (_stateMachine.BoardDate.Cards.CardsMulligan.Count != 4)
        {
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
            await Task.Delay(1000, ct).ConfigureAwait(false);
        }

        IEnumerable<InGameCard> cardsToReplace = _strategy.Mulligan(_stateMachine.BoardDate.Cards.CardsMulligan);
        foreach (InGameCard card in cardsToReplace)
        {
            if (!_stateMachine.BoardDate.Cards.CardsMulligan.Contains(card))
                continue;

            _userSimulator.ClickCard(card);
            await Task.Delay(Random.Shared.Next(200, 400), ct).ConfigureAwait(false);
        }

        _userSimulator.CommitOrPassOrSkipTurn();
        _userSimulator.ResetMousePosition();

        // Wait mulligan
        for (int i = 0; _stateMachine.BoardDate.Cards.CardsMulligan.Count == 0 && i < 10; ++i)
        {
            await Task.Delay(1000, ct).ConfigureAwait(false);
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Performs the Block phase of the game.
    /// </summary>
    /// <param name="ct">The cancellation token (optional).</param>
    private async Task BlockAsync(CancellationToken ct = default)
    {
        Dictionary<InGameCard, InGameCard> blockCards = _strategy.Block(
            _stateMachine.BoardDate,
            out List<CardTargetSelector>? spellsToUse
        );

        foreach ((InGameCard? myCard, InGameCard? opponentCard) in blockCards)
        {
            // Update before block
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);

            // Check if card is already blocked
            bool isBlocking = true;
            foreach (InGameCard allyCard in _stateMachine.BoardDate.Cards.CardsAttackOrBlock)
            {
                if (Math.Abs(allyCard.TopCenterPos.X - opponentCard.TopCenterPos.X) >= 10)
                    continue;

                isBlocking = false;
                break;
            }

            if (!isBlocking)
            {
                continue;
            }

            _userSimulator.BlockCard(myCard, opponentCard);
            await Task.Delay(Random.Shared.Next(600, 800), ct).ConfigureAwait(false);
        }

        if (spellsToUse is not null)
        {
            foreach (CardTargetSelector selector in spellsToUse)
            {
                await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
                _userSimulator.PlayCardFromHand(selector.GetSelectedCard(), selector);

                await Task.Delay(Random.Shared.Next(600, 800), ct).ConfigureAwait(false);
            }
        }

        _userSimulator.CommitOrPassOrSkipTurn();
        _userSimulator.ResetMousePosition();

        // TODO: Should beak from that loop, if the opponent uses a spell while me blocking and i have a spell to response
        for (int i = 0; _stateMachine.GameState == GameState.Blocking && i < 10; ++i)
        {
            await Task.Delay(1000, ct).ConfigureAwait(false);
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Plays a card from the hand.
    /// </summary>
    /// <param name="ct">The cancellation token (optional).</param>
    /// <returns><c>true</c> if a card is played; otherwise, <c>false</c>.</returns>
    private async Task<bool> PlayCardFromHandAsync(CancellationToken ct = default)
    {
        // Update cards
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);

        // TODO: What if card can predict
        // TODO: What if card have To Play pick one option like (PETTY OFFICER)
        (InGameCard HandCard, CardTargetSelector? Target)? playHandCard = _strategy.PlayHandCard(
            _stateMachine.BoardDate,
            _stateMachine.GameState,
            _stateMachine.BoardDate.Mana,
            _stateMachine.BoardDate.SpellMana
        );

        if (playHandCard is null)
        {
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
            return false;
        }

        _userSimulator.PlayCardFromHand(playHandCard.Value.HandCard, playHandCard.Value.Target);
        _userSimulator.ResetMousePosition();

        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Handles stuff needs to be done before the DefendTurn or AttackTurn phase of the game.
    /// </summary>
    /// <param name="gameState">The current game state.</param>
    /// <param name="ct">The cancellation token (optional).</param>
    /// <returns><c>true</c> if an action is handled; otherwise, <c>false</c>.</returns>
    private async Task<bool> PreDefendOrAttackAsync(GameState gameState, CancellationToken ct = default)
    {
        // Update cards
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);

        // TODO: Add spell counter to strategy, as this condition is just skip opponent spells 
        if (_stateMachine.BoardDate.Cards.SpellStack.Count != 0 &&
            _stateMachine.BoardDate.Cards.SpellStack.TrueForAll(
                card => card.Type is EGameCardType.Spell or EGameCardType.Ability
            ))
        {
            _userSimulator.CommitOrPassOrSkipTurn();
            await Task.Delay(_stateMachine.BoardDate.Cards.SpellStack.Count * 2500, ct).ConfigureAwait(false);

            return true;
        }

        // Determinate what to do
        EGamePlayAction gamePlayAction = gameState == GameState.DefendTurn
            ? _strategy.RespondToOpponentAction(
                _stateMachine.BoardDate,
                _stateMachine.GameState,
                _stateMachine.BoardDate.Mana,
                _stateMachine.BoardDate.SpellMana
            )
            : _strategy.AttackTokenUsage(
                _stateMachine.BoardDate,
                _stateMachine.BoardDate.Mana,
                _stateMachine.BoardDate.SpellMana
            );
        if (gamePlayAction == EGamePlayAction.Skip)
        {
            _userSimulator.CommitOrPassOrSkipTurn();

            // Update cards
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
            return true;
        }

        // Play card from hand
        List<InGameCard> playableCards = _strategy.GetPlayableHandCards(
            _stateMachine.BoardDate.Cards,
            _stateMachine.BoardDate.Mana,
            _stateMachine.BoardDate.SpellMana
        );
        if (playableCards.Count <= 0 || gamePlayAction != EGamePlayAction.PlayCards)
            return false;

        bool thereCardPlayed = await PlayCardFromHandAsync(ct).ConfigureAwait(false);
        if (!thereCardPlayed)
            return false;

        // Update cards
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// Handles the DefendTurn phase of the game.
    /// </summary>
    /// <param name="ct">The cancellation token (optional).</param>
    private async Task DefendAsync(CancellationToken ct = default)
    {
        bool actionHandled = await PreDefendOrAttackAsync(GameState.DefendTurn, ct).ConfigureAwait(false);
        if (actionHandled)
        {
            _userSimulator.ResetMousePosition();
            return;
        }

        // Any other 'gamePlayAction' or there is no playable cards
        _userSimulator.CommitOrPassOrSkipTurn();
        _userSimulator.ResetMousePosition();
    }

    /// <summary>
    /// Handles the AttackTurn phase of the game.
    /// </summary>
    /// <param name="ct">The cancellation token (optional).</param>
    private async Task AttackAsync(CancellationToken ct = default)
    {
        bool actionHandled = await PreDefendOrAttackAsync(GameState.Attacking, ct).ConfigureAwait(false);
        if (actionHandled)
        {
            _userSimulator.ResetMousePosition();
            return;
        }

        // Attack
        List<InGameCard> cardsToAttack = _strategy.Attack(
            _stateMachine.BoardDate,
            _stateMachine.BoardDate.Cards.CardsBoard
        );
        foreach (InGameCard atkCard in cardsToAttack)
        {
            _userSimulator.MoveBoardCardToField(atkCard);

            await Task.Delay(Random.Shared.Next(300, 400), ct).ConfigureAwait(false);

            // Update cards
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
        }

        // Wait for any card effect, TODO: Need to be enhanced
        await Task.Delay(2000, ct).ConfigureAwait(false);

        // Submit attack
        _userSimulator.CommitOrPassOrSkipTurn();
        _userSimulator.ResetMousePosition();

        await Task.Delay(4000, ct).ConfigureAwait(false);

        // Update cards
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Processes the game and performs appropriate actions based on the current game state.
    /// </summary>
    /// <param name="ct">The cancellation token (optional).</param>
    public async Task<bool> ProcessAsync(CancellationToken ct = default)
    {
        _gameWindow.UpdateClientInfo();
        if (_gameWindow.GameWindowHandle == nint.Zero)
        {
            _logger?.LogError("Legends of Runeterra isn't running!, please launch it.");
            return false;
        }

        // TODO: If you attack and have spell cards, while opponent set a block cards, 
        //       That will count as 'GameState.Blocking'
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);

        _logger?.LogInformation("Current game state: {GameState}", _stateMachine.GameState);
        // TODO: Add a way to detect 'GameState.SearchGame' so this statement doesnt get called more than once
        switch (_stateMachine.GameState)
        {
            case GameState.None:
                return false;

            case GameState.Hold:
                await Task.Delay(3000, ct).ConfigureAwait(false);
                return true;

            case GameState.Menus:
            case GameState.MenusDeckSelected:
                _userSimulator.SelectGameRotation(_gameRotation, _isPvp);
                _userSimulator.SelectDeck(0);
                break;

            case GameState.SearchGame:
                break;

            case GameState.UserInteractNotReady:
                await Task.Delay(3000, ct).ConfigureAwait(false);
                return true;

            case GameState.Mulligan:
                await MulliganAsync(ct).ConfigureAwait(false);
                break;

            case GameState.OpponentTurn:
                await Task.Delay(3000, ct).ConfigureAwait(false);
                return true;

            case GameState.DefendTurn:
                await DefendAsync(ct).ConfigureAwait(false);
                break;

            case GameState.AttackTurn:
                await AttackAsync(ct).ConfigureAwait(false);
                break;

            case GameState.Attacking:
                break;

            case GameState.Blocking:
                await BlockAsync(ct).ConfigureAwait(false);
                break;

            case GameState.End:
                _userSimulator.GameEndContinueAndReplay(_stateMachine);
                break;

            default:
                throw new UnreachableException();
        }

        _userSimulator.ResetMousePosition();
        return true;
    }

    /// <summary>
    /// Disposes the resources used by this instance.
    /// </summary>
    public void Dispose()
    {
        _pluginsLoader.Dispose();
        _stateMachine.Dispose();
        _overlay?.Dispose();
    }
}
