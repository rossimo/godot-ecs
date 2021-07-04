using Ecs;
using System;
using Godot;
using System.Linq;

public record Ticks : Component
{
    public uint Tick;
}

public record Speed : Component
{
    public float Value;
}

public record Destination : Component
{
    public Position Position;
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
    public static float PHYSICS_FPS = $"{ProjectSettings.GetSetting("physics/common/physics_fps")}".ToFloat();

    public static State System(State previous, State state, Game game, float delta)
    {
        var configChange = previous != state;

        state = state.With("physics", new Ticks
        {
            Tick = state["physics"].Get<Ticks>().Tick + 1
        });

        if (configChange)
        {
            var (enters, collisions, moves) = Diff.Compare<EnterEvent, Collision, Move>(previous, state);

            foreach (var (id, move) in moves.Added.Concat(moves.Changed))
            {
                state = state.Without<Move>(id);

                var (position, speed) = state[id].Get<Position, Speed>();
                if (position == null) continue;

                speed = speed ?? new Speed { Value = 1f };

                var velocity = new Vector2(position.X, position.Y)
                    .DirectionTo(new Vector2(move.Destination.X, move.Destination.Y))
                    .Normalized() * speed.Value;

                state = state.With(id, new Velocity { X = velocity.x, Y = velocity.y });
                state = state.With(id, new Destination { Position = move.Destination });
            }

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
            var (destination, position) = entity.Get<Destination, Position>();

            var physics = game.GetNodeOrNull<KinematicBody2D>($"{id}-physics");
            if (physics == null) continue;

            var travel = new Vector2(velocity.X, velocity.Y) * (60f / PHYSICS_FPS);
            var withinReach = false;

            if (destination != null)
            {
                var moveDistance = travel.DistanceTo(new Vector2(0, 0));
                var remainingDistance = new Vector2(position.X, position.Y)
                    .DistanceTo(new Vector2(destination.Position.X, destination.Position.Y));

                withinReach = remainingDistance < moveDistance;
            }

            var collided = physics.MoveAndCollide(travel);

            if (withinReach)
            {
                state = state.Without<Destination>(id);
                state = state.Without<Velocity>(id);

                if (collided == null)
                {
                    physics.Position = new Vector2(destination.Position.X, destination.Position.Y);
                }
            }
        }

        foreach (var (id, position) in state.Get<Position>())
        {
            var entity = state[id];

            var node = game.GetNodeOrNull<KinematicBody2D>($"{id}-physics");
            if (node == null) continue;

            if (position?.X != node.Position.x || position?.Y != node.Position.y)
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