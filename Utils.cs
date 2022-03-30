using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leopotam.EcsLite;
using System.Threading;


public enum MetaType
{
    Component,
    Target
}

public class RunTask : Game.RunListener
{
    private TaskCompletionSource<bool> source = new TaskCompletionSource<bool>();
    private Boolean canceled = false;

    public Task<bool> Alive()
    {
        return source.Task;
    }

    public void Run()
    {
        var old = source;
        source = new TaskCompletionSource<bool>();
        old.SetResult(true);
    }

    public void Cancel()
    {
        var old = source;
        source = new TaskCompletionSource<bool>();
        old.SetResult(false);
    }
}

/*
    public async Task WaitSeconds(float seconds)
    {
        var filter = world.Filter<Tick>().End();

        var ticks = seconds * PhysicsSystem.TARGET_PHYSICS_FPS;

        while (ticks > 0 && await Runner.Alive())
        {
            ticks--;
        }
    }
*/

public class Entity : Utils.Cancellable
{
    public EcsWorld World;
    public int ID;

    private List<Utils.Cancellable> cancellables = new List<Utils.Cancellable>();

    public async Task<(int, T)> Removed<T>()
    {
        var (listener, task) = World.Removed<T>(ID);
        cancellables.Add(listener);
        var result = await task;
        cancellables.Remove(listener);
        return result;
    }

    public async Task<(int, T)> Added<T>()
    {
        var (listener, task) = World.Added<T>(ID);
        cancellables.Add(listener);
        var result = await task;
        cancellables.Remove(listener);
        return result;
    }

    public ref T Get<T>() where T : struct
    {
        return ref World.GetPool<T>().Get(ID);
    }

    public ref T Set<T>(T component) where T : struct
    {
        ref var current = ref World.GetPool<T>().Ensure(ID);
        current = component;
        return ref current;
    }

    public void Cancel()
    {
        foreach (var cancel in cancellables.ToList())
        {
            cancel.Cancel();
        }
    }
}

public static class Utils
{
    public static readonly List<Type> COMPONENTS = GetComponents().ToList();
    public static readonly List<Type> TARGETS = GetTargets().ToList();

    public static async Task<Entity> AttachEntity(this Godot.Object obj, EcsWorld world)
    {
        if (!obj.HasUserSignal("entity"))
        {
            obj.AddUserSignal("entity");
        }

        var id = obj.GetEntity(world);

        if (id == -1)
        {
            await obj.ToSignal(obj, "entity");

            id = obj.GetEntity(world);
        }

        return new Entity()
        {
            ID = id,
            World = world
        };
    }

    public static async Task<Task> Until(params Task[] tasks)
    {
        var result = await Task.WhenAny(tasks);
        if (result.Exception != null)
        {
            Console.WriteLine(result.Exception);
        }
        return result;
    }

    public static Task SafeCancel(this Task task)
    {
        return task.ContinueWith(action =>
            {
                foreach (var exception in action.Exception.Flatten().InnerExceptions)
                {
                    if (!(exception is OperationCanceledException))
                    {
                        throw action.Exception;
                    }
                }
            }, Game.GodotTasks.Scheduler);
    }

    public class AddListener<T> : IEcsWorldComponentListener<T>, Cancellable
    {
        public int[] Entities = new int[] { };

        private TaskCompletionSource<(int, T)> source = new TaskCompletionSource<(int, T)>();

        public Task<(int, T)> Find()
        {
            return source.Task;
        }

        public void OnComponentCreated(int entity, T component)
        {
            if (source != null && (Entities?.Length == 0 || Entities.Contains(entity)))
            {
                source.SetResult((entity, component));
                source = null;
            }
        }

        public void Cancel()
        {
            source?.SetException(new OperationCanceledException());
            source = null;
        }

        public void OnComponentDeleted(int entity, T component)
        {

        }
    }

    public interface Cancellable
    {
        void Cancel();
    }

