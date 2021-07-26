using System;
using Leopotam.EcsLite;
using System.Runtime.CompilerServices;

public struct Publish<C> { }

public static class PublishUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref Publish<C> Publish<C>(this EcsPool<C> pool, int entity)
        where C : struct
    {
        return ref pool.GetWorld().GetPool<Publish<C>>().Replace(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref C AddPublish<C>(this EcsPool<C> pool, int entity)
        where C : struct
    {
        pool.Publish(entity);
        return ref pool.Add(entity);
    }
}

public struct Delete { }

public class EntityDelete : IEcsInitSystem, IEcsRunSystem
{
    private EcsPool<Delete> _pool;
    private EcsFilter _filter;

    public void Init(EcsSystems systems)
    {
        var world = systems.GetWorld();

        _pool = world.GetPool<Delete>();
        _filter = world.Filter<Delete>().End();
    }

    public void Run(EcsSystems systems)
    {
        var world = systems.GetWorld();

        foreach (var entity in _filter)
        {
            world.DelEntity(entity);
        }
    }
}

public class ComponentDelete<C> : IEcsInitSystem, IEcsRunSystem
    where C : struct
{
    private EcsPool<C> _pool;
    private EcsFilter _filter;

    public void Init(EcsSystems systems)
    {
        var world = systems.GetWorld();

        _pool = world.GetPool<C>();
        _filter = world.Filter<C>().End();
    }

    public void Run(EcsSystems systems)
    {
        var world = systems.GetWorld();

        foreach (var entity in _filter)
        {
            _pool.Del(entity);
        }
    }
}

public static class PoolUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref C Replace<C>(this EcsPool<C> pool, int entity)
        where C : struct
    {
        pool.Del(entity);
        return ref pool.Add(entity);
    }
}
