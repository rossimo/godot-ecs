using Godot;
using System;
using SimpleEcs;
using System.Linq;
using System.Collections.Generic;

public record Ticks : Component
{
    public ulong Tick;
}

public record Speed : Component
{
    public float Value;
}

public record Destination : Component
{
    public Position Position;
}

public record Velocity : Component
{
    public float X;
    public float Y;
}

public record Collision : Component;

public record CollisionEvent : Event
{
    public CollisionEvent(params Task[] tasks) => (Tasks) = (tasks);

    public override string ToString() => base.ToString();
}

public record Area : Component;

public record AreaEnterEvent : Event
{
    public AreaEnterEvent(params Task[] tasks) => (Tasks) = (tasks);

    public override string ToString() => base.ToString();
}

public record PhysicsNode : Component
{
    public KinematicBody2D Node;
}

public static class Physics
{
    public static int ENTITY = 1;

    public static float PHYSICS_FPS = $"{ProjectSettings.GetSetting("physics/common/physics_fps")}".ToFloat();

    public static ulong MillisToTicks(ulong millis)
    {
        return Convert.ToUInt64((Convert.ToSingle(millis) / 1000f) * PHYSICS_FPS);
    }

    public static State System(State previous, State state, Game game, float delta)
    {
        var configChange = previous != state;

        state = state.With(ENTITY, new Ticks
        {
            Tick = (state.Ticks(ENTITY)?.Tick ?? 0) + 1
        });

        if (configChange)
        {
            var (areas, areaEnterEvents, collisions, positions, moves, physics) =
                Diff.Compare<Area, AreaEnterEvent, Collision, Position, Move, PhysicsNode>(previous, state);

            var needPhysics = areas.Added.Select(entry => entry.ID)
                .Concat(areas.Changed.Select(entry => entry.ID))
                .Concat(positions.Added.Select(entry => entry.ID))
                .Concat(positions.Changed.Select(entry => entry.ID))
                .Concat(collisions.Added.Select(entry => entry.ID))
                .Concat(collisions.Changed.Select(entry => entry.ID))
                .Distinct();

            var notNeedPhysics = areas.Removed.Select(entry => entry.ID)
                .Concat(collisions.Removed.Select(entry => entry.ID))
                .Concat(positions.Removed.Select(entry => entry.ID))
                .Concat(physics.Removed.Select(entry => entry.ID))
                .Distinct()
                .Where(id => !needPhysics.Contains(id));

            foreach (var (id, move) in moves.Added.Concat(moves.Changed))
            {
                state = state.WithoutMove(id);

                var position = state.Position(id);
                var speed = state.Speed(id);
                if (position == null) continue;

                speed = speed ?? new Speed { Value = 1f };

                var velocity = new Vector2(position.X, position.Y)
                    .DirectionTo(new Vector2(move.Destination.X, move.Destination.Y))
                    .Normalized() * speed.Value;

                state = state.With(id, new Velocity { X = velocity.x, Y = velocity.y });
                state = state.With(id, new Destination { Position = move.Destination });
            }

            foreach (var id in notNeedPhysics)
            {
                var node = game.GetNodeOrNull<KinematicBody2D>(id + "-physics");
                if (node == null) continue;

                node.RemoveAndSkip();
                node.QueueFree();

                state = state.WithoutPhysicsNode(id);
            }

            foreach (var id in needPhysics)
            {
                var existing = state.PhysicsNode(id);
                if (existing?.Node != null) continue;

                var position = state.Position(id) ?? new Position { X = 0, Y = 0 };

                var node = new KinematicBody2D()
                {
                    Name = id + "-physics",
                    Position = new Vector2(position.X, position.Y)
                };

                state = state.With(id, new PhysicsNode()
                {
                    Node = node
                });

                game.AddChild(node);
            }

            foreach (var (id, enter) in areas.Removed)
            {
                var node = game.GetNodeOrNull<Node2D>(id + "-physics");
                var area = node?.GetNodeOrNull<Area2D>("area");
                if (area == null) continue;

                area.RemoveAndSkip();
                area.QueueFree();
            }

            foreach (var (id, component) in areas.Added.Concat(areas.Changed))
            {
                var node = state.PhysicsNode(id)?.Node;
                if (node == null) continue;

                var sprite = state.Sprite(id);
                var scale = state.Scale(id);
                scale = scale ?? new Scale { X = 1, Y = 1 };

                var area = node.GetNodeOrNull<Node2D>("area");
                if (area != null)
                {
                    area.RemoveAndSkip();
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
            }

            foreach (var (id, ev) in areaEnterEvents.Removed)
            {
                var node = state.PhysicsNode(id)?.Node;
                var area = node?.GetNodeOrNull<Node2D>("area");
                if (area == null) continue;

                if (area.IsConnected("area_entered", game, nameof(game._Event)))
                {
                    area.Disconnect("area_entered", game, nameof(game._Event));
                }
            }

            foreach (var (id, ev) in areaEnterEvents.Added.Concat(areaEnterEvents.Changed))
            {
                var node = state.PhysicsNode(id)?.Node;
                var area = node?.GetNodeOrNull<Node2D>("area");
                if (area == null) continue;

                if (area.IsConnected("area_entered", game, nameof(game._Event)))
                {
                    area.Disconnect("area_entered", game, nameof(game._Event));
                }

                area.Connect("area_entered", game, nameof(game._Event), new Godot.Collections.Array() {
                    id, new GodotWrapper(ev)
                });
            }

            foreach (var (id, component) in collisions.Removed)
            {
                var node = state.PhysicsNode(id)?.Node;
                var collision = node?.GetNodeOrNull<Node2D>("collision");

                if (collision != null)
                {
                    collision.RemoveAndSkip();
                    collision.QueueFree();
                }
            }

            foreach (var (id, component) in collisions.Changed.Concat(collisions.Added))
            {
                var node = state.PhysicsNode(id)?.Node;
                if (node == null) continue;

                var sprite = state.Sprite(id);
                var scale = state.Scale(id);
                scale = scale ?? new Scale { X = 1, Y = 1 };

                var collision = node.GetNodeOrNull<Node2D>("collision");
                if (collision != null)
                {
                    collision.RemoveAndSkip();
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

        var positionBatch = new Dictionary<int, Position>();

        foreach (var (id, component) in state.Velocity())
        {
            var velocity = component as Velocity;
            var destination = state.Destination(id);
            var position = state.Position(id);
            var collision = state.Collision(id);
            var area = state.Area(id);

            var physics = state.PhysicsNode(id)?.Node;
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

            KinematicCollision2D collided = null;

            if (collision != null || area != null)
            {
                collided = physics.MoveAndCollide(travel);
            }
            else
            {
                physics.Position += travel;
            }

            if (withinReach || collided != null)
            {
                state = state.WithoutDestination(id);
                state = state.WithoutVelocity(id);

                if (collided == null)
                {
                    physics.Position = new Vector2(destination.Position.X, destination.Position.Y);
                }
                else
                {
                    var collideId = Convert.ToInt32((collided.Collider as Node).Name.Split("-").First());

                    var ev = state.CollisionEvent(id);
                    if (ev != null)
                    {
                        var queue = (id, collideId, ev as Event);
                        state = state.With(Events.ENTITY, new EventQueue()
                        {
                            Events = state.EventQueue(Events.ENTITY).Events.With(queue)
                        });
                    }

                    var otherEv = state.ContainsKey(collideId)
                        ? state.CollisionEvent(collideId)
                        : null;
                    if (otherEv != null)
                    {
                        var queue = (collideId, id, otherEv as Event);

                        state = state.With(Events.ENTITY, new EventQueue()
                        {
                            Events = state.EventQueue(Events.ENTITY).Events.With(queue)
                        });
                    }
                }
            }

            var physicsPosition = physics.Position;
            positionBatch.Add(id, new Position { X = physicsPosition.x, Y = physicsPosition.y });
        }

        state = state.Batch<Position>(positionBatch);

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