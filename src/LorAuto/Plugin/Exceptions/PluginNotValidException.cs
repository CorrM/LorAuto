namespace LorAuto.Plugin.Exceptions;

internal enum PluginNotValidReason
{
    EntryNotFound,
    NativeEntryNotFound,
    MultiEntry,
    LoadMethodNotFound,
    UnloadMethodNotFound,
    OutdatedSdk,
    CanNotCreateInstance,
    InfoNotFound,
    UnknownPluginType,
    InvalidInfoSourceCodeLink
}

internal class PluginNotValidException : Exception
{
    public override string Message { get; }

    public PluginNotValidException(string pluginId, PluginNotValidReason reason)
    {
        Message = BuildMessage(pluginId, reason);
    }

    private static string BuildMessage(string pluginId, PluginNotValidReason reason)
    {
        return reason switch
        {
            PluginNotValidReason.EntryNotFound => $"'{pluginId}' Doesn't have any class that inherits from 'PluginBase'.",
            PluginNotValidReason.NativeEntryNotFound => $"'{pluginId}' Doesn't have any exported entrypoint.",
            PluginNotValidReason.MultiEntry => $"'{pluginId}' Have multi class that inherits from 'PluginBase'.",
            PluginNotValidReason.LoadMethodNotFound => $"'{pluginId}' Doesn't have 'OnLoad' method.",
            PluginNotValidReason.UnloadMethodNotFound => $"'{pluginId}' Doesn't have 'OnUnload' method.",
            PluginNotValidReason.OutdatedSdk => $"'{pluginId}' Targeting outdated CheatGear SDK.",
            PluginNotValidReason.CanNotCreateInstance => $"'{pluginId}' Can't create an instance of a plugin.",
            PluginNotValidReason.InfoNotFound => $"'{pluginId}' Doesn't have a 'PluginInfo' attribute.",
            PluginNotValidReason.UnknownPluginType => $"Can't detect plugin type of '{pluginId}'.",
            PluginNotValidReason.InvalidInfoSourceCodeLink => $"'{pluginId}' have invalid sourcecode link.",
            _ => throw new InvalidOperationException()
        };
    }
}
