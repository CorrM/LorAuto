using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using LorAuto.Extensions;
using LorAuto.Plugin.Exceptions;
using LorAuto.Plugin.Holders;
using LorAuto.Plugin.Holders.Python;
using LorAuto.Plugin.Model;
using LorAuto.Plugin.Types;
using Python.Runtime;

namespace LorAuto.Plugin;

internal sealed class PluginLoader : IDisposable
{
    private readonly bool _enablePython;
    private readonly string[] _pluginDirNames;
    private readonly string _pluginDir;
    private readonly Dictionary<string, PluginHolder> _plugins;
    private readonly IntPtr _beginAllowThreads;

    public PluginLoader(bool enablePython, string? pythonVersion)
    {
        _enablePython = enablePython;

        _pluginDirNames = new string[] { "Strategy" };
        _pluginDir = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "Plugins");
        _plugins = new Dictionary<string, PluginHolder>();

        if (enablePython)
        {
            if (string.IsNullOrWhiteSpace(pythonVersion))
                throw new InvalidOperationException("Python version not provided.");

            Runtime.PythonDLL = $"python{pythonVersion.Replace(".", "")}.dll";
            PythonEngine.Initialize();
            _beginAllowThreads = PythonEngine.BeginAllowThreads();

            PyObjectConversions.RegisterDecoder(new GenericPyObjectDecoder());
            //PyObjectConversions.RegisterEncoder(new GenericPyObjectEncoder());

            using Py.GILState _ = Py.GIL();

            using var sys = (PyModule)Py.Import("sys");
            using PyObject pathProp = sys.Get("path");

            // Add SDK
            string pySdkLoc = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!, "SDK", "Python");
            pathProp.InvokeMethod("insert", 0, pySdkLoc).Dispose();

            // Add plugins
            foreach (string pluginDirName in _pluginDirNames)
                pathProp.InvokeMethod("insert", 1, Path.Combine(_pluginDir, pluginDirName)).Dispose();
        }

        Load();
    }

    private static bool IsDotnetAssembly(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        // Try to read CLI metadata from the PE file.
        using var peReader = new PEReader(fs);

        try
        {
            if (!peReader.HasMetadata)
                return false; // File does not have CLI metadata.

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

    private void LoadPluginFromPath(string pluginPath)
    {
        string fileName = Path.GetFileName(pluginPath);

        // Remove old holders
        if (_plugins.TryGetValue(fileName, out PluginHolder? plugin))
        {
            plugin.Unload();
            _plugins.Remove(fileName);
        }

        if (_enablePython && fileName.EndsWith(".py", StringComparison.CurrentCultureIgnoreCase))
            plugin = new PythonPluginHolder(pluginPath);
        else if (IsDotnetAssembly(pluginPath))
            plugin = new DotnetPluginHolder(pluginPath);
        else
            throw new PluginNotValidException(fileName, PluginNotValidReason.UnknownPluginType);

        _plugins.Add(fileName, plugin);
    }

    private void CollectPlugins()
    {
        // Create directories
        foreach (string dirName in _pluginDirNames)
        {
            string pluginDirPath = Path.Combine(_pluginDir, dirName);
            if (Directory.Exists(pluginDirPath))
                continue;

            Directory.CreateDirectory(pluginDirPath);
        }

        // Get dll from directory
        List<string> pluginsPaths = _pluginDirNames.SelectMany(dir => Directory.GetFiles(Path.Combine(_pluginDir, dir)))
            .Where(f => f.EndsWith(".dll") || (_enablePython && f.EndsWith(".py")))
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
        PythonEngine.EndAllowThreads(_beginAllowThreads);

        PythonEngine.Shutdown();
    }
}
