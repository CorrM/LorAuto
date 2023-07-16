using LorAuto.Plugin.Exceptions;
using LorAuto.Plugin.Model;
using LorAuto.Plugin.Types;

namespace LorAuto.Plugin.Holders;

internal abstract class PluginHolder : IDisposable
{
    protected Type? PluginType { get; private set; }

    public string Id { get; }
    public string PluginPath { get; }
    public PluginInfo PluginInfo { get; private set; } = null!;
    public EPluginKind PluginKind { get; private set; } = EPluginKind.Unknown;
    public PluginBase? Instance { get; private set; }

    protected PluginHolder(string pluginPath)
    {
        Id = Path.GetFileNameWithoutExtension(pluginPath);
        PluginPath = pluginPath;
    }

    private EPluginKind GetPluginKind()
    {
        if (PluginType is null)
            throw new Exception($"'{nameof(Load)}' should be called first.");

        if (PluginType.BaseType is null)
            return EPluginKind.Unknown;

        if (PluginType.IsAssignableTo(typeof(StrategyPlugin)))
            return EPluginKind.Strategy;

        return EPluginKind.Unknown;
    }

    protected abstract Type GetPluginType();

    protected abstract PluginInfo GetPluginInfo();

    protected abstract Version? GetTargetedSdkVersion();

    protected abstract PluginBase GetPluginInstance();

    public void Load()
    {
        if (Instance is not null)
            throw new Exception($"Plugin '{Id}' already loaded.");

        // ! Don't change order of these function calls
        PluginType = GetPluginType();
        Instance = GetPluginInstance();
        PluginKind = GetPluginKind();
        PluginInfo = GetPluginInfo();

        if (PluginKind == EPluginKind.Unknown)
            throw new PluginNotValidException(Id, PluginNotValidReason.UnknownPluginType);

        if (PluginInfo is null)
            throw new PluginNotValidException(Id, PluginNotValidReason.InfoNotFound);

        if (!string.IsNullOrWhiteSpace(PluginInfo.SourceCodeLink) && !Uri.IsWellFormedUriString(PluginInfo.SourceCodeLink, UriKind.Absolute))
            throw new PluginNotValidException(Id, PluginNotValidReason.InvalidInfoSourceCodeLink);

        // Check targeted SDK version
        Version? targetedSdkVersion = GetTargetedSdkVersion();
        if (targetedSdkVersion is null)
            throw new PluginNotValidException(Id, PluginNotValidReason.UnknownPluginType);
    }

    public void Unload()
    {
        if (Instance is null)
            return;

        Instance.Dispose();
        Instance = null;
    }

    public abstract void Dispose();
}
