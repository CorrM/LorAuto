using LorAuto.Client;
using Microsoft.Extensions.Logging;

namespace LorAuto.Bot.Model;

/// <summary>
/// Represents the parameters for the LorBot.
/// </summary>
public readonly struct LorBotParams
{
    /// <summary>
    /// Gets or initializes the state machine for game state management.
    /// </summary>
    public required StateMachine StateMachine { get; init; }

    /// <summary>
    /// Gets or initializes the name of the strategy plugin to be used for decision making.
    /// </summary>
    public required string StrategyPluginName { get; init; }

    /// <summary>
    /// Gets or initializes the game rotation to be used.
    /// </summary>
    public required EGameRotation GameRotation { get; init; }

    /// <summary>
    /// Gets or initializes a value indicating whether the bot is playing against a human player.
    /// </summary>
    public required bool IsPvp { get; init; }

    /// <summary>
    /// Gets or initializes a value indicating whether to enable Python plugins.
    /// </summary>
    public required bool EnablePythonPlugins { get; init; }

    /// <summary>
    /// Gets or initializes the version of Python to be used with the plugins.
    /// </summary>
    public required string? PythonVersion { get; init; }

    /// <summary>
    /// Gets or initializes the logger for logging bot actions (optional).
    /// </summary>
    public required ILogger? Logger { get; init; }
}
