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

public record Event : Task
{
    public Task[] Tasks = new Task[] { };

    public Event(IEnumerable<Task> tasks)
        => (Tasks) = (tasks.ToArray());

    public Event(params Task[] tasks)
        => (Tasks) = (tasks);
}

public record AddEntity(Entity Entity, string ID = null) : Task;

public record RemoveEntity(string Target = null) : Task(Target);

public record AddComponent(Component Component, string Target = null) : Task(Target);

public record RemoveComponent(Component Component, string Target = null) : Task(Target);

public static class Events
{
    public static State System(int tick, State state, string id, string otherId, Task[] tasks)
    {
        foreach (var task in tasks)
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
                case AddEntity addEntity:
                    {
                        var entityId = addEntity.ID?.Length > 0
                            ? addEntity.ID
                            : Guid.NewGuid().ToString();
                        state = state.With(entityId, addEntity.Entity);
                    }
                    break;

                case RemoveEntity removeEntity:
                    {
                        state = state.Without(target);
                    }
                    break;

                case RemoveComponent removeComponent:
                    {
                        var entity = state[target];
                        state = state.With(target, entity with
                        {
                            Components = entity.Components.Where(component => component != removeComponent.Component)
                        });
                    }
                    break;

                case AddItem addItem:
                    {
                        var entity = state[target];
                        var inventory = entity.Get<Inventory>();
                        if (inventory != null)
                        {
                            state = state.With(target, entity.With(new Inventory(inventory.Items.Concat(new[] { addItem.Item }))));
                        }
                    }
                    break;

                case AddComponent addComponent:
                    {
                        var newComponent = addComponent.Component;
                        if (newComponent is Task addTask)
                        {
                            newComponent = addTask with { Tick = tick };
                        }
                        state = state.With(target, newComponent);
                    }
                    break;

                default:
                    {
                        state = state.With(target, task with { Tick = tick });
                    }
                    break;

            }
        }

        return state;
    }
}