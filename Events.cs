using Ecs;
using System;
using System.Collections.Generic;

public record Player() : Component;

public record Event : Component
{
    public IEnumerable<Command> Commands = new List<Command>();

    public Event() { }

    public Event(IEnumerable<Command> commands)
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

public record RemoveEntity(string Target = null, bool TargetOther = false) : Command(Target, TargetOther);

public record AddRotation(float Degrees, string Target = null) : Command(Target);

public static class Events
{
    public static State System(State state, string id, string otherId, Component component)
    {
        if (!(component is Event))
        {
            return state;
        }

        Func<Command, string> findId = (Command command) =>
        {
            if (command.TargetOther)
            {
                return otherId;
            }

            if (command.Target?.Length > 0)
            {
                return command.Target;
            }

            return id;
        };

        foreach (var command in (component as Event).Commands)
        {
            switch (command)
            {
                case AddRotation rotate:
                    {
                        state = state.With(findId(command), new Rotation(
                            (state[findId(command)].Get<Rotation>()?.Degrees ?? 0) +
                            rotate.Degrees));
                    }
                    break;

                case RemoveEntity remove:
                    {
                        state = state.Without(findId(command));
                    }
                    break;
            }
        }

        return state;
    }
}