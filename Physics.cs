using Ecs;
using Godot;

public record Speed(float Value) : Component;

public record Move(Position Start, Position End) : Component;

public record Velocity(float X, float Y) : Component;

public static class Physics
{
    public static State System(State state, Game game)
    {
        foreach (var (id, velocity) in state.Get<Velocity>())
        {
            var node = game.GetNodeOrNull<KinematicBody2D>(id);
            if (node == null) continue;

            var speed = state[id]?.Get<Speed>() ?? new Speed(1f);

            node.MoveAndCollide(new Vector2(velocity.X, velocity.Y) * speed.Value);
        }

        foreach (var (id, position) in state.Get<Position>())
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            if (node == null) continue;

            if (position.X != node.Position.x || position.Y != node.Position.y)
            {
                state = state.With(id, new Position(node.Position.x, node.Position.y, true));
            }
        }

        foreach (var (id, move) in state.Get<Move>())
        {
            var entity = state[id];
            var position = entity.Get<Position>();

            var traveled = new Vector2(move.Start.X, move.Start.Y)
                .DistanceTo(new Vector2(position.X, position.Y));

            var max = new Vector2(move.Start.X, move.Start.Y)
                .DistanceTo(new Vector2(move.End.X, move.End.Y));

            if (traveled >= max)
            {
                state = state.Without<Move>(id);
                state = state.Without<Velocity>(id);
            }
        }

        return state;
    }
}