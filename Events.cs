using Ecs;
using System;
using System.Linq;
using System.Collections.Generic;

public record Player() : Component;

public record Command(string Target = null, int Tick = 0) : Component
{
    public static string TARGET_OTHER = "__OTHER";
    public static string TARGET_SELF = "__SELF";
}

public record Event : Command
{
    public Command[] Commands = new Command[] { };

    public Event(IEnumerable<Command> commands)
        => (Commands) = (commands.ToArray());

    public Event(params Command[] commands)
        => (Commands) = (commands);
}

public record Collide : Event
{
    public Collide(params Command[] commands)
        => (Commands) = (commands);
}

public record Click : Event
{
    public Click(params Command[] commands)
        => (Commands) = (commands);
}

public record AddEntity(Entity Entity, string ID = null) : Command;

public record RemoveEntity(string Target = null) : Command(Target);

public record AddComponent(Component Component, string Target = null) : Command(Target);

public record RemoveComponent(Component Component, string Target = null) : Command(Target);

public static class Events
{
    public static State System(int tick, State state, string id, string otherId, Command[] commands)
    {
        foreach (var command in commands)
        {
            var target = id;

            if (command.Target == Command.TARGET_OTHER)
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
            else if (command.Target == Command.TARGET_SELF)
            {
                target = id;
            }
            else if (command.Target?.Length > 0)
            {
                target = command.Target;
            }

            switch (command)
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
                        if (newComponent is Command addCommand)
                        {
                            newComponent = addCommand with { Tick = tick };
                        }
                        state = state.With(target, newComponent);
                    }
                    break;

                default:
                    {
                        state = state.With(target, command with { Tick = tick });
                    }
                    break;

            }
        }

        return state;
    }
}