    public class RemoveListener<T> : IEcsWorldComponentListener<T>, Cancellable
    {
        public int[] Entities = new int[] { };

        private TaskCompletionSource<(int, T)> source = new TaskCompletionSource<(int, T)>();

        public Task<(int, T)> Find()
        {
            return source.Task;
        }

        public void OnComponentCreated(int entity, T component)
        {

        }

        public void Cancel()
        {
            source?.SetException(new OperationCanceledException());
            source = null;
        }

        public void OnComponentDeleted(int entity, T component)
        {
            if (source != null && (Entities?.Length == 0 || Entities.Contains(entity)))
            {
                source.SetResult((entity, component));
                source = null;
            }
        }
    }

    public static (AddListener<T>, Task<(int, T)>) Added<T>(this EcsWorld world, params int[] entities)
    {
        var task = new AddListener<T>()
        {
            Entities = entities
        };

        world.AddComponentListener<T>(task);
        return (task, task.Find().ContinueWith(action =>
        {
            world.RemoveComponentListener<T>(task);
            return action.Result;
        }, Game.GodotTasks.Scheduler));
    }

    public static (RemoveListener<T>, Task<(int, T)>) Removed<T>(this EcsWorld world, params int[] entities)
    {
        var task = new RemoveListener<T>()
        {
            Entities = entities
        };

        world.AddComponentListener<T>(task);
        return (task, task.Find().ContinueWith(action =>
        {
            world.RemoveComponentListener<T>(task);
            return action.Result;
        }, Game.GodotTasks.Scheduler));
    }

