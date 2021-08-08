using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

public static class Utils
{
    public static readonly List<Type> COMPONENTS = GetComponents().ToList();

    public static T[] ToArray<T>(this Godot.Collections.Array array)
    {
        var list = new List<T>();

        foreach (T element in array)
        {
            list.Add(element);
        }

        return list.ToArray();
    }

    public static Dictionary<string, object> ToFieldMap(this object obj)
    {
        var type = obj.GetType();
        var metadata = new Dictionary<string, object>();

        foreach (var fieldInfo in type.GetFields())
        {
            var key = fieldInfo.Name.ToLower();
            var value = fieldInfo.GetValue(obj);
            var fieldType = fieldInfo.FieldType;

            if (fieldType.IsEditable())
            {
                metadata.Add(key, value);
            }
            else
            {
                metadata.Add(key, value.ToFieldMap());
            }
        }

        return metadata;
    }

    public static bool IsEditable(this Type type)
    {
        return type.IsPrimitive || type == typeof(string);
    }

    public static Dictionary<string, object> ToFlat(this Dictionary<string, object> input, string sep, string prefix = "")
    {
        var output = new Dictionary<string, object>();

        foreach (var entry in input)
        {
            var key = entry.Key;
            var value = entry.Value;
            var name = String.Join(sep, new[] { prefix, key }.Where(el => el.Length > 0));

            if (value is Dictionary<string, object> inner)
            {
                foreach (var innerEntry in ToFlat(inner, sep, name))
                {
                    var innerKey = innerEntry.Key;
                    var innerValue = innerEntry.Value;

                    output.Add(innerKey, innerValue);
                }
            }
            else
            {
                output.Add(name, value);
            }
        }

        return output;
    }

    public static object[] ToComponents(this Godot.Node node)
    {
        var components = new Dictionary<Type, object>();

        foreach (var key in node.GetMetaList() ?? new string[] { })
        {
            var path = key.Split('/');
            if (path.Length < 2) continue;

            var prefix = path[0];
            if (path[0] != "components") continue;

            var name = path[1];
            var type = COMPONENTS.FirstOrDefault(el => el.Name.ToLower() == name.ToLower());
            if (type == null) continue;

            var component = components.ContainsKey(type)
                ? components[type]
                : Activator.CreateInstance(type);

            component = SetField(component, String.Join('/', path.Skip(2)), node.GetMeta(key));

            components[type] = component;
        }

        return components.Values.ToArray();
    }

    public static object SetField(object obj, string path, object value)
    {
        var parts = path.Split('/');
        if (parts.Length == 0) return obj;

        var fieldInfo = obj.GetType().GetFields()
            .FirstOrDefault(el => el.Name.ToLower() == parts[0].ToLower());
        if (fieldInfo == null) return obj;

        var field = fieldInfo.GetValue(obj);

        if (parts.Length == 1)
        {
            fieldInfo.SetValue(obj, Convert.ChangeType(value, fieldInfo.FieldType));
        }
        else
        {
            fieldInfo.SetValue(obj, SetField(field, String.Join('/', parts.Skip(1)), value));
        }

        return obj;
    }

    public static Type[] GetComponents()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.GetCustomAttributes(typeof(EditorComponent), false)?.Length > 0)
            .OrderBy(component => component.Name)
            .ToArray();
    }
}