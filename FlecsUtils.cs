using Godot;
using Flecs.NET.Core;
using System.Reflection;

public static class FlecsUtils
{
    public static void SetEntity(this GodotObject obj, Entity entity)
    {

        obj.SetMeta($"entity{Utils.DELIMETER}id", entity.Id.Value);

        if (!obj.HasUserSignal("entity"))
        {
            obj.AddUserSignal("entity");
        }

        obj.EmitSignal("entity", Array.Empty<Variant>());
    }

    public static void Cleanup(this Entity entity)
    {
        var count = 0;
        Ecs.EachIdCallback eachIdCallback = (id) =>
        {
            count++;
        };

        entity.Each(eachIdCallback);

        if (count == 0)
        {
            entity.Destruct();
        }
    }


    private static Dictionary<Type, MethodInfo> addComponentCache = new Dictionary<Type, MethodInfo>();

    private static MethodInfo entitySetComponentdMethod = typeof(Entity).GetMethods().First(m => m.ToString() == "Flecs.NET.Core.Entity& Set[T](T)");

    public static void UnsafeAddComponent(this Entity entity, object component)
    {
        var type = component.GetType();
        MethodInfo set = null;

        if (addComponentCache.ContainsKey(type))
        {
            set = addComponentCache[type];
        }

        if (set == null)
        {
            set = entitySetComponentdMethod.MakeGenericMethod(new Type[] { type });
            addComponentCache.Add(type, set);
        }

        set.Invoke(entity, new[] { component });
    }

    public delegate void ForEach<T1>(ref T1 c1);

    public static Action System<T1>(this World world, ForEach<T1> callback)
    {
        var query = world.Query(filter: world.FilterBuilder().Term<T1>());

        return () => world.Defer(() => query.Iter(it =>
        {
            var c1 = it.Field<T1>(1);

            foreach (int i in it)
                callback(ref c1[i]);
        }));
    }

    public delegate void ForEachEntity<T1>(Entity entity, ref T1 c1);

    public static Action System<T1>(this World world, ForEachEntity<T1> callback)
    {
        var query = world.Query(filter: world.FilterBuilder().Term<T1>());

        return () => world.Defer(() => query.Iter(it =>
        {
            var c1 = it.Field<T1>(1);

            foreach (int i in it)
                callback(it.Entity(i), ref c1[i]);
        }));
    }

    public delegate void ForEach<T1, T2>(ref T1 c1, ref T2 c2);

    public static Action System<T1, T2>(this World world, ForEach<T1, T2> callback)
    {
        var query = world.Query(filter: world.FilterBuilder().Term<T1>().Term<T2>());

        return () => world.Defer(() => query.Iter(it =>
        {
            var c1 = it.Field<T1>(1);
            var c2 = it.Field<T2>(2);

            foreach (int i in it)
                callback(ref c1[i], ref c2[i]);
        }));
    }

    public delegate void ForEachEntity<T1, T2>(Entity entity, ref T1 c1, ref T2 c2);

    public static Action System<T1, T2>(this World world, ForEachEntity<T1, T2> callback)
    {
        var query = world.Query(filter: world.FilterBuilder().Term<T1>().Term<T2>());

        return () => world.Defer(() => query.Iter(it =>
        {
            var c1 = it.Field<T1>(1);
            var c2 = it.Field<T2>(2);

            foreach (int i in it)
                callback(it.Entity(i), ref c1[i], ref c2[i]);
        }));
    }

    public delegate void ForEach<T1, T2, T3>(ref T1 c1, ref T2 c2, ref T2 c3);

    public static Action System<T1, T2, T3>(this World world, ForEach<T1, T2, T3> callback)
    {
        var query = world.Query(filter: world.FilterBuilder().Term<T1>().Term<T2>().Term<T3>());

        return () => world.Defer(() => query.Iter(it =>
        {
            var c1 = it.Field<T1>(1);
            var c2 = it.Field<T2>(2);
            var c3 = it.Field<T2>(3);

            foreach (int i in it)
                callback(ref c1[i], ref c2[i], ref c3[i]);
        }));
    }

    public delegate void ForEachEntity<T1, T2, T3>(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3);

    public static Action System<T1, T2, T3>(this World world, ForEachEntity<T1, T2, T3> callback)
    {
        var query = world.Query(filter: world.FilterBuilder().Term<T1>().Term<T2>().Term<T3>());

        return () => world.Defer(() => query.Iter(it =>
        {
            var c1 = it.Field<T1>(1);
            var c2 = it.Field<T2>(2);
            var c3 = it.Field<T3>(3);

            foreach (int i in it)
                callback(it.Entity(i), ref c1[i], ref c2[i], ref c3[i]);
        }));
    }
}