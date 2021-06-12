using Ecs;
using System.Collections.Generic;

public record Player() : Component;

public record Position(float X, float Y, bool Self = false) : Component;

public record Move(Position Position, float Speed) : Component;

public record Rotation(float Degrees) : Component;

public record AddRotation(float Degrees) : Component;

public record Trigger : Component
{
    public IEnumerable<Component> Events = new List<Component>();

    public Trigger() { }

    public Trigger(IEnumerable<Component> components)
        => (Events) = (components);
}

public record Collide : Trigger
{
    public Collide(params Component[] components)
        => (Events) = (components);
}

public record Click : Trigger
{
    public Click(params Component[] components)
        => (Events) = (components);
}

public record RemoveEntity() : Component;

public static class Event
{
    public static State System(State state, string id, IEnumerable<Component> events)
    {
        foreach (var ev in events)
        {
            switch (ev)
            {
                case AddRotation rotate:
                    {
                        state = state.With(id, new Rotation(
                            (state[id].Get<Rotation>()?.Degrees ?? 0) +
                            rotate.Degrees));
                    }
                    break;

                case RemoveEntity remove:
                    {
                        state = state.Without(id);
                    }
                    break;
            }
        }

        return state;
    }
}