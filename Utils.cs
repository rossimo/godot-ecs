using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leopotam.EcsLite;
using System.Threading;
using Godot;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class Hidden : Attribute
{

}

public enum MetaType
{
    Component,
    Target
}

public class Entity
{
    readonly public EcsWorld World;
    readonly public EcsPackedEntity Self;

    public Entity(EcsWorld world, int id)
    {
        World = world;
        Self = world.PackEntity(id);
    }

    private int GetId()
    {
        if (!Self.Unpack(World, out int id))
        {
            throw new MissingEntityException(Self);
        }
        return id;
    }

    public Task<T> Removed<T>(CancellationToken token)
    {
        GetId();

        return World.Removed<T>(token, Self).ContinueWith(result =>
        {
            return result.Result.Item2;
        }, TaskContinuationOptions.ExecuteSynchronously);
    }

    public Task<T> Added<T>(CancellationToken token)
    {
        GetId();

        return World.Added<T>(token, Self).ContinueWith(result =>
        {
            return result.Result.Item2;
        }, TaskContinuationOptions.ExecuteSynchronously);
    }

    public ref T Get<T>() where T : struct
    {
        return ref World.GetPool<T>().Get(GetId());
    }

    public ref T Set<T>(T component) where T : struct
    {
        ref var current = ref World.GetPool<T>().Ensure(GetId());
        current = component;
        return ref current;
    }

    public ref T SetAndNotify<T>(T component) where T : struct
    {
        ref var current = ref this.Set(component);
        World.AddNotify(GetId(), component);
        return ref current;
    }

    public TaskChain WhenAny(CancellationToken token)
    {
        return new TaskChain(this, token);
    }
}

public struct Resolve<T> {
    public T Component;
}

public struct Rejected<T> {
    public T Component;
    public Exception Exception;
}

public class TaskWrapper
{
    public Task Task = null;
}

public class TaskChain
{
    private readonly Entity entity;
    private readonly CancellationToken token;
    private readonly List<Func<CancellationToken, Task>> links = new();

    public TaskChain(Entity entity, CancellationToken token)
    {
        this.entity = entity;
        this.token = token;
    }

    public TaskChain Added<T>()
    {
        links.Add((CancellationToken token) =>
        {
            return entity.Added<T>(this.token);
        });

        return this;
    }

    public TaskChain Removed<T>()
    {
        links.Add((CancellationToken token) =>
        {
            return entity.Removed<T>(this.token);
        });

        return this;
    }

    public async Task<Task> Task()
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(token);

        try
        {
            var tasks = links.Select(link => link(token));
            return await System.Threading.Tasks.Task.WhenAny(tasks);
        }
        finally
        {
            source.Cancel();
        }
    }
}

public class MissingEntityException : Exception
{
    private EcsPackedEntity packed;

    public MissingEntityException(EcsPackedEntity packed) : base($"Entity {packed.Id} is missing")
    {
        this.packed = packed;
    }

    public EcsPackedEntity Packed
    {
        get
        {
            return this.packed;
        }
    }
}

public static class Utils
{
    public static readonly List<Type> COMPONENTS = GetComponents().ToList();
    public static readonly List<Type> TARGETS = GetTargets().ToList();

    public static bool Running(this CancellationToken token)
    {
        return !token.IsCancellationRequested;
    }

    public static async Task<Entity> AttachEntity(this Godot.GodotObject obj, EcsWorld world)
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

