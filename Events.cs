using Ecs;
using System;
using System.Linq;
using System.Collections.Generic;

public record Player() : Component;

public record Task(string Target = null, int Tick = 0) : Component
{
    public static string TARGET_OTHER = "__OTHER";
    public static string TARGET_SELF = "__SELF";
}

public record Event : Component
{
    public Task[] Tasks = new Task[] { };

    public Event(params Task[] tasks)
        => (Tasks) = (tasks);
}

public record AddEntityTask(Entity Entity, string ID = null) : Task;

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

            if (task.Target == Task.TARGET_OTHER)
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
            else if (task.Target == Task.TARGET_SELF)
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
                        var entityId = addEntity.ID?.Length > 0
                            ? addEntity.ID
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