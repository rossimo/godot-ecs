using Ecs;
using System;
using System.Linq;
using System.Collections.Generic;

public record Player() : Component;

public record Event : Component
{
    public string ID;

    public Command[] Commands = new Command[] {};

    public Event() { }

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

public record Command(string Target = null, bool TargetOther = false);

public record AddEntity(Entity Entity) : Command;

public record RemoveEntity(string Target = null, bool TargetOther = false) : Command(Target, TargetOther);

public record AddComponent(Component Component, string Target = null, bool TargetOther = false) : Command(Target, TargetOther);

public record RemoveComponent(Component Component, string Target = null, bool TargetOther = false) : Command(Target, TargetOther);

public static class Events
{
    public static State System(State state, string id, string otherId, Component component)
    {
        if (!(component is Event))
        {
            return state;
        }

        var ev = component as Event;

        foreach (var command in ev.Commands)
        {
            var target = id;

            if (command.TargetOther)
            {
                target = otherId;
            }

            if (command.Target?.Length > 0)
            {
                target = command.Target;
            }

            switch (command)
            {
                case AddEntity addEntity:
                    {
                        state = state.With(Guid.NewGuid().ToString(), addEntity.Entity);
                    }
                    break;

                case RemoveEntity removeEntity:
                    {
                        state = state.Without(target);
                    }
                    break;

                case AddComponent addComponent:
                    {
                        var entity = state[target];
                        state = state.With(target, addComponent.Component);
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
            }
        }

        return state;
    }
}