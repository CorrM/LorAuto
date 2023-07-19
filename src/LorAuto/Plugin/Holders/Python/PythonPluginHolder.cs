using System.Diagnostics;
using LorAuto.Extensions;
using LorAuto.Plugin.Exceptions;
using LorAuto.Plugin.Model;
using LorAuto.Plugin.Types;
using Python.Runtime;

namespace LorAuto.Plugin.Holders.Python;

internal sealed class PythonPluginHolder : PluginHolder
{
    private readonly PyModule _pluginModule;
    private readonly PyObject _pyInstance;
    private readonly PluginInfo _pluginInfo;
    private readonly string _pluginName;

    public PythonPluginHolder(string pluginPath) : base(pluginPath)
    {
        using Py.GILState _ = Py.GIL();

        _pluginName = Path.GetFileNameWithoutExtension(pluginPath);
        _pluginModule = (PyModule)Py.Import(_pluginName);
        _pyInstance = _pluginModule.Get(_pluginName);

        using PyObject pluginInfo = _pyInstance.GetProperty(nameof(PluginBase.PluginInformation));
        _pluginInfo = pluginInfo.As<PluginInfo>();
    }

    protected override Type GetPluginType()
    {
        return _pluginInfo.PluginKind switch
        {
            EPluginKind.Unknown => throw new PluginNotValidException(_pluginName, PluginNotValidReason.UnknownPluginType),
            EPluginKind.Strategy => typeof(PythonStrategyPluginWrapper),
            _ => throw new UnreachableException()
        };
    }

    protected override PluginInfo GetPluginInfo()
    {
        return _pluginInfo;
    }

    protected override Version? GetTargetedSdkVersion()
    {
        return System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
    }

    protected override PluginBase GetPluginInstance()
    {
        return _pluginInfo.PluginKind switch
        {
            EPluginKind.Unknown => throw new PluginNotValidException(_pluginName, PluginNotValidReason.UnknownPluginType),
            EPluginKind.Strategy => (StrategyPlugin)Activator.CreateInstance(PluginType!, _pyInstance, _pluginInfo)!,
            _ => throw new UnreachableException()
        };
    }

    public override void Dispose()
    {
        using Py.GILState _ = Py.GIL();

        _pyInstance.InvokeMethod("dispose").Dispose();

        _pluginModule.Dispose();
        _pyInstance.Dispose();
    }
}
