using Ecs;
using System;
using System.Linq;

public static class Target
{
    public static string Other = "__OTHER";
    public static string Self = "__SELF";
}

public record Task
{
    public string Target = null;
}

public record Event : Component
{
    public Task[] Tasks = new Task[] { };

    public Event(params Task[] tasks)
        => (Tasks) = (tasks);
}

public record Add : Task
{
    public Component Component;

    public Add() { }

    public Add(Component component, string target = null)
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
    public Entity Entity;

    public AddEntity() { }

    public AddEntity(Entity entity, string target = null)
        => (Entity, Target) = (entity, target);
}

public record RemoveEntity : Task;

public static class Events
{
    public static State System(State state, string id, string otherId, Event ev)
    {
        foreach (var task in ev.Tasks)
        {
            var target = task.Target == Target.Other
                ? otherId
                : task.Target == null || task.Target == Target.Self
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
                        var entity = state[target];
                        state = state.With(target, entity.Without(remove.Type.Name));
                    }
                    break;

                case AddEntity addEntity:
                    {
                        target = addEntity.Target?.Length > 0
                            ? addEntity.Target
                            : Guid.NewGuid().ToString();

                        state = state.With(target, addEntity.Entity);
                    }
                    break;

                case RemoveEntity removeEntity:
                    {
                        state = state.Without(target);
                    }
                    break;
            }
        }

        return state;
    }
}