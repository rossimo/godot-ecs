using System;
using Leopotam.EcsLite;
using System.Runtime.CompilerServices;

public struct Event<C, E>
    where C : struct
    where E : struct
{
    public EcsPackedEntity Entity;
}

public struct Add { }

public class AddEvents<C> : Events<C, Add> where C : struct
{
    public AddEvents(EcsWorld world) : base(world)
    {
    }
}

public struct Delete { }

public class DeleteEvents<C> : Events<C, Delete> where C : struct
{
    public DeleteEvents(EcsWorld world) : base(world)
    {
    }
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
    public EventEnumerator<C, E> GetEnumerator()
    {
        return new EventEnumerator<C, E>(_filter.GetEnumerator(), _world, _eventPool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref C Get(int entity)
    {
        return ref _pool.Get(entity);
    }

    public struct EventEnumerator<EC, EE> : IDisposable
        where EC : struct
        where EE : struct
    {
        EcsFilter.Enumerator _enumerator;
        int _current;

        readonly EcsPool<Event<EC, EE>> _events;
        readonly EcsWorld _world;

        public EventEnumerator(EcsFilter.Enumerator enumerator, EcsWorld world, EcsPool<Event<EC, EE>> events)
        {
            _enumerator = enumerator;
            _events = events;
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
                var current = _enumerator.Current;
                var @event = _events.Get(current);
                _events.Del(current);
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

public static class EventUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref C AddEmit<C>(this EcsPool<C> pool, EcsWorld world, int entity)
        where C : struct
    {
        ref var component = ref pool.Add(entity);
        AddOrReplaceEmit(pool, world, entity);
        return ref component;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddOrReplaceEmit<C>(this EcsPool<C> pool, EcsWorld world, int entity)
        where C : struct
    {
        var addEvents = world.GetPool<Event<C, Add>>();
        addEvents.Del(entity);

        var delEvents = world.GetPool<Event<C, Delete>>();
        delEvents.Del(entity);

        ref var @event = ref addEvents.Add(entity);
        @event.Entity = world.PackEntity(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref C AddOrReplace<C>(this EcsPool<C> pool, int entity)
        where C : struct
    {
        pool.Del(entity);
        ref var component = ref pool.Add(entity);
        return ref component;
    }
}
