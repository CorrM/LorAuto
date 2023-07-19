using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using LorAuto.Extensions;
using Python.Runtime;

namespace LorAuto.Plugin.Holders.Python;

/// <summary>
/// Marshal Python objects to .NET
/// </summary>
internal class GenericPyObjectDecoder : IPyObjectDecoder
{
    public bool CanDecode(PyType objectType, Type targetType)
    {
        if (targetType.IsAssignableTo(typeof(Exception)))
            return false;

        if (targetType.IsEnum)
            return false;

        return !targetType.IsPrimitive;
    }

    public bool TryDecode<T>(PyObject pyObj, [UnscopedRef] out T? value)
    {
        using Py.GILState _ = Py.GIL();

        Type targetType = typeof(T);
        PropertyInfo[] propertyInfos = targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        using PyList pyObjDir = pyObj.Dir();
        Dictionary<string, PyObject> propertiesNames = propertyInfos
            .Join(
                pyObjDir,
                prop => prop.Name,
                item => item.ToString(CultureInfo.InvariantCulture).Replace("_", ""),
                (prop, item) => new { Property = prop, Item = item },
                StringComparer.InvariantCultureIgnoreCase)
            .ToDictionary(x => x.Property.Name, x => x.Item);

        if (propertiesNames.Count == 0)
        {
            value = (T)(object)null!;
            return false;
        }

        value = Activator.CreateInstance<T>();

        foreach ((string propName, PyObject pyProperty) in propertiesNames)
        {
            PropertyInfo? propertyInfo = targetType.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (propertyInfo is null)
                throw new KeyNotFoundException($"'{propName}' property not found.");

            using PyObject pyPropValue = pyObj.GetAttr(pyProperty);

            object? propValue = propertyInfo.PropertyType.IsEnum
                ? pyPropValue.AsEnum(propertyInfo.PropertyType)
                : pyPropValue.AsManagedObject(propertyInfo.PropertyType);

            propertyInfo.SetValue(value, propValue);
        }

        foreach ((string _, PyObject pyProperty) in propertiesNames)
            pyProperty.Dispose();

        return true;
    }
}
