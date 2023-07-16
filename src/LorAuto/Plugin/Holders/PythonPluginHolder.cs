using LorAuto.Plugin.Model;
using LorAuto.Plugin.Types;

namespace LorAuto.Plugin.Holders;

internal sealed class PythonPluginHolder : PluginHolder
{
    public PythonPluginHolder(string pluginPath) : base(pluginPath)
    {
        throw new NotImplementedException();
    }

    protected override Type GetPluginType()
    {
        throw new NotImplementedException();
    }

    protected override PluginInfo GetPluginInfo()
    {
        throw new NotImplementedException();
    }

    protected override Version? GetTargetedSdkVersion()
    {
        throw new NotImplementedException();
    }

    protected override PluginBase GetPluginInstance()
    {
        throw new NotImplementedException();
    }

    public override void Dispose()
    {
        throw new NotImplementedException();
    }
}
