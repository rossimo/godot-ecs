using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Leopotam.EcsLite;

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

    public static Dictionary<string, object> ToFlat(this Dictionary<string, object> dict, string sep, string prefix = "")
    {
        var output = new Dictionary<string, object>();

        foreach (var entry in dict)
        {
            var key = entry.Key;
            var value = entry.Value;
            var name = String.Join(sep, new[] { prefix, key }.Where(el => el.Length > 0));

            if (value is Dictionary<string, object> childDict)
            {
                foreach (var child in ToFlat(childDict, sep, name))
                {
                    output.Add(child.Key, child.Value);
                }
            }
            else
            {
                output.Add(name, value);
            }
        }

        return output;
    }

    public static object[] ToComponents(this Godot.Object obj)
    {
        var dict = new Dictionary<Type, object>();
        var metalist = obj.GetMetaList() ?? new string[] { };

        foreach (var meta in metalist)
        {
            var path = meta.Split('/');
            if (path.Length < 2) continue;

            var prefix = path[0];
            if (prefix != "components") continue;

            var isMany = path[1].Contains("[]");
            var isEvent = path[1].Contains("()");

            var name = path[1].Replace("[]", string.Empty).Replace("()", string.Empty);
            var componentType = COMPONENTS.FirstOrDefault(el => el.Name.ToLower() == name.ToLower());

            var eventType = typeof(Event<>).MakeGenericType(new[] { componentType });
            var elementType = isEvent ? eventType : componentType;
            var manyType = typeof(Many<>).MakeGenericType(new[] { elementType });

            if (componentType == null) continue;

            var type = componentType;
            if (isEvent)
            {
                type = eventType;
            }
            if (isMany)
            {
                type = manyType;
            }

            object component = null;
            object manyComponent = null;
            object eventComponent = null;

            if (dict.ContainsKey(type))
            {
                component = dict[type];
            }
            else
            {
                component = Activator.CreateInstance(type);

                if (isMany)
                {
                    component.SetField("Items", Array.CreateInstance(elementType, 0));
                }

                dict[type] = component;
            }

            if (isMany)
            {
                manyComponent = component;

                var index = Convert.ToInt32(path[2]);
                var array = type.GetField("Items").GetValue(component) as Array;

                component = index < array.Length
                    ? array.GetValue(index)
                    : Activator.CreateInstance(elementType);
            }

            if (isEvent)
            {
                eventComponent = component;

                var fieldInfo = component.GetType().GetField("Component");
                component = fieldInfo.GetValue(component);
                if (component == null)
                {
                    component = Activator.CreateInstance(componentType);
                }
            }

            var fieldPath = String.Join('/', path.Skip(isMany ? 3 : 2));
            var value = obj.GetMeta(meta);

            try
            {
                component = SetField(component, fieldPath, obj.GetMeta(meta));
            }
            catch (Exception ex)
            {
                var objName = obj is Godot.Node node ? node.Name : obj.ToString();
                Console.WriteLine($"Unable to set {componentType.Name} {fieldPath} to '{value}' for '{objName}': {ex.Message}");
            }

            if (isEvent)
            {
                var fieldInfo = eventType.GetField("Component");
                fieldInfo.SetValue(eventComponent, component);
                component = eventComponent;
            }

            if (isMany)
            {
                var key = Convert.ToInt32(path[2]);
                var fieldInfo = type.GetField("Items");
                var array = fieldInfo.GetValue(manyComponent) as Array;

                if (key >= array.Length)
                {
                    var expanded = Array.CreateInstance(elementType, key + 1);
                    Array.Copy(array, expanded, array.Length);
                    array = expanded;
                    fieldInfo.SetValue(manyComponent, array);
                }
                array.SetValue(component, key);
                component = manyComponent;
            }

            dict[type] = component;
        }

        var components = dict.Values.ToArray();
        for (var i = 0; i < components.Length; i++)
        {
            ref var component = ref components[i];
            var type = component.GetType();
            if (type.IsArray)
            {
                var array = component as Array;
                var prefix = $"components/{type.GetElementType().Name.ToLower()}";

                var indexes = Enumerable
                    .Range(0, array.Length)
                    .Where(index => metalist.Where(meta => meta.StartsWith($"{prefix}/{index}")).Count() > 0)
                    .ToArray();

                var trimmed = Array.CreateInstance(type.GetElementType(), indexes.Count());
                for (var j = 0; j < indexes.Count(); j++)
                {
                    var index = indexes[j];
                    trimmed.SetValue(array.GetValue(index), j);
                }
                component = trimmed;
            }
        }

        return components;
    }

    public static object SetField(this object obj, string path, object value)
    {
        var parts = path.Split('/');
        if (parts.Length == 0) return obj;

        var fieldInfo = obj.GetType().GetFields()
            .FirstOrDefault(el => el.Name.ToLower() == parts[0].ToLower());
        if (fieldInfo == null) return obj;

        var field = fieldInfo.GetValue(obj);

        if (parts.Length == 1)
        {
            var isConvertable = fieldInfo.FieldType
                .FindInterfaces((intf, o) => intf == typeof(IConvertible), value)
                .Count() > 0;

            var isArray = fieldInfo.FieldType.IsArray;

            object converted = null;

            if (isConvertable)
            {
                converted = Convert.ChangeType(value, fieldInfo.FieldType);
            }
            else if (isArray)
            {
                var length = (value as Array).Length;

                converted = Array.CreateInstance(
                    fieldInfo.FieldType.GetElementType(),
                    length);

                Array.Copy(value as Array, converted as Array, length);
            }

            fieldInfo.SetValue(obj, converted);
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
            .Where(type => type.GetCustomAttributes(typeof(Editor), false)?.Length > 0)
            .OrderBy(component => component.Name)
            .ToArray();
    }

    private static Dictionary<Type, MethodInfo> getPoolMethodCache =
        new Dictionary<Type, MethodInfo>();

    private static Dictionary<Type, MethodInfo> addMethodCache =
        new Dictionary<Type, MethodInfo>();

    public static void Add(this EcsWorld world, int entity, object component)
    {
        var type = component.GetType();
        var poolType = type;

        MethodInfo getPoolMethod;
        getPoolMethodCache.TryGetValue(type, out getPoolMethod);
        if (getPoolMethod == null)
        {
            getPoolMethod = typeof(EcsWorld).GetMethod("GetPool")
                .MakeGenericMethod(poolType);

            getPoolMethodCache.Add(type, getPoolMethod);
        }

        var pool = getPoolMethod.Invoke(world, null);

        MethodInfo addMethod;
        addMethodCache.TryGetValue(type, out addMethod);
        if (addMethod == null)
        {
            addMethod = typeof(Utils).GetMethod("ReflectionAdd")
                .MakeGenericMethod(poolType);

            addMethodCache.Add(type, addMethod);
        }

        addMethod.Invoke(null, new[] { pool, entity, component });
    }

    public static void ReflectionAdd<T>(EcsPool<T> pool, int entity, T value)
        where T : struct
    {
        ref var reference = ref pool.Ensure(entity);
        reference = value;
    }

    public static void ReflectionConcat<T>(EcsPool<Many<T>> pool, int entity, T value)
        where T : struct
    {
        ref var reference = ref pool.Concat(entity);
        reference = value;
    }

    public static bool HasManyHint(this Type type)
    {
        return type.GetCustomAttributes(typeof(IsMany), false)?.Length > 0;
    }

    public static bool HasEventHint(this Type type)
    {
        return type.GetCustomAttributes(typeof(IsEvent), false)?.Length > 0;
    }
}