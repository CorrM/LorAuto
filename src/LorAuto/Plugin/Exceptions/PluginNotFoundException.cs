namespace LorAuto.Plugin.Exceptions;

internal class PluginNotFoundException : Exception
{
    public override string Message { get; }

    public PluginNotFoundException(string pluginId, string? pluginPath = null)
    {
        Message = BuildMessage(pluginId, pluginPath);
    }

    private static string BuildMessage(string pluginId, string? pluginPath)
    {
        return $"Plugin '{pluginId}' not found." + (string.IsNullOrWhiteSpace(pluginPath) ? "" : $" Path({pluginPath})");
    }
}
