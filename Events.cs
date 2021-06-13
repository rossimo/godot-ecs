using Ecs;
using System;
using System.Linq;
using System.Collections.Generic;

public record Player() : Component;

public record Event : Component
{
    public IEnumerable<Command> Commands = new List<Command>();

    public Event() { }

    public Event(IEnumerable<Command> commands)
        => (Commands) = (commands);

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

public record Move(Position Position, float Speed) : Event;

public record Command(string Target = null, bool TargetOther = false);

public record RemoveEntity(string Target = null, bool TargetOther = false) : Command(Target, TargetOther);

public record RemoveComponent(Component Component, string Target = null, bool TargetOther = false) : Command(Target, TargetOther);

public record AddRotation(float Degrees, string Target = null) : Command(Target);

public static class Events
{
    public static State System(State state, string id, string otherId, Component component)
    {
        if (!(component is Event))
        {
            return state;
        }

        foreach (var command in (component as Event).Commands)
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
                case AddRotation rotate:
                    {
                        state = state.With(target, new Rotation(
                            (state[target].Get<Rotation>()?.Degrees ?? 0) +
                            rotate.Degrees));
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

                case RemoveEntity removeEntity:
                    {
                        state = state.Without(target);
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