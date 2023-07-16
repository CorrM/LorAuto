using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using LorAuto.Plugin.Holders;
using LorAuto.Plugin.Model;
using LorAuto.Plugin.Types;

namespace LorAuto.Plugin;

internal sealed class PluginLoader : IDisposable
{
    private readonly string[] _pluginDirNames;
    private readonly Dictionary<string, PluginHolder> _plugins;

    public PluginLoader()
    {
        _pluginDirNames = new string[] { "Strategy" };
        _plugins = new Dictionary<string, PluginHolder>();

        Load();
    }

    private static bool IsDotnetAssembly(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        // Try to read CLI metadata from the PE file.
        using var peReader = new PEReader(fs);

        if (!peReader.HasMetadata)
            return false; // File does not have CLI metadata.

        try
        {
            // Check that file has an assembly manifest.
            MetadataReader reader = peReader.GetMetadataReader();
            return reader.IsAssembly;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
    }

    private IEnumerable<PluginHolder> GetPlugins()
    {
        return _plugins.Select(kv => kv.Value);
    }

    private IEnumerable<PluginHolder> GetPluginsByType(EPluginKind pluginKind)
    {
        return GetPlugins().Where(p => p.PluginKind == pluginKind);
    }

    private PluginHolder? GetPlugin(string pluginId)
    {
        return GetPlugins().FirstOrDefault(p => string.Equals(p.Id, pluginId, StringComparison.CurrentCultureIgnoreCase));
    }

    private void LoadPluginFromPath(string dllPath)
    {
        string dllName = Path.GetFileName(dllPath);

        // Remove old holders
        if (_plugins.TryGetValue(dllName, out PluginHolder? plugin))
        {
            plugin.Unload();
            _plugins.Remove(dllName);
        }

        bool isDotnetAssembly = IsDotnetAssembly(dllPath);
        plugin = isDotnetAssembly ? new DotnetPluginHolder(dllPath) : new PythonPluginHolder(dllPath);

        _plugins.Add(dllName, plugin);
    }

    private void CollectPlugins()
    {
        string pluginDir = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "Plugins");

        // Create directories
        foreach (string dirName in _pluginDirNames)
        {
            string pluginDirPath = Path.Combine(pluginDir, dirName);
            if (Directory.Exists(pluginDirPath))
                continue;

            Directory.CreateDirectory(pluginDirPath);
        }

        // Get dll from directory
        List<string> pluginsPaths = _pluginDirNames.SelectMany(dir => Directory.GetFiles(Path.Combine(pluginDir, dir)))
            .Where(f => f.EndsWith(".dll") || f.EndsWith(".py"))
            .GroupBy(Path.GetFileName)
            .Select(g => g.First())
            .ToList();

        foreach (string pluginPath in pluginsPaths)
            LoadPluginFromPath(pluginPath);
    }

    private void Load()
    {
        CollectPlugins();

        foreach (PluginHolder plugin in GetPlugins())
            plugin.Load();
    }

    public IEnumerable<PluginInfo> GetPluginsInfos()
    {
        return GetPlugins().Select(p => p.PluginInfo);
    }

    public IEnumerable<PluginInfo> GetPluginsInfosByType(EPluginKind pluginKind)
    {
        return GetPluginsByType(pluginKind).Select(p => p.PluginInfo);
    }

    public PluginBase? GetPluginInstance(string pluginId)
    {
        return GetPlugin(pluginId)?.Instance;
    }

    public void Dispose()
    {
        foreach (PluginHolder plugin in GetPlugins())
        {
            plugin.Unload();
            plugin.Dispose();
        }

        _plugins.Clear();
    }
}
