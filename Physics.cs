using Ecs;
using Godot;
using System.Linq;

public record Speed(float Value) : Component;

public record Move(Position Start, Position End) : Component;

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

        foreach (var (id, path) in paths.Removed)
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            var tween = game.GetNodeOrNull<Tween>($"{id}/path");
            if (node == null || tween == null) continue;

            tween.StopAll();
            node.RemoveChild(tween);
            tween.QueueFree();
        }

        foreach (var (id, path) in paths.Added.Concat(paths.Changed))
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            var tween = game.GetNodeOrNull<Tween>($"{id}/path");

            if (node == null) continue;

            if (tween == null)
            {
                tween = new Tween()
                {
                    Name = "path"
                };
                node.AddChild(tween);
            }

            tween.StopAll();

            if (Renderer.HasConnection(tween, "tween_all_completed", nameof(game._Event)))
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
                id, new GodotWrapper(path with { Commands = path.Commands.Concat(new [] { new RemoveComponent(path) })})
            });
        }

        return state;
    }
}