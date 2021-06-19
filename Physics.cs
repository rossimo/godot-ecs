using Ecs;
using Godot;
using System;
using System.Linq;

public record Speed : Component
{
    public float Value;
}

public record Move : Component
{
    public Position Destination;
}

public record Collision : Component
{
}

public record Velocity : Component
{
    public float X;
    public float Y;
}

public record EnterEvent : Event
{
    public EnterEvent(params Task[] tasks)
        => (Tasks) = (tasks);
}

public static class Physics
{
    public static State System(State previous, State state, Game game, float delta)
    {
        var (enters, collisions) = Diff.Compare<EnterEvent, Collision>(previous, state);

        foreach (var (id, enter) in enters.Removed)
        {
            var node = game.GetNodeOrNull<Node2D>($"{id}-physics");
            if (node == null) continue;

            var area = node.GetNodeOrNull<Area2D>("area");
            if (area != null)
            {
                node.RemoveChild(area);
                area.QueueFree();
            }
        }

        foreach (var (id, enter) in enters.Added.Concat(enters.Changed))
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

            var area = node.GetNodeOrNull<Node2D>("area");
            if (area != null)
            {
                node.RemoveChild(area);
                area.QueueFree();
            }

            area = new Area2D()
            {
                Name = "area"
            };
            node.AddChild(area);

            var sprite = state[id].Get<Sprite>();
            if (sprite != null)
            {
                var texture = GD.Load<Texture>(sprite.Image);
                var scale = state[id].Get<Scale>() ?? new Scale { X = 1, Y = 1 };

                area.AddChild(new CollisionShape2D()
                {
                    Shape = new RectangleShape2D()
                    {
                        Extents = new Vector2(
                            texture.GetHeight() * scale.X,
                            texture.GetWidth() * scale.Y) / 2f
                    }
                });
            }

            if (area.IsConnected("area_entered", game, nameof(game._Event)))
            {
                area.Disconnect("area_entered", game, nameof(game._Event));
            }

            area.Connect("area_entered", game, nameof(game._Event), new Godot.Collections.Array() {
                id, new GodotWrapper(enter)
            });
        }

        foreach (var (id, body) in collisions.Removed)
        {
            var node = game.GetNodeOrNull<KinematicBody2D>($"{id}-physics");
            if (node == null) continue;

            var collision = node.GetNodeOrNull<Node2D>("collision");
            if (collision != null)
            {
                node.RemoveChild(collision);
                collision.QueueFree();
            }
        }

        foreach (var (id, body) in collisions.Changed.Concat(collisions.Added))
        {
            var entity = state[id];
            var node = game.GetNodeOrNull<KinematicBody2D>($"{id}-physics");
            var position = entity.Get<Position>() ?? new Position { X = 0, Y = 0 };
            if (node == null)
            {
                node = new KinematicBody2D()
                {
                    Name = $"{id}-physics",
                    Position = new Vector2(position.X, position.Y)
                };
                game.AddChild(node);
            }

            var collision = node.GetNodeOrNull<Node2D>("collision");
            if (collision != null)
            {
                node.RemoveChild(collision);
                collision.QueueFree();
            }

            var sprite = state[id].Get<Sprite>();
            if (sprite != null)
            {
                var texture = GD.Load<Texture>(sprite.Image);
                var scale = state[id].Get<Scale>() ?? new Scale { X = 1, Y = 1 };

                node.AddChild(new CollisionShape2D()
                {
                    Name = "collision",
                    Shape = new RectangleShape2D()
                    {
                        Extents = new Vector2(
                            texture.GetHeight() * scale.X,
                            texture.GetWidth() * scale.Y) / 2f
                    }
                });

                node.AddChild(new RectangleNode()
                {
                    Rect = new Rect2(0, 0, texture.GetHeight() * scale.X, texture.GetWidth() * scale.Y),
                    Color = new Godot.Color(1, 0, 0)
                });
            }
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
                    delta);

                tween.Start();
            }
        }

        foreach (var (id, position) in state.Get<Position>())
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            if (node == null) continue;

            if (position.X != node.Position.x || position.Y != node.Position.y)
            {
                state = state.With(id, new Position { X = node.Position.x, Y = node.Position.y });
            }
        }

        return state;
    }
}

public class RectangleNode : Node2D
{
    public Rect2 Rect = new Rect2();
    public Godot.Color Color = new Godot.Color(1, 0, 0);

    public override void _Draw()
    {
        var vertices = new[] {
            new Vector2(0, 0),
            new Vector2(Rect.Size.x, 0),
            new Vector2(Rect.Size.x, Rect.Size.y),
            new Vector2(0,  Rect.Size.y),
            new Vector2(0, 0)
        }.Select(vert => vert - new Vector2(Rect.Size.x / 2, Rect.Size.y / 2)).ToArray();

        DrawPolyline(vertices, Color);
    }
}