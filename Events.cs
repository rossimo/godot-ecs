using Ecs;
using System;
using System.Linq;

public record Player() : Component;

public static class Target
{
    public static string Other = "__OTHER";
    public static string Self = "__SELF";
}

public record Task(string Target = null, int Tick = 0) : Component;

public record Event : Component
{
    public Task[] Tasks = new Task[] { };

    public Event(params Task[] tasks)
        => (Tasks) = (tasks);
}

public record AddEntityTask(Entity Entity, string Target = null) : Task(Target);

public record RemoveEntityTask(string Target = null) : Task(Target);

public record AddComponentTask(string Target = null) : Task(Target);

public record RemoveComponentTask(Component Component, string Target = null) : Task(Target);

public static class Events
{
    public static State System(int tick, State state, string id, string otherId, Event ev)
    {
        foreach (var task in ev.Tasks)
        {
            var target = id;

            if (task.Target == Target.Other)
            {
                if (otherId?.Length > 0)
                {
                    target = otherId;
                }
                else
                {
                    continue;
                }
            }
            else if (task.Target == Target.Self)
            {
                target = id;
            }
            else if (task.Target?.Length > 0)
            {
                target = task.Target;
            }

            switch (task)
            {
                case AddEntityTask addEntity:
                    {
                        var entityId = addEntity.Target?.Length > 0
                            ? addEntity.Target
                            : Guid.NewGuid().ToString();
                        state = state.With(entityId, addEntity.Entity);
                    }
                    break;

                case RemoveEntityTask removeEntity:
                    {
                        state = state.Without(target);
                    }
                    break;

                case RemoveComponentTask removeComponent:
                    {
                        var entity = state[target];
                        state = state.With(target, entity with
                        {
                            Components = entity.Components.Where(component => component != removeComponent.Component)
                        });
                    }
                    break;

                case AddComponentTask addComponent:
                    {
                        state = state.With(target, task with { Tick = tick });
                    }
                    break;
            }
        }

        return state;
    }
}