    public static IEnumerable<T> NotNull<T>(this IEnumerable<T> collection)
    {
        return collection.Where(el => el != null);
    }

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
        return type.IsPrimitive || type == typeof(string) || type.IsEnum;
    }

    public static object Instantiate(Type baseComponentType)
    {
        var componentType = baseComponentType;
        var elementType = componentType;

        if (baseComponentType.HasEventHint())
        {
            componentType = typeof(Event<>).MakeGenericType(new[] { componentType });
            elementType = componentType;
        }

        if (baseComponentType.HasManyHint())
        {
            componentType = typeof(Many<>).MakeGenericType(new[] { componentType });
        }

        var component = Activator.CreateInstance(componentType);

        if (baseComponentType.HasManyHint())
        {
            var array = Array.CreateInstance(elementType, 1);
            array.SetValue(Activator.CreateInstance(elementType), 0);
            component = component.SetField("Items", array);
        }

        return component;
    }

    public static string ComponentName(object obj)
    {
        var type = obj.GetType();
        var name = type.Name.ToLower();
        var annotations = new List<string>();

        var isMany = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Many<>);
        if (isMany)
        {
            type = type.GetGenericArguments().First();
            name = type.Name.ToLower();
            annotations.Add("[]");
        }

        var isEvent = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Event<>);
        if (isEvent)
        {
            type = type.GetGenericArguments().First();
            name = type.Name.ToLower();
            annotations.Add("()");
        }

        return $"{name}{(String.Join("", annotations))}";
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

    public static Dictionary<string, object> ToMeta(this object component)
    {
        var type = component.GetType();
        var name = ComponentName(component);

        var isMany = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Many<>);
        if (isMany)
        {
            type = type.GetGenericArguments().First();
        }

        var isEvent = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Event<>);
        if (isEvent)
        {
            type = type.GetGenericArguments().First();
        }

        Func<object, Dictionary<string, object>> eventMeta = (object thisComponent) =>
        {
            var eventDict = new Dictionary<string, object>();

            var targetField = thisComponent.GetType().GetField("Target");
            var target = targetField.GetValue(thisComponent);
            var targetMeta = target?.ToMeta();
            eventDict["target"] = (targetMeta == null || targetMeta.Count == 0) ? false : targetMeta;

            var childComponentField = thisComponent.GetType().GetField("Component");
            var childComponent = childComponentField.GetValue(thisComponent);
            var childMeta = childComponent?.ToMeta();
            eventDict["component"] = (childMeta == null || childMeta.Count == 0) ? false : childMeta;

            return new Dictionary<string, object>() {
                { name, eventDict }
            }.ToFlat("/");
        };

        if (isMany)
        {
            var itemsField = component.GetType().GetField("Items");
            var array = itemsField.GetValue(component) as Array;
            if (array == null) array = Array.CreateInstance(type, 0);
            var manyDict = new Dictionary<string, object>();

            for (var i = 0; i < array.Length; i++)
            {
                var element = array.GetValue(i);
                var elementMeta = isEvent ? eventMeta(element) : element.ToMeta();
                var value = elementMeta.Select(entry =>
                   new KeyValuePair<string, object>(
                       String.Join("/", entry.Key.Split("/").Skip(1)),
                       entry.Value));
                manyDict.Add($"{i}", new Dictionary<string, object>(value));
            }

            return new Dictionary<string, object>() {
                { name, manyDict }
            }.ToFlat("/");
        }

        if (isEvent)
        {
            return eventMeta(component);
        }

        var values = component.ToFieldMap();
        if (values.Count > 0)
        {
            return new Dictionary<string, object>() {
                { name, values }
            }.ToFlat("/");
        }

        return new Dictionary<string, object>() {
            { name, true }
        }.ToFlat("/");
    }

    public static object[] ToComponents(this Godot.Object obj, string prefix, MetaType metaType = MetaType.Component)
    {
        var dict = new Dictionary<Type, object>();
        var metalist = obj.GetMetaList() ?? new string[] { };

        foreach (var meta in metalist.Where(part => part.StartsWith(prefix)))
        {
            var subpath = new string(meta.Skip(prefix.Length).ToArray())
                .Split('/')
                .Select(part => part.Trim())
                .Where(part => part.Length != 0)
                .ToArray();

            if (subpath.Length == 0) continue;

            var isMany = subpath[0].Contains("[]");
            var isEvent = subpath[0].Contains("()");

            var name = subpath[0].Replace("[]", string.Empty).Replace("()", string.Empty);
            Type componentType = null;

            switch (metaType)
            {
                case MetaType.Component:
                    {
                        componentType = COMPONENTS.FirstOrDefault(el => el.Name.ToLower() == name.ToLower());
                    }
                    break;
                case MetaType.Target:
                    {
                        componentType = TARGETS.FirstOrDefault(el => el.Name.ToLower() == name.ToLower());
                    }
                    break;
            }

            var eventType = typeof(Event<>).MakeGenericType(new[] { componentType });
            var elementType = isEvent ? eventType : componentType;
            var manyType = typeof(Many<>).MakeGenericType(new[] { elementType });
            var manyDictType = typeof(Dictionary<,>).MakeGenericType(new[] { typeof(string), elementType });

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
                if (isMany)
                {
                    component = Activator.CreateInstance(manyDictType);
                }
                else
                {
                    component = Activator.CreateInstance(type);
                }

                dict[type] = component;
            }

            if (isMany)
            {
                manyComponent = component;

                var key = subpath[1];
                var manyDict = manyComponent as IDictionary;

                component = manyDict.Contains(key)
                    ? manyDict[key]
                    : Activator.CreateInstance(elementType);
            }

            var fieldPath = String.Join('/', subpath.Skip(isMany ? 2 : 1));

            if (isEvent)
            {
                eventComponent = component;

                if (fieldPath.StartsWith("component/"))
                {
                    var eventComponentPath = new string(meta.SkipLast(fieldPath.Length).ToArray()) + "component";
                    component = obj.ToComponents(eventComponentPath).First();
                }
                else if (fieldPath.StartsWith("target/"))
                {
                    var eventTargetPath = new string(meta.SkipLast(fieldPath.Length).ToArray()) + "target";
                    component = obj.ToComponents(eventTargetPath, MetaType.Target).First();
                }
            }

            var value = obj.GetMeta(meta);

            try
            {
                component = SetField(component, fieldPath, value);
            }
            catch (Exception ex)
            {
                var objName = obj is Godot.Node node ? node.Name : obj.ToString();
                Console.WriteLine($"Unable to set {componentType.Name} {fieldPath} to '{value}' for '{objName}': {ex.Message}");
            }

            if (isEvent)
            {
                if (fieldPath.StartsWith("component/"))
                {
                    var fieldInfo = eventType.GetField("Component");
                    fieldInfo.SetValue(eventComponent, component);
                }
                else if (fieldPath.StartsWith("target/"))
                {
                    var fieldInfo = eventType.GetField("Target");
                    fieldInfo.SetValue(eventComponent, component);
                }
                component = eventComponent;
            }

            if (isMany)
            {
                var key = subpath[1];
                var manyDict = manyComponent as IDictionary;
                manyDict[key] = component;
                component = manyComponent;
            }

            dict[type] = component;
        }

        var components = dict.Values.ToArray();
        for (var i = 0; i < components.Length; i++)
        {
            ref var component = ref components[i];
            var type = component.GetType();
            if (type.FindInterfaces((intf, o) => intf == typeof(IDictionary), component).Count() > 0)
            {
                var manyDict = component as IDictionary;

                var elementType = manyDict.GetType().GenericTypeArguments[1];
                var manyType = typeof(Many<>).MakeGenericType(new[] { elementType });
                var array = Array.CreateInstance(elementType, manyDict.Count);
                manyDict.Values.CopyTo(array, 0);

                component = Activator.CreateInstance(manyType);
                component = component.SetField("Items", array);
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

    public static Type[] GetTargets()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.GetCustomAttributes(typeof(IsTarget), false)?.Length > 0)
            .OrderBy(component => component.Name)
            .ToArray();
    }

    private static Dictionary<Type, MethodInfo> getPoolMethodCache =
        new Dictionary<Type, MethodInfo>();

    private static Dictionary<Type, MethodInfo> addMethodCache =
        new Dictionary<Type, MethodInfo>();

    public static void AddNotify(this EcsWorld world, int entity, object component)
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
            addMethod = typeof(Utils).GetMethod("ReflectionAddNotify")
                .MakeGenericMethod(poolType);

            addMethodCache.Add(type, addMethod);
        }

        addMethod.Invoke(null, new[] { pool, entity, component });
    }

    public static void ReflectionAddNotify<T>(EcsPool<T> pool, int entity, T value)
        where T : struct
    {
        ref var reference = ref pool.Ensure(entity);
        pool.Notify<T>(entity);
        reference = value;
    }

    public static bool SafeHas<T>(this EcsPool<T> pool, int entity)
        where T : struct
    {
        return entity != -1 && pool.Has(entity);
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

    public static void SetEntity(this Godot.Object obj, EcsWorld world, int entity)
    {
        var packed = world.PackEntity(entity);
        obj.SetMeta("entity/id", packed.Id);
        obj.SetMeta("entity/gen", packed.Gen);

        if (!obj.HasUserSignal("entity"))
        {
            obj.AddUserSignal("entity");
        }

        obj.EmitSignal("entity", new object[] { });
    }

    public static int GetEntity(this Godot.Object obj, EcsWorld world)
    {
        if (obj == null ||
            !obj.HasMeta("entity/id") ||
            !obj.HasMeta("entity/gen"))
        {
            return -1;
        }

        EcsPackedEntity packed;
        packed.Id = Convert.ToInt32(obj.GetMeta("entity/id"));
        packed.Gen = Convert.ToInt32(obj.GetMeta("entity/gen"));

        int entityId = -1;
        packed.Unpack(world, out entityId);
        return entityId;
    }

    public static int[] Find(this EcsFilter filter)
    {
        var array = new int[filter.GetEntitiesCount()];

        var i = 0;
        foreach (var entity in filter)
        {
            array[i++] = entity;
        }

        return array;
    }

    public static int[] Find(this EcsWorld.Mask mask)
    {
        return mask.End().Find();
    }
}