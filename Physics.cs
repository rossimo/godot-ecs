using Ecs;
using Godot;
using System.Linq;

public record Speed : Component
{
    public float Value;
}

public record Move : Component
{
    public Position Destination;
}

public record Velocity : Component
{
    public float X;
    public float Y;
}

public record CollideEvent : Event
{
    public CollideEvent(params Task[] tasks)
        => (Tasks) = (tasks);
}

public static class Physics
{
    public static State System(State previous, State state, Game game)
    {
        var collides = Diff.Compare<CollideEvent>(previous, state);

        foreach (var (id, collide) in collides.Removed)
        {
            var node = game.GetNodeOrNull<Node2D>($"{id}-physics");
            if (node == null) continue;

            if (node.IsConnected("area_entered", game, nameof(game._Event)))
            {
                node.Disconnect("area_entered", game, nameof(game._Event));
            }
        }

        foreach (var (id, collide) in collides.Added.Concat(collides.Changed))
        {
            var entity = state[id];
            var position = entity.Get<Position>() ?? new Position { X = 0, Y = 0 };
            var node = game.GetNodeOrNull<KinematicBody2D>($"{id}-physics");
            if (node == null)
            {
                node = new KinematicBody2D()
                {
                    Name = $"{id}-physics",
                    Position = new Vector2(position.X, position.Y)
                };
                game.AddChild(node);
            }

            foreach (Node child in node.GetChildren())
            {
                node.RemoveChild(child);
                child.QueueFree();
            }

            var sprite = state[id].Get<Sprite>();
            if (sprite != null)
            {
                var texture = GD.Load<Texture>(sprite.Image);
                var scale = state[id].Get<Scale>() ?? new Scale { X = 1, Y = 1 };

                node.AddChild(new CollisionShape2D()
                {
                    Shape = new RectangleShape2D()
                    {
                        Extents = new Vector2(
                            texture.GetHeight() * scale.X,
                            texture.GetWidth() * scale.Y) / 2f
                    }
                });
            }

            if (node.IsConnected("area_entered", game, nameof(game._Event)))
            {
                node.Disconnect("area_entered", game, nameof(game._Event));
            }

            node.Connect("area_entered", game, nameof(game._Event), new Godot.Collections.Array() {
                id, new GodotWrapper(collide)
            });
        }

        foreach (var (id, velocity) in state.Get<Velocity>())
        {
            var physics = game.GetNodeOrNull<KinematicBody2D>($"{id}-physics");
            if (physics == null) continue;

            var entity = state[id];
            var speed = entity.Get<Speed>();
            var travel = new Vector2(velocity.X, velocity.Y) * (speed?.Value ?? 1f);

            var (move, position) = entity.Get<Move, Position>();
            if (move != null && position != null)
            {
                var velocityDistance = travel.DistanceTo(new Vector2(0, 0));
                var moveDistance = new Vector2(position.X, position.Y)
                    .DistanceTo(new Vector2(move.Destination.X, move.Destination.Y));

                if (moveDistance < velocityDistance)
                {
                    state = state.Without<Move>(id);
                    state = state.Without<Velocity>(id);
                    travel *= (moveDistance / velocityDistance);
                }
            }

            var oldPosition = physics.Position;
            physics.MoveAndCollide(travel);
            var newPosition = physics.Position;

            if (oldPosition.x != newPosition.x || oldPosition.y != newPosition.y)
            {
                var sprite = game.GetNodeOrNull<Godot.Sprite>($"{id}");
                if (sprite == null) continue;

                var tween = sprite.GetNodeOrNull<Tween>("move");
                if (tween == null)
                {
                    tween = new Tween()
                    {
                        Name = "move"
                    };
                    sprite.AddChild(tween);
                }

                tween.RemoveAll();

                tween.InterpolateProperty(sprite, "position",
                    sprite.Position,
                    newPosition,
                    1f / 60f);

                tween.Start();
            }
        }

        foreach (var (id, position) in state.Get<Position>())
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            if (node == null) continue;

            if (position.X != node.Position.x || position.Y != node.Position.y)
            {
                state = state.With(id, new Position { X = node.Position.x, Y = node.Position.y, Self = true });
            }
        }

        return state;
    }
}