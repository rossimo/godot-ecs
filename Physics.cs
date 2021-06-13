using Ecs;
using Godot;
using System.Linq;

public record Speed(float Value) : Component;

public record Move(Position Target) : Component;

public record Velocity(float X, float Y) : Component;

public record Path(Position Position, float Speed) : Event;

public static class Physics
{
    public static State System(State previous, State state, Game game)
    {
        var paths = Diff.Compare<Path>(previous, state);

        foreach (var (id, velocity) in state.Get<Velocity>())
        {
            var node = game.GetNodeOrNull<KinematicBody2D>(id);
            if (node == null) continue;

            var entity = state[id];
            var speed = entity.Get<Speed>() ?? new Speed(1f);
            var travel = new Vector2(velocity.X, velocity.Y) * speed.Value;

            var (move, position) = entity.Get<Move, Position>();
            if (move != null && position != null)
            {
                var velocityDistance = travel.DistanceTo(new Vector2(0, 0));
                var moveDistance = new Vector2(position.X, position.Y)
                    .DistanceTo(new Vector2(move.Target.X, move.Target.Y));

                if (moveDistance < velocityDistance)
                {
                    state = state.Without<Move>(id);
                    state = state.Without<Velocity>(id);
                    travel *= (moveDistance / velocityDistance);
                }
            }

            node.MoveAndCollide(travel);
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

        foreach (var (id, path) in paths.Removed)
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            var tween = game.GetNodeOrNull<Tween>($"{id}/path");
            if (node == null || tween == null) continue;

            tween.StopAll();
        }

        foreach (var (id, path) in paths.Added.Concat(paths.Changed))
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            var tween = game.GetNodeOrNull<Tween>($"{id}/path");
            if (node == null) continue;

            tween.StopAll();

            if (tween.IsConnected("tween_all_completed", game, nameof(game._Event)))
            {
                tween.Disconnect("tween_all_completed", game, nameof(game._Event));
            }

            var entity = state[id];
            var position = entity?.Get<Position>() ?? new Position(0, 0);

            var start = new Vector2(node.Position.x, node.Position.y);
            var end = new Vector2(path.Position.X, path.Position.Y);
            var distance = start.DistanceTo(end);
            var duration = distance / path.Speed;
            tween.InterpolateProperty(node, "position", start, end, duration);
            tween.Start();

            tween.Connect("tween_all_completed", game, nameof(game._Event), new Godot.Collections.Array() {
                id, new GodotWrapper(path with { Commands = path.Commands.Concat(new [] { new RemoveComponent(path) }).ToArray()})
            });
        }

        return state;
    }
}