using Ecs;

public record Player() : Component;

public record Position(float X, float Y, bool Self = false) : Component;

public record Move(Position Position, float Speed): Component;

public record Rotation(float Degrees) : Component;

public record AddRotation(float Degrees) : Component;

public record Collide(Component Event) : Component;

public record RemoveEntity() : Component;

public static class Event
{
    public static State System(State state, string sourceId, Component ev)
    {
        state = state.With(sourceId, ev);

        foreach (var (id, add) in state.Get<AddRotation>())
        {
            state = state.With(id, state[id].Without<AddRotation>());
            state = state.With(id, new Rotation(
                (state[id].Get<Rotation>()?.Degrees ?? 0) +
                add.Degrees));
        }

        foreach (var (id, remove) in state.Get<RemoveEntity>())
        {
            state = state.Without(id);
        }

        return state;
    }
}