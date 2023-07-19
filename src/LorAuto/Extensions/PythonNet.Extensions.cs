using Python.Runtime;

namespace LorAuto.Extensions;

internal static class PythonNetExtensions
{
    public static PyObject Invoke(this PyObject method, params object[] args)
    {
        bool isCallable = method.IsCallable();
        if (!isCallable)
            throw new ArgumentException("This python object are not a method.", nameof(method));

        PyObject[] pyArgs = args.Select(a => a.ToPython()).ToArray();
        PyObject ret = method.Invoke(pyArgs);

        foreach (PyObject pyArg in pyArgs)
            pyArg.Dispose();

        return ret;
    }

    public static T Invoke<T>(this PyObject method, params object[] args)
    {
        return Invoke(method, args).As<T>();
    }

    public static PyObject InvokeMethod(this PyObject obj, string methodName, params object[] args)
    {
        bool isCallable = obj.GetAttr(methodName).IsCallable();
        if (!isCallable)
            throw new ArgumentException($"This python object doesnt have a method called '{methodName}'.", nameof(obj));

        var pyArgs = new PyObject[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            PyObject pyArg = args[i] as PyObject ?? args[i].ToPython();
            pyArgs[i] = pyArg;
        }

        PyObject ret = obj.InvokeMethod(methodName, pyArgs);

        for (int i = 0; i < pyArgs.Length; i++)
        {
            bool originalArgIsPyObj = args[i] is PyObject;
            if (originalArgIsPyObj)
                continue;

            PyObject pyArg = pyArgs[i];
            pyArg.Dispose();
        }

        return ret;
    }

    public static PyObject GetProperty(this PyObject obj, string property)
    {
        using PyObject pluginInfoInstance = obj.GetAttr(property);
        return pluginInfoInstance.InvokeMethod("fget", pluginInfoInstance);
    }

    public static T GetProperty<T>(this PyObject obj, string property)
    {
        return GetProperty(obj, property).As<T>();
    }

    public static object AsEnum(this PyObject enumValue, Type enumType)
    {
        using PyObject pyEnumValue = enumValue.GetAttr("name");
        return Enum.Parse(enumType, pyEnumValue.As<string>());
    }

    public static T AsEnum<T>(this PyObject enumValue) where T : struct
    {
        return (T)AsEnum(enumValue, typeof(T));
    }
}
