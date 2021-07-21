using System;
using SimpleEcs;
using System.Linq;

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
        var queue = state.EventQueue(ENTITY).Events;
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