using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Collections.Generic;

public struct EventQueue
{
    public List<Event> Events;
}

public struct Event
{
    public EcsPackedEntity Source;
    public EcsPackedEntity Target;
    public EventTask Task;
}

public struct EventTrigger<T>
    where T : struct
{
    public EventTask[] Tasks;
}

public interface EventTask
{
    public void Run(EcsWorld world, int self, int other);
}

public struct AddNotifySelf<C> : EventTask
    where C : struct
{
    public C Component;

    public void Run(EcsWorld world, int self, int other)
    {
        if (self == -1) return;

        var pool = world.GetPool<C>();
        ref var component = ref pool.Ensure<C>(self);
        pool.Notify(self);
        component = Component;
    }
}

public struct AddNotifyOther<C> : EventTask
    where C : struct
{
    public C Component;

    public void Run(EcsWorld world, int self, int other)
    {
        if (other == -1) return;

        var pool = world.GetPool<C>();
        ref var component = ref pool.Ensure<C>(other);
        pool.Notify(other);
        component = Component;
    }
}

/* Boxing optimization */
public static class EventUtils
{
    public static void Run(this EventTask[] tasks, EcsWorld world, int self, int other)
    {
        for (int i = 0; i < tasks.Length; i++)
        {
            ref var task = ref tasks[i];
            Run(ref task, world, self, other);
        }
    }

    public static void Run<T>(ref T task, EcsWorld world, int self, int other)
        where T : EventTask
    {
        task.Run(world, self, other);
    }
}

public class EventSystem : IEcsInitSystem, IEcsRunSystem
{
    [EcsWorld] readonly EcsWorld world = default;
    [EcsShared] readonly Shared shared = default;
    [EcsPool] readonly EcsPool<EventQueue> eventQueues = default;

    public void Init(EcsSystems systems)
    {
        var world = systems.GetWorld();
        var shared = systems.GetShared<Shared>();

        var events = world.NewEntity();
        ref var queuedTask = ref eventQueues.Add(events);
        queuedTask.Events = new List<Event>();

        shared.Events = events;
    }

    public void Queue(Event ev)
    {
        ref var eventQueue = ref eventQueues.Get(shared.Events);

        eventQueue.Events.Add(ev);
    }

    public void Run(EcsSystems systems)
    {
        ref var eventQueue = ref eventQueues.Get(shared.Events);

        for (var i = 0; i < eventQueue.Events.Count; i++)
        {
            var ev = eventQueue.Events[i];

            ev.Source.Unpack(world, out var source);
            ev.Target.Unpack(world, out var target);

            EventUtils.Run(ref ev.Task, world, source, target);
        }

        eventQueue.Events.Clear();
    }
}

/*
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