        return new Entity(world, id);
    }

    public static Task<Entity> AttachEntity(this int entity, EcsWorld world)
    {
        return Task.FromResult(new Entity(world, entity));
    }


    public static (Task, CancellationTokenSource) RunEntityTask(this Godot.GodotObject obj, EcsWorld world, Func<Entity, CancellationToken, Task> script)
    {
        var source = new CancellationTokenSource();

        var task = obj
            .AttachEntity(world)
            .ContinueWith(task =>
            {

                var entity = task.Result;

                var deleteTask = entity.Added<Delete>(source.Token);
                var scriptTask = script(entity, source.Token)
                    .ContinueWith(task =>
                    {

                        var exception = task.Exception is AggregateException aggregate
                            && aggregate.InnerException != null
                                ? aggregate.InnerException
                                : task.Exception;

                        var dead = exception is MissingEntityException missing &&
                            missing.Packed.Id == entity.Self.Id &&
                            missing.Packed.Gen == entity.Self.Gen;

                        if (!dead)
                        {
                            Console.WriteLine(exception);
                        }
                    }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted);

                return Task
                    .WhenAny(deleteTask, scriptTask)
                    .ContinueWith(task =>
                    {
                        source.Cancel();
                    }, TaskContinuationOptions.ExecuteSynchronously);

            }, TaskContinuationOptions.ExecuteSynchronously);

        return (task, source);
    }

    public static (Task, CancellationTokenSource) RunEntityTask(this int entity, EcsWorld world, Func<Entity, CancellationToken, Task> script)
    {
        var source = new CancellationTokenSource();

        var task = entity
            .AttachEntity(world)
            .ContinueWith(task =>
            {

                var entity = task.Result;

                var deleteTask = entity.Added<Delete>(source.Token);
                var scriptTask = script(entity, source.Token)
                    .ContinueWith(task =>
                    {

                        var exception = task.Exception is AggregateException aggregate
                            && aggregate.InnerException != null
                                ? aggregate.InnerException
                                : task.Exception;

                        var dead = exception is MissingEntityException missing &&
                            missing.Packed.Id == entity.Self.Id &&
                            missing.Packed.Gen == entity.Self.Gen;

                        if (!dead)
                        {
                            Console.WriteLine(exception);
                        }
                    }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted);

                return Task
                    .WhenAny(deleteTask, scriptTask)
                    .ContinueWith(task =>
                    {
                        source.Cancel();
                    }, TaskContinuationOptions.ExecuteSynchronously);

            }, TaskContinuationOptions.ExecuteSynchronously);

        return (task, source);
    }

    public class EntityListener<T> : IEcsWorldComponentListener<T>, IEcsWorldEventListener
    {
        public EcsPackedEntity[] Entities = Array.Empty<EcsPackedEntity>();
        protected TaskCompletionSource<(EcsPackedEntity, T)> Source =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void OnEntityDestroyed(int entity, short gen)
        {
            if (HasEntity(entity, gen, out EcsPackedEntity found))
            {
                Source.TrySetException(new MissingEntityException(found));
            }
        }

        public Task<(EcsPackedEntity, T)> Task(CancellationToken token)
        {
            token.Register(() =>
            {
                Source.TrySetCanceled();
            });

            return Source.Task;
        }

        public bool HasEntity(int id, short gen, out EcsPackedEntity found)
        {
            foreach (var entity in Entities)
            {
                if (entity.Id == id && entity.Gen == gen)
                {
                    found = entity;
                    return true;
                }
            }

            found = default;
            return false;
        }

        public virtual void OnComponentCreated(int entity, short gen, T component) { }
        public virtual void OnComponentDeleted(int entity, short gen, T component) { }
        public virtual void OnEntityCreated(int entity) { }
        public virtual void OnFilterCreated(EcsFilter filter) { }
        public virtual void OnWorldResized(int newSize) { }
        public virtual void OnWorldDestroyed(EcsWorld world) { }
    }

    public class AddListener<T> : EntityListener<T>
    {
        public override void OnComponentCreated(int id, short gen, T component)
        {
            if (HasEntity(id, gen, out EcsPackedEntity entity))
            {
                Source.TrySetResult((entity, component));
            }
        }
    }

    public class RemoveListener<T> : EntityListener<T>
    {
        public override void OnComponentDeleted(int id, short gen, T component)
        {
            EcsPackedEntity entity;
            if (HasEntity(id, gen, out entity))
            {
                Source.TrySetResult((entity, component));
            }
        }
    }

    public static async Task<(EcsPackedEntity, T)> Added<T>(this EcsWorld world, CancellationToken token, params EcsPackedEntity[] entities)
    {
        var listener = new AddListener<T>()
        {
            Entities = entities
        };

        world.AddComponentListener(listener);
        world.AddEventListener(listener);

        try
        {
            return await listener.Task(token);
        }
        finally
        {
            world.RemoveComponentListener(listener);
            world.RemoveEventListener(listener);
        }
    }

    public static async Task<(EcsPackedEntity, T)> Removed<T>(this EcsWorld world, CancellationToken token, params EcsPackedEntity[] entities)
    {
        var listener = new RemoveListener<T>()
        {
            Entities = entities
        };

        world.AddComponentListener(listener);
        world.AddEventListener(listener);

        try
        {
            return await listener.Task(token);
        }
        finally
        {
            world.RemoveComponentListener(listener);
            world.RemoveEventListener(listener);
        }
    }

    public static IEnumerable<T> NotNull<T>(this IEnumerable<T> collection)
    {
        return collection.Where(el => el != null);
    }

    public static T[] ToArray<[MustBeVariant] T>(this Godot.Collections.Array array)
    {
        var list = new List<T>();

        foreach (Godot.Variant element in array)
        {
            list.Add(element.As<T>());
        }

        return list.ToArray();
    }

    public static Godot.Variant ToVariant(this object obj)
    {
        return obj switch
        {
            int v => Variant.From(v),
            Vector4I v => Variant.From(v),
            Vector4 v => Variant.From(v),
            Transform3D v => Variant.From(v),
            Quaternion v => Variant.From(v),
            Basis v => Variant.From(v),
            Vector3I v => Variant.From(v),
            Vector3 v => Variant.From(v),
            Transform2D v => Variant.From(v),
            Rect2I v => Variant.From(v),
            Rect2 v => Variant.From(v),
            Vector2I v => Variant.From(v),
            string v => Variant.From(v),
            Projection v => Variant.From(v),
            double v => Variant.From(v),
            float v => Variant.From(v),
            ulong v => Variant.From(v),
            uint v => Variant.From(v),
            ushort v => Variant.From(v),
            byte v => Variant.From(v),
            long v => Variant.From(v),
            short v => Variant.From(v),
            sbyte v => Variant.From(v),
            Vector2 v => Variant.From(v),
            Aabb v => Variant.From(v),
            Plane v => Variant.From(v),
            char v => Variant.From(v),
            Godot.Collections.Array v => Variant.From(v),
            Godot.Collections.Dictionary v => Variant.From(v),
            Rid v => Variant.From(v),
            NodePath v => Variant.From(v),
            StringName v => Variant.From(v),
            GodotObject v => Variant.From(v),
            GodotObject[] v => Variant.From(v),
            Signal v => Variant.From(v),
            Callable v => Variant.From(v),
            Color v => Variant.From(v),
            bool v => Variant.From(v),
            _ => throw new NotSupportedException(),
        };
    }

    public static Dictionary<string, object?> ToFieldMap(this object obj)
    {
        var type = obj.GetType();
        var metadata = new Dictionary<string, object?>();

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
                metadata.Add(key, value?.ToFieldMap());
            }
        }

        return metadata;
    }

    public static bool IsEditable(this Type type)
    {
        return type.IsPrimitive || type == typeof(string) || type.IsEnum;
    }

    public static double Within(this Random random, double bound)
    {
        return random.Within(0, bound);
    }

    public static double Within(this Random random, double firstBound, double secondBound)
    {
        var delta = firstBound - secondBound;
        var value = random.NextDouble();

        return firstBound - delta * value;
    }

    public static int Within(this Random random, int bound)
    {
        return random.Within(0, bound);
    }

    public static int Within(this Random random, int firstBound, int secondBound)
    {
        var delta = firstBound - secondBound;
        var value = random.Next(Math.Abs(delta));

        return firstBound + (secondBound > firstBound ? 1 : -1) * value;
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

        var component = Activator.CreateInstance(componentType) ??
            throw new Exception($"Unable to instantiate {componentType}");

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
            annotations.Add(MANY);
        }

        var isEvent = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Event<>);
        if (isEvent)
        {
            type = type.GetGenericArguments().First();
            name = type.Name.ToLower();
            annotations.Add(EVENT);
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

            var targetField = thisComponent.GetType().GetField("Target") ?? throw new Exception("Target missing");
            var target = targetField.GetValue(thisComponent);
            var targetMeta = target?.ToMeta();
            eventDict["target"] = (targetMeta == null || targetMeta.Count == 0) ? false : targetMeta;

            var childComponentField = thisComponent.GetType().GetField("Component") ?? throw new Exception("Component missing");
            var childComponent = childComponentField.GetValue(thisComponent);
            var childMeta = childComponent?.ToMeta();
            eventDict["component"] = (childMeta == null || childMeta.Count == 0) ? false : childMeta;

            return new Dictionary<string, object>() {
                { name, eventDict }
            }.ToFlat(DELIMETER);
        };

        if (isMany)
        {
            var itemsField = component.GetType().GetField("Items") ?? throw new Exception("Items missing");
            var array = itemsField.GetValue(component) as Array;
            if (array == null) array = Array.CreateInstance(type, 0);
            var manyDict = new Dictionary<string, object>();

            for (var i = 0; i < array.Length; i++)
            {
                var element = array.GetValue(i) ?? throw new Exception("Element missing");
                var elementMeta = isEvent ? eventMeta(element) : element.ToMeta();
                var value = elementMeta.Select(entry =>
                   new KeyValuePair<string, object>(
                       String.Join(DELIMETER, entry.Key.Split(DELIMETER).Skip(1)),
                       entry.Value));
                manyDict.Add($"{i}", new Dictionary<string, object>(value));
            }

            return new Dictionary<string, object>() {
                { name, manyDict }
            }.ToFlat(DELIMETER);
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
            }.ToFlat(DELIMETER);
        }

        return new Dictionary<string, object>() {
            { name, true }
        }.ToFlat(DELIMETER);
    }

    public static string DELIMETER = "_";
    public static string MANY = "M";
    public static string EVENT = "E";

    public static string EncodeComponentPath(string path)
    {
        return path.Replace("/", DELIMETER).Replace("[]", MANY).Replace("()", EVENT);
    }

    public static object[] ToComponents(this Godot.GodotObject obj, string prefix, MetaType metaType = MetaType.Component)
    {
        prefix = EncodeComponentPath(prefix);

        var dict = new Dictionary<Type, object?>();
        var metalist = obj.GetMetaList()?.Select(meta => meta.ToString()) ?? Array.Empty<string>();

        foreach (var meta in metalist.Where(part => part.StartsWith(prefix)))
        {
            var subpath = new string(meta.Skip(prefix.Length).ToArray())
                .Split(DELIMETER)
                .Select(part => part.Trim())
                .Where(part => part.Length != 0)
                .ToArray();

            if (subpath.Length == 0) continue;

            var isMany = subpath[0].Contains(MANY);
            var isEvent = subpath[0].Contains(EVENT);

            var name = subpath[0].Replace(MANY, string.Empty).Replace(EVENT, string.Empty);
            Type componentType;

            switch (metaType)
            {
                case MetaType.Component:
                    {
                        componentType = COMPONENTS.FirstOrDefault(el => el.Name.ToLower() == name.ToLower()) ??
                            throw new Exception($"Component {name} not found");
                    }
                    break;
                case MetaType.Target:
                    {
                        componentType = TARGETS.FirstOrDefault(el => el.Name.ToLower() == name.ToLower()) ??
                            throw new Exception($"Target {name} not found");
                    }
                    break;

                default:
                    continue;
            }

            var eventType = typeof(Event<>).MakeGenericType(new[] { componentType });
            var elementType = isEvent ? eventType : componentType;
            var manyType = typeof(Many<>).MakeGenericType(new[] { elementType });
            var manyDictType = typeof(Dictionary<,>).MakeGenericType(new[] { typeof(string), elementType });

            var type = componentType;
            if (isEvent)
            {
                type = eventType;
            }
            if (isMany)
            {
                type = manyType;
            }

            object? component = null;
            object? manyComponent = null;
            object? eventComponent = null;

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

            if (component == null)
            {
                continue;
            }

            if (isMany)
            {
                manyComponent = component;

                var key = subpath[1];
                if (manyComponent is not IDictionary manyDict)
                    throw new Exception("Many[] component is not a dictionary");

                component = manyDict.Contains(key)
                    ? manyDict[key]
                    : Activator.CreateInstance(elementType);
            }

            var fieldPath = String.Join(DELIMETER, subpath.Skip(isMany ? 2 : 1));

            if (isEvent)
            {
                eventComponent = component;

                if (fieldPath.StartsWith("component" + DELIMETER))
                {
                    var eventComponentPath = new string(meta.SkipLast(fieldPath.Length).ToArray()) + "component";
                    component = obj.ToComponents(eventComponentPath).First();
                }
                else if (fieldPath.StartsWith("target" + DELIMETER))
                {
                    var eventTargetPath = new string(meta.SkipLast(fieldPath.Length).ToArray()) + "target";
                    component = obj.ToComponents(eventTargetPath, MetaType.Target).First();
                }
            }

            var value = obj.GetMeta(meta).Obj;

            try
            {
                if (component is null)
                {
                    throw new Exception($"Component {componentType.Name} not null");
                }

                component = SetField(component, fieldPath, value);
            }
            catch (Exception ex)
            {
                var objName = obj is Node node ? node.Name.ToString() : obj.ToString();
                Console.WriteLine($"Unable to set {componentType.Name} {fieldPath} to '{value}' for '{objName}': {ex.Message}");
            }

            if (isEvent)
            {
                if (fieldPath.StartsWith("component" + DELIMETER))
                {
                    var fieldInfo = eventType.GetField("Component");
                    fieldInfo?.SetValue(eventComponent, component);
                }
                else if (fieldPath.StartsWith("target" + DELIMETER))
                {
                    var fieldInfo = eventType.GetField("Target");
                    fieldInfo?.SetValue(eventComponent, component);
                }
                component = eventComponent;
            }

            if (isMany)
            {
                var key = subpath[1];
                if (manyComponent is IDictionary manyDict)
                {
                    manyDict[key] = component;
                }
                component = manyComponent;
            }

            dict[type] = component;
        }

        var components = dict.Values.ToArray();
        for (var i = 0; i < components.Length; i++)
        {
            ref var component = ref components[i];
            var type = component?.GetType();
            if (type?.FindInterfaces((intf, o) => intf == typeof(IDictionary), component).Length > 0)
            {
                var manyDict = component as IDictionary;

                var elementType = manyDict?.GetType().GenericTypeArguments[1] ?? throw new Exception("Many[] component is not a dictionary");
                var manyType = typeof(Many<>).MakeGenericType(new[] { elementType });
                var array = Array.CreateInstance(elementType, manyDict.Count);
                manyDict.Values.CopyTo(array, 0);

                component = Activator.CreateInstance(manyType) ?? throw new Exception("Many[] component is not a instantiated");
                component = component.SetField("Items", array);
            }
        }

        return (components as object[])?.Where(el => el != null).ToArray() ?? Array.Empty<object>();
    }

    public static object SetField(this object obj, string path, object? value)
    {
        var parts = path.Split(DELIMETER);
        if (parts.Length == 0) return obj;

        var fieldInfo = obj.GetType().GetFields()
            .FirstOrDefault(el => el.Name.ToLower() == parts[0].ToLower());
        if (fieldInfo == null) return obj;

        var field = fieldInfo.GetValue(obj);

        if (parts.Length == 1)
        {
            var isConvertable = fieldInfo.FieldType
                .FindInterfaces((intf, o) => intf == typeof(IConvertible), value)
                .Length > 0;

            var isArray = fieldInfo.FieldType.IsArray;

            object? converted;

            if (isConvertable)
            {
                converted = Convert.ChangeType(value, fieldInfo.FieldType);
            }
            else if (isArray)
            {
                var array = value as Array;
                var length = (value as Array)?.Length ?? 0;

                converted = Array.CreateInstance(
                    fieldInfo.FieldType?.GetElementType() ?? throw new Exception("Unable to get array element type"),
                    length);

                if (value is Array arrayValue && converted is Array convertedValue)
                {
                    Array.Copy(arrayValue, convertedValue, length);
                }
                else
                {
                    throw new Exception("Cannot copy array");
                }
            }
            else
            {
                throw new Exception($"Unable to set field {fieldInfo.Name} to {value}");
            }

            fieldInfo.SetValue(obj, converted);
        }
        else if (field != null)
        {
            fieldInfo.SetValue(obj, SetField(field, String.Join(DELIMETER, parts.Skip(1)), value));
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

    private static readonly Dictionary<Type, MethodInfo> getPoolMethodCache =
        new();

    private static readonly Dictionary<Type, MethodInfo> addMethodCache =
        new();

    public static void AddNotify(this EcsWorld world, int entity, object component)
    {
        var type = component.GetType();
        var poolType = type;

        getPoolMethodCache.TryGetValue(type, out MethodInfo? getPoolMethod);
        if (getPoolMethod == null)
        {
            getPoolMethod = (typeof(EcsWorld).GetMethod("GetPool") ?? throw new Exception("GetPool method not found"))
                .MakeGenericMethod(poolType);

            getPoolMethodCache.Add(type, getPoolMethod);
        }

        var pool = getPoolMethod.Invoke(world, null);

        addMethodCache.TryGetValue(type, out MethodInfo? addMethod);
        if (addMethod == null)
        {
            addMethod = (typeof(Utils).GetMethod("ReflectionAddNotify") ?? throw new Exception("ReflectionAddNotify method not found"))
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

    public static void SetEntity(this Godot.GodotObject obj, EcsWorld world, int entity)
    {
        var packed = world.PackEntity(entity);
        obj.SetMeta($"entity{DELIMETER}id", packed.Id);
        obj.SetMeta($"entity{DELIMETER}gen", packed.Gen);

        if (!obj.HasUserSignal("entity"))
        {
            obj.AddUserSignal("entity");
        }

        obj.EmitSignal("entity", Array.Empty<Variant>());
    }

    public static int GetEntity(this Godot.GodotObject obj, EcsWorld world)
    {
        if (obj == null ||
            !obj.HasMeta($"entity{DELIMETER}id") ||
            !obj.HasMeta($"entity{DELIMETER}gen"))
        {
            return -1;
        }

        EcsPackedEntity packed;
        packed.Id = obj.GetMeta($"entity{DELIMETER}id").AsInt32();
        packed.Gen = obj.GetMeta($"entity{DELIMETER}gen").AsInt32();


        packed.Unpack(world, out int entityId);
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