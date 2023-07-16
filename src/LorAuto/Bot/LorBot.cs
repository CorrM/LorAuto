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
    private readonly StateMachine _stateMachine;
    private readonly StrategyPlugin _strategy;
    private readonly EGameRotation _gameRotation;
    private readonly bool _isPvp;
    private readonly ILogger? _logger;
    private readonly PluginLoader _pluginsLoader;
    private readonly UserSimulator _userSimulator;

    /// <summary>
    /// Initializes a new instance of the <see cref="LorBot"/> class with the specified dependencies.
    /// </summary>
    /// <param name="stateMachine">The state machine for game state management.</param>
    /// <param name="strategyPluginName">The strategy plugin name to be used for decision making.</param>
    /// <param name="gameRotation">The game rotation to be used.</param>
    /// <param name="isPvp">Specifies whether the bot is playing against a human player.</param>
    /// <param name="logger">The logger for logging bot actions (optional).</param>
    public LorBot(StateMachine stateMachine, string strategyPluginName, EGameRotation gameRotation, bool isPvp, ILogger? logger)
    {
        _stateMachine = stateMachine;
        _gameRotation = gameRotation;
        _isPvp = isPvp;
        _logger = logger;

        _pluginsLoader = new PluginLoader();
        _userSimulator = new UserSimulator(_stateMachine);

        _strategy = GetStrategy(strategyPluginName);
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
        Dictionary<InGameCard, InGameCard> blockCards = _strategy.Block(_stateMachine.BoardDate, out List<CardTargetSelector>? spellsToUse);

        foreach ((InGameCard? myCard, InGameCard? opponentCard) in blockCards)
        {
            // Update before block
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);

            // Check if card is already blocked
            bool isBlockable = true;
            foreach (InGameCard allyCard in _stateMachine.BoardDate.Cards.CardsAttackOrBlock)
            {
                if (Math.Abs(allyCard.TopCenterPos.X - opponentCard.TopCenterPos.X) >= 10)
                    continue;

                isBlockable = false;
                break;
            }

            if (!isBlockable)
                continue;

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
        for (int i = 0; _stateMachine.GameState == EGameState.Blocking && i < 10; ++i)
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

        (InGameCard HandCard, CardTargetSelector? Target)? playHandCard = _strategy.PlayHandCard(
            _stateMachine.BoardDate,
            _stateMachine.GameState,
            _stateMachine.BoardDate.Mana,
            _stateMachine.BoardDate.SpellMana);

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
    private async Task<bool> PreDefendOrAttackAsync(EGameState gameState, CancellationToken ct = default)
    {
        // Update cards
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);

        // TODO: Add spell counter to strategy, as this condition is just skip opponent spells 
        if (_stateMachine.BoardDate.Cards.SpellStack.Count != 0 && _stateMachine.BoardDate.Cards.SpellStack.All(card => card.Type is EGameCardType.Spell or EGameCardType.Ability))
        {
            _userSimulator.CommitOrPassOrSkipTurn();
            await Task.Delay(_stateMachine.BoardDate.Cards.SpellStack.Count * 2500, ct).ConfigureAwait(false);

            return true;
        }

        // Determinate what to do
        EGamePlayAction gamePlayAction = gameState == EGameState.DefendTurn
            ? _strategy.RespondToOpponentAction(_stateMachine.BoardDate, _stateMachine.GameState, _stateMachine.BoardDate.Mana, _stateMachine.BoardDate.SpellMana)
            : _strategy.AttackTokenUsage(_stateMachine.BoardDate, _stateMachine.BoardDate.Mana, _stateMachine.BoardDate.SpellMana);
        if (gamePlayAction == EGamePlayAction.Skip)
        {
            _userSimulator.CommitOrPassOrSkipTurn();

            // Update cards
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
            return true;
        }

        // Play card from hand
        List<InGameCard> playableCards = _strategy.GetPlayableHandCards(_stateMachine.BoardDate.Cards, _stateMachine.BoardDate.Mana, _stateMachine.BoardDate.SpellMana);
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
        bool actionHandled = await PreDefendOrAttackAsync(EGameState.DefendTurn, ct).ConfigureAwait(false);
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
        bool actionHandled = await PreDefendOrAttackAsync(EGameState.Attacking, ct).ConfigureAwait(false);
        if (actionHandled)
        {
            _userSimulator.ResetMousePosition();
            return;
        }

        // Attack
        List<InGameCard> cardsToAttack = _strategy.Attack(_stateMachine.BoardDate, _stateMachine.BoardDate.Cards.CardsBoard);
        foreach (InGameCard atkCard in cardsToAttack)
        {
            _userSimulator.MoveBoardCardToField(atkCard);

            await Task.Delay(Random.Shared.Next(300, 400), ct).ConfigureAwait(false);

            // Update cards
            await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);
        }

        // Wait for any card effect
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
    public async Task ProcessAsync(CancellationToken ct = default)
    {
        // TODO: When attack then opponent set block cards then you have spell cards
        //       That will count as '_stateMachine.GameState' == 'EGameState.Blocking'
        await _stateMachine.UpdateGameDataAsync(ct).ConfigureAwait(false);

        _logger?.LogInformation("Current game state: {GameState}", _stateMachine.GameState);

        switch (_stateMachine.GameState)
        {
            case EGameState.None:
                return;

            case EGameState.Hold:
                await Task.Delay(3000, ct).ConfigureAwait(false);
                return;

            case EGameState.Menus:
                _userSimulator.SelectDeck(_gameRotation, _isPvp);
                // TODO: Add a way to detect 'EGameState.SearchGame' so this statement doesnt get called more than once
                break;

            case EGameState.MenusDeckSelected:
                _userSimulator.SelectDeck(_gameRotation, _isPvp);
                break;

            case EGameState.SearchGame:
                break;

            case EGameState.UserInteractNotReady:
                await Task.Delay(3000, ct).ConfigureAwait(false);
                return;

            case EGameState.Mulligan:
                await MulliganAsync(ct).ConfigureAwait(false);
                break;

            case EGameState.OpponentTurn:
                await Task.Delay(3000, ct).ConfigureAwait(false);
                return;

            case EGameState.DefendTurn:
                await DefendAsync(ct).ConfigureAwait(false);
                break;

            case EGameState.AttackTurn:
                await AttackAsync(ct).ConfigureAwait(false);
                break;

            case EGameState.Attacking:
                break;

            case EGameState.Blocking:
                await BlockAsync(ct).ConfigureAwait(false);
                break;

            case EGameState.End:
                _userSimulator.GameEndContinueAndReplay();
                break;

            default:
                throw new UnreachableException();
        }

        // Move mouse to center after each play 
        _userSimulator.ResetMousePosition();
    }

    /// <summary>
    /// Disposes the resources used by this instance.
    /// </summary>
    public void Dispose()
    {
        _pluginsLoader.Dispose();
    }
}
