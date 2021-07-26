using System;
using Leopotam.EcsLite;
using System.Runtime.CompilerServices;

public struct Event<C, E>
    where C : struct
    where E : struct
{
    public EcsPackedEntity Entity;
    public E Data;
}

public struct Add { }

public class AddEvents<C> : Events<C, Add>
    where C : struct
{
    public AddEvents(EcsWorld world) : base(world) { }
}

public class Events<C, E>
    where C : struct
    where E : struct
{
    private EcsWorld _world;
    private EcsFilter _filter;
    private EcsPool<C> _pool;
    private EcsPool<Event<C, E>> _eventPool;

    public Events(EcsWorld world)
    {
        _world = world;
        _pool = _world.GetPool<C>();
        _eventPool = _world.GetPool<Event<C, E>>();
        _filter = _world.Filter<C>().Inc<Event<C, E>>().End();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventEnumerator GetEnumerator()
    {
        return new EventEnumerator(_filter.GetEnumerator(), _world, _eventPool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref C Get(int entity)
    {
        return ref _pool.Get(entity);
    }

    public struct EventEnumerator : IDisposable
    {
        EcsFilter.Enumerator _enumerator;
        int _current;

        readonly EcsPool<Event<C, E>> _eventPool;
        readonly EcsWorld _world;

        public EventEnumerator(EcsFilter.Enumerator enumerator, EcsWorld world, EcsPool<Event<C, E>> events)
        {
            _enumerator = enumerator;
            _eventPool = events;
            _world = world;
            _current = -1;
        }

        public int Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (_enumerator.MoveNext())
            {
                ref var @event = ref _eventPool.Get(_enumerator.Current);
                if (@event.Entity.Unpack(_world, out var entity))
                {
                    _current = entity;
                    return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            _enumerator.Dispose();
        }
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

public class EventDelete<C, E> : IEcsInitSystem, IEcsRunSystem
    where C : struct
    where E : struct
{
    private EcsPool<Event<C, E>> _pool;
    private EcsFilter _filter;

    public void Init(EcsSystems systems)
    {
        var world = systems.GetWorld();

        _pool = world.GetPool<Event<C, E>>();
        _filter = world.Filter<Event<C, E>>().End();
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

public static class EventUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref Event<C, E> Event<C, E>(this EcsPool<C> pool, int entity)
        where C : struct
        where E : struct
    {
        var world = pool.GetWorld();
        ref var @event = ref world.GetPool<Event<C, E>>().Replace(entity);
        @event.Entity = world.PackEntity(entity);
        return ref @event;
    }
}

public static class AddUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref C AddEvent<C>(this EcsPool<C> pool, int entity)
        where C : struct
    {
        pool.Event<C, Add>(entity);
        return ref pool.Add(entity);
    }
}
