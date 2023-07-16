namespace LorAuto.Plugin.Model;

/// <summary>
/// Represents information about a plugin.
/// </summary>
public sealed class PluginInfo
{
    /// <summary>
    /// Gets or initializes the name of the plugin.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initializes the type of the plugin.
    /// </summary>
    public required EPluginKind Type { get; init; }

    /// <summary>
    /// Gets or initializes the description of the plugin.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets or initializes the optional source code link for the plugin.
    /// </summary>
    public required string? SourceCodeLink { get; init; }
}
