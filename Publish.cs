using System;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
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

public class EntityDelete : IEcsRunSystem
{
    [EcsWorld] readonly EcsWorld world = default;
    [EcsFilter(typeof(Delete))] readonly EcsFilter _filter = default;

    public void Run(EcsSystems systems)
    {
        foreach (var entity in _filter)
        {
            world.DelEntity(entity);
        }
    }
}

public class ComponentDelete<C> : IEcsInitSystem, IEcsRunSystem
    where C : struct
{
    EcsFilter _filter;
    [EcsPool] readonly EcsPool<C> _pool = default;

    public void Init(EcsSystems systems)
    {
        var world = systems.GetWorld();

        _filter = world.Filter<C>().End();
    }

    public void Run(EcsSystems systems)
    {
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
