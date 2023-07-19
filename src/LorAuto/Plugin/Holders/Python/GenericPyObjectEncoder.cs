using System.Collections;
using Python.Runtime;

namespace LorAuto.Plugin.Holders.Python;

/// <summary>
/// Marshal .NET objects to Python
/// </summary>
internal class GenericPyObjectEncoder : IPyObjectEncoder
{
    public bool CanEncode(Type type)
    {
        if (type.IsAssignableTo(typeof(IEnumerable)))
            return true;

        return false;
    }

    public PyObject? TryEncode(object value)
    {
        PyObject fromManagedObject = PyObject.FromManagedObject(value);
        return fromManagedObject;
    }
}
