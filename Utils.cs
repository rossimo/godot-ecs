using System;
using System.Linq;
using System.Collections.Generic;

public static class Utils
{
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

            if (fieldType.IsPrimitive || value is string)
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
}