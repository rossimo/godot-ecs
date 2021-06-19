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
    public static float PHYSICS_FPS = float.Parse(ProjectSettings.GetSetting("physics/common/physics_fps").ToString());

    public static State System(State previous, State state, Game game, float delta)
    {
        if (previous != state)
        {
            var (enters, collisions) = Diff.Compare<EnterEvent, Collision>(previous, state);

            foreach (var (id, enter) in enters.Removed)
            {
                var collision = state.Get(id)?.Get<Collision>();

                var node = game.GetNodeOrNull<Node2D>($"{id}-physics");
                if (node == null) continue;

                if (collision == null)
                {
                    game.RemoveChild(node);
                    node.QueueFree();
                }
                else
                {
                    var area = node.GetNodeOrNull<Area2D>("area");
                    if (area != null)
                    {
                        node.RemoveChild(area);
                        area.QueueFree();
                    }
                }
            }

            foreach (var (id, enter) in enters.Added.Concat(enters.Changed))
            {
                var entity = state[id];
                var (sprite, position, scale) = entity.Get<Sprite, Position, Scale>();
                position = position ?? new Position { X = 0, Y = 0 };
                scale = scale ?? new Scale { X = 1, Y = 1 };

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

                if (sprite != null)
                {
                    var texture = GD.Load<Texture>(sprite.Image);

                    area.AddChild(new CollisionShape2D()
                    {
                        Shape = new RectangleShape2D()
                        {
                            Extents = new Vector2(
                                texture.GetHeight() * scale.X,
                                texture.GetWidth() * scale.Y) / 2f
                        }
                    });

                    area.AddChild(new RectangleNode()
                    {
                        Rect = new Rect2(0, 0, texture.GetHeight() * scale.X, texture.GetWidth() * scale.Y),
                        Color = new Godot.Color(0, 0, 1)
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
                var enter = state.Get(id)?.Get<EnterEvent>();

                var node = game.GetNodeOrNull<KinematicBody2D>($"{id}-physics");
                if (node == null) continue;

                if (enter == null)
                {
                    game.RemoveChild(node);
                    node.QueueFree();
                }
                else
                {
                    var collision = node.GetNodeOrNull<Node2D>("collision");
                    if (collision != null)
                    {
                        node.RemoveChild(collision);
                        collision.QueueFree();
                    }
                }
            }

            foreach (var (id, body) in collisions.Changed.Concat(collisions.Added))
            {
                var entity = state[id];
                var (sprite, scale) = entity.Get<Sprite, Scale>();
                scale = scale ?? new Scale { X = 1, Y = 1 };

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

                if (sprite != null)
                {
                    var texture = GD.Load<Texture>(sprite.Image);

                    var shape = new CollisionShape2D()
                    {
                        Name = "collision",
                        Shape = new RectangleShape2D()
                        {
                            Extents = new Vector2(
                                texture.GetHeight() * scale.X,
                                texture.GetWidth() * scale.Y) / 2f
                        }
                    };
                    node.AddChild(shape);

                    shape.AddChild(new RectangleNode()
                    {
                        Rect = new Rect2(0, 0, texture.GetHeight() * scale.X, texture.GetWidth() * scale.Y),
                        Color = new Godot.Color(1, 0, 0)
                    });
                }
            }
        }

        foreach (var (id, velocity) in state.Get<Velocity>())
        {
            var entity = state[id];
            var (move, position, speed) = entity.Get<Move, Position, Speed>();
            speed = speed ?? new Speed { Value = 1f };

            var physics = game.GetNodeOrNull<KinematicBody2D>($"{id}-physics");
            if (physics == null) continue;

            var travel = new Vector2(velocity.X, velocity.Y) * speed.Value * (delta / (30f / 1000f));
            var velocityDistance = travel.DistanceTo(new Vector2(0, 0));
            var moveDistance = new Vector2(position.X, position.Y)
                .DistanceTo(new Vector2(move.Destination.X, move.Destination.Y));

            var oldPosition = physics.Position;

            if (moveDistance < velocityDistance)
            {
                physics.Position = new Vector2(move.Destination.X, move.Destination.Y);
                state = state.Without<Move>(id);
                state = state.Without<Velocity>(id);
            }
            else
            {
                physics.MoveAndCollide(travel);
            }
        }

        foreach (var (id, collision) in state.Get<Collision>())
        {
            var entity = state[id];
            var position = entity.Get<Position>();

            var node = game.GetNodeOrNull<KinematicBody2D>($"{id}-physics");
            if (node == null) continue;

            if (position == null || position.X != node.Position.x || position.Y != node.Position.y)
            {
                state = state.With(id, new Position { X = node.Position.x, Y = node.Position.y });

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
                    node.Position,
                    delta);

                tween.Start();
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
            new Vector2(Rect.Position.x, Rect.Position.y),
            new Vector2(Rect.Position.x + Rect.Size.x, Rect.Position.y),
            new Vector2(Rect.Position.x + Rect.Size.x, Rect.Position.y + Rect.Size.y),
            new Vector2(Rect.Position.x, Rect.Position.y + Rect.Size.y),
            new Vector2(Rect.Position.x, Rect.Position.y)
        }.Select(vert => vert - new Vector2(Rect.Size.x / 2, Rect.Size.y / 2)).ToArray();

        DrawPolyline(vertices, Color);
    }
}