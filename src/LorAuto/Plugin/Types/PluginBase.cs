using LorAuto.Plugin.Model;

namespace LorAuto.Plugin.Types;

/// <summary>
/// Base class for all plugins
/// </summary>
public abstract class PluginBase : IDisposable
{
    /// <summary>
    /// Gets the information about the plugin.
    /// </summary>
    public abstract PluginInfo PluginInformation { get; }

    /// <summary>
    /// Disposes the resources used by this instance.
    /// </summary>
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
