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

/*
public static class Target
{
    public static int Other = -1;
    public static int Self = -2;
}

public record Task
{
    public int Target = -2;
}

public record Event : Component
{
    public Task[] Tasks = new Task[] { };

    public Event(params Task[] tasks)
        => (Tasks) = (tasks);

    public override string ToString()
    {
        return $"{this.GetType().Name} {{ {Utils.Log(nameof(Tasks), Tasks)} }}";
    }
}

public record Add : Task
{
    public Component Component;

    public Add() { }

    public Add(Component component, int target = -2)
        => (Component, Target) = (component, target);
}

public record Remove : Task
{
    public Type Type;

    public Remove(Type type)
        => (Type) = (type);

    public Remove(Component component)
        => (Type) = (component.GetType());
}

public record AddEntity : Task
{
    public Component[] Components;

    public AddEntity() { }

    public AddEntity(Component[] components, int target = -2)
        => (Components, Target) = (components, target);
}

public record RemoveEntity : Task;

public record EventQueue : Component
{
    public (int Source, int Target, Event Event)[] Events =
        new (int Source, int Target, Event Event)[] { };

    public EventQueue(params (int Source, int Target, Event Event)[] queue)
        => (Events) = (queue);

    public override string ToString()
    {
        return $"{this.GetType().Name} {{ {Utils.Log(nameof(Events), Events)} }}";
    }
}

public static class Events
{
    public static int ENTITY = 2;

    public static State System(State previous, State state)
    {
        var queue = state.Get<EventQueue>(ENTITY).Events;
        if (queue?.Count() == 0) return state;

        foreach (var queued in queue)
        {
            var (id, otherId, @event) = queued;

            foreach (var task in @event.Tasks)
            {
                var target = task.Target == Target.Other
                    ? otherId
                    : task.Target == Target.Self
                        ? id
                        : task.Target;

                switch (task)
                {
                    case Add add:
                        {
                            state = state.With(target, add.Component);
                        }
                        break;

                    case Remove remove:
                        {
                            state = state.Without(remove.Type.Name.GetHashCode(), target);
                        }
                        break;

                    case AddEntity addEntity:
                        {
                            state = state.With(target, addEntity.Components);
                        }
                        break;

                    case RemoveEntity removeEntity:
                        {
                            state = state.Without(target);
                        }
                        break;
                }
            }
        }

        return state = state.With(Events.ENTITY, new EventQueue());
    }
}
*/