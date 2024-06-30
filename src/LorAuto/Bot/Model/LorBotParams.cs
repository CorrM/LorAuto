using LorAuto.Card;
using Microsoft.Extensions.Logging;

namespace LorAuto.Bot.Model;

/// <summary>
/// Represents the parameters for the LorBot.
/// </summary>
public readonly struct LorBotParams
{
    /// <summary>
    /// Gets or initializes the logger for logging bot actions.
    /// </summary>
    public ILogger? Logger { get; init; }

    /// <summary>
    /// Gets or initializes a value indicating whether to enable debug overlay.
    /// </summary>
    public bool DebugOverlay { get; init; }

    /// <summary>
    /// Gets or initializes the card sets manager.
    /// </summary>
    public required CardSetsManager CardSets { get; init; }

    /// <summary>
    /// Gets or initializes the game client port.
    /// </summary>
    public required int GamePort { get; init; }

    /// <summary>
    /// Gets or initializes the name of the strategy plugin to be used for decision-making.
    /// </summary>
    public required string StrategyPluginName { get; init; }

    /// <summary>
    /// Gets or initializes the game rotation to be used.
    /// </summary>
    public required GameRotation GameRotation { get; init; }

    /// <summary>
    /// Gets or initializes a value indicating whether the bot is playing against a human player.
    /// </summary>
    public required bool IsPvp { get; init; }
}
