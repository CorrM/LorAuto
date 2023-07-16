using System.Reflection;
using System.Runtime.Loader;

// https://docs.microsoft.com/en-us/dotnet/core/dependency-loading/overview

namespace LorAuto.Plugin;

internal sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly string _pluginDir;
    private readonly string _pluginName;
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(Path.GetFileNameWithoutExtension(pluginPath), true)
    {
        _pluginDir = Path.GetDirectoryName(pluginPath)!;
        _pluginName = Path.GetFileNameWithoutExtension(pluginPath);
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    private Assembly? LoadSameAssemblyCheatGearLoaded(AssemblyName assemblyName)
    {
        return base.Load(assemblyName);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (string.IsNullOrWhiteSpace(assemblyPath))
            return LoadSameAssemblyCheatGearLoaded(assemblyName);

        if (assemblyName.Name == GetType().Assembly.GetName().Name)
            return LoadSameAssemblyCheatGearLoaded(assemblyName);

        // Load managed lib API from 'plugin name' folder ex: 'Generic' folder inside 'Plugins'
        assemblyPath = Path.Combine(_pluginDir, _pluginName, Path.GetFileName(assemblyPath));
        return File.Exists(assemblyPath)
            ? LoadFromAssemblyPath(assemblyPath)
            : LoadSameAssemblyCheatGearLoaded(assemblyName);
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        throw new Exception("TODO");
        //string? libraryPath = _resolver.ResolveUnmanagedDllToPath(Path.Combine(_pluginDir, _pluginName, unmanagedDllName));
        //return !string.IsNullOrWhiteSpace(libraryPath)
        //    ? LoadUnmanagedDllFromPath(libraryPath)
        //    : nint.Zero;
    }
}
