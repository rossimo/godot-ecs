using System;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Runtime.CompilerServices;

public struct Notify<C> { }

public static class NotifyUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref Notify<C> Notify<C>(this EcsPool<C> pool, int entity)
        where C : struct
    {
        return ref pool.GetWorld().GetPool<Notify<C>>().Ensure(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref C AddNotify<C>(this EcsPool<C> pool, int entity)
        where C : struct
    {
        pool.Notify(entity);
        return ref pool.Add(entity);
    }
}

public class AsyncSystem : IEcsRunSystem
{
    public void Run(EcsSystems systems)
    {
        EventLoopSynchronizationContext.Update();
        EventLoop.Scheduler.Execute();
    }
}

[Editor]
public struct Delete { }

public class DeleteEntitySystem : IEcsRunSystem
{
    [EcsWorld] readonly EcsWorld world = default;
    [EcsFilter(typeof(Delete))] readonly EcsFilter _filter = default;

    public void Run(EcsSystems systems)
    {
        foreach (var entity in _filter)
        {
            var remaining = new object[world.GetComponentsCount(entity)];
            world.GetComponents(entity, ref remaining);
            foreach (var component in remaining)
            {
                world.GetPoolByType(component.GetType()).Del(entity);
            }


            world.DelEntity(entity);
        }
    }
}

public class DeleteComponentSystem<C> : IEcsInitSystem, IEcsRunSystem
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
    public static ref C Ensure<C>(this EcsPool<C> pool, int entity)
        where C : struct
    {
        if (pool.Has(entity))
        {
            return ref pool.Get(entity);
        }
        else
        {
            return ref pool.Add(entity);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref C Replace<C>(this EcsPool<C> pool, int entity)
        where C : struct
    {
        pool.Del(entity);
        return ref pool.Add(entity);
    }
}
