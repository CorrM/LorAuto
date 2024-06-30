using System.Diagnostics;
using System.Reflection;
using LorAuto.Plugin.Exceptions;
using LorAuto.Plugin.Model;
using LorAuto.Plugin.Types;

namespace LorAuto.Plugin.Holders;

internal sealed class DotnetPluginHolder : PluginHolder
{
    private readonly PluginLoadContext _loader;

    public DotnetPluginHolder(string pluginPath) : base(pluginPath)
    {
        _loader = new PluginLoadContext(pluginPath);
    }

    protected override Type GetPluginType()
    {
        // Check if dll is deleted
        if (!File.Exists(PluginPath))
            throw new FileNotFoundException("Plugin file not found", PluginPath);

        // Load plugin
        Assembly pluginAssembly;
        using (var pluginStream = new FileStream(PluginPath, FileMode.Open))
        {
            pluginAssembly = _loader.LoadFromStream(pluginStream);
        }

        // Get type of plugin
        Type cgPluginType = typeof(PluginBase);

        // Fetch all types that extends the plugin and are a class
        Type[] pluginTypes = pluginAssembly.GetTypes()
            .Where(p => cgPluginType.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract)
            .ToArray();

        string dllFileName = Path.GetFileName(PluginPath);
        switch (pluginTypes.Length)
        {
            case 0:
                throw new PluginNotValidException(dllFileName, PluginNotValidReason.EntryNotFound);

            case > 1:
                throw new PluginNotValidException(dllFileName, PluginNotValidReason.MultiEntry);
        }

        return pluginTypes[0];
    }

    protected override PluginInfo GetPluginInfo()
    {
        if (Instance is null)
            throw new UnreachableException();

        return Instance.PluginInformation;
    }

    protected override Version? GetTargetedSdkVersion()
    {
        if (PluginType is null)
            throw new UnreachableException("'Load' method must to be called first.");

        return Array.Find(
                PluginType.Assembly.GetReferencedAssemblies(),
                an => an.Name == GetType().Assembly.GetName().Name
            )
            ?.Version;
    }

    protected override PluginBase GetPluginInstance()
    {
        if (PluginType is null)
            throw new UnreachableException("'Load' method must to be called first.");

        var ret = (PluginBase?)Activator.CreateInstance(PluginType);
        if (ret is null)
            throw new PluginNotValidException(Id, PluginNotValidReason.CanNotCreateInstance);

        return ret;
    }

    public override void Dispose()
    {
        _loader.Unload();
    }
}
