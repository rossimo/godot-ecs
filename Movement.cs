using Ecs;
using System;

public record Player() : Component;

public record Position(float X, float Y, Boolean Self = false) : Component;

public record Move(Position Position, float Speed): Component;

public record Rotation(float Degrees) : Component;

public record AddRotation(float Degrees) : Component;

public record Collide() : Component;

public static class Movement
{
    public static State System(State state)
    {
        foreach (var (id, add) in state.Get<AddRotation>())
        {
            state = state.With(id, state[id].Without<AddRotation>());
            state = state.With(id, new Rotation(
                (state[id].Get<Rotation>()?.Degrees ?? 0) +
                add.Degrees));
        }

        return state;
    }

    public static double Distance(Position start, Position end)
    {
        return Math.Sqrt(Math.Pow((end.X - start.X), 2) + Math.Pow((end.Y - start.Y), 2));
    }
}