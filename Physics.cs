using DefaultEcs;
using System;
using Godot;
using System.Linq;
using System.Collections.Generic;

public record Ticks
{
    public ulong Tick;
}

public record Speed
{
    public float Value;
}

public record Destination
{
    public Position Position;
}

public record Velocity
{
    public float X;
    public float Y;
}

public record Collision;

public record CollisionEvent : Event
{
    public CollisionEvent(params Task[] tasks) => (Tasks) = (tasks);

    public override string ToString() => base.ToString();
}

public record Area;

public record AreaEnterEvent : Event
{
    public AreaEnterEvent(params Task[] tasks) => (Tasks) = (tasks);

    public override string ToString() => base.ToString();
}

public record Move
{
    public Position Destination;
}

public record PhysicsNode
{
    public EntityKinematicBody2D Node;
}

public class Physics
{
    public static int ENTITY = 1;

    public static float PHYSICS_FPS = $"{ProjectSettings.GetSetting("physics/common/physics_fps")}".ToFloat();

    private Diff<Area> areas;
    private Diff<AreaEnterEvent> areaEnterEvents;
    private Diff<Collision> collisions;
    private Diff<Move> moves;
    private Diff<Position> positions;
    private Diff<PhysicsNode> physics;
    private DefaultEcs.World world;
    private EntitySet velocities;

    public Physics(DefaultEcs.World world)
    {
        this.world = world;
        world.Set(new Ticks());

        areas = new Diff<Area>(world);
        areaEnterEvents = new Diff<AreaEnterEvent>(world);
        collisions = new Diff<Collision>(world);
        moves = new Diff<Move>(world);
        positions = new Diff<Position>(world);
        physics = new Diff<PhysicsNode>(world);
        velocities = world.GetEntities().With<Velocity>().AsSet();
    }

    public static ulong MillisToTicks(ulong millis)
    {
        return Convert.ToUInt64((Convert.ToSingle(millis) / 1000f) * PHYSICS_FPS);
    }

    public void System(Game game, float delta)
    {
        world.Set(new Ticks
        {
            Tick = (world.TryGet<Ticks>()?.Tick ?? 0) + 1
        });

        var needPhysics = areas.Added().ToArray()
            .Concat(areas.Changed().ToArray())
            .Concat(positions.Added().ToArray())
            .Concat(positions.Changed().ToArray())
            .Concat(collisions.Added().ToArray())
            .Concat(collisions.Changed().ToArray())
            .Distinct();

        var notNeedPhysics = areas.Removed().ToArray()
            .Concat(collisions.Removed().ToArray())
            .Concat(positions.Removed().ToArray())
            .Concat(physics.Removed().ToArray())
            .Distinct()
            .Where(id => !needPhysics.Contains(id));

        foreach (var entity in moves.Added().ToArray().Concat(moves.Changed().ToArray()))
        {
            var id = entity.ID();
            var move = entity.TryGet<Move>();
            entity.Remove<Move>();

            var position = entity.TryGet<Position>();
            var speed = entity.TryGet<Speed>();
            if (position == null) continue;

            speed = speed ?? new Speed { Value = 1f };

            var velocity = new Vector2(position.X, position.Y)
                .DirectionTo(new Vector2(move.Destination.X, move.Destination.Y))
                .Normalized() * speed.Value;

            entity.Set(new Velocity { X = velocity.x, Y = velocity.y });
            entity.Set(new Destination { Position = move.Destination });
        }

        foreach (var entity in notNeedPhysics)
        {
            var node = game.GetNodeOrNull<EntityKinematicBody2D>(entity.ID() + "-physics");
            if (node == null) continue;

            node.RemoveAndSkip();
            node.QueueFree();

            entity.Remove<PhysicsNode>();
        }

        foreach (var entity in needPhysics)
        {
            var id = entity.ID();
            if (entity.Has<PhysicsNode>()) continue;

            var position = entity.TryGet<Position>() ?? new Position { X = 0, Y = 0 };

            var node = new EntityKinematicBody2D()
            {
                Name = id + "-physics",
                Entity = entity,
                Position = new Vector2(position.X, position.Y)
            };

            entity.Set(new PhysicsNode()
            {
                Node = node
            });
            game.AddChild(node);
        }

        foreach (var entity in areas.Removed())
        {
            var id = entity.ID();

            var node = game.GetNodeOrNull<Node2D>(id + "-physics");
            var area = node?.GetNodeOrNull<Area2D>("area");
            if (area == null) continue;

            area.RemoveAndSkip();
            area.QueueFree();
        }

        foreach (var entity in areas.Added().ToArray().Concat(areas.Changed().ToArray()))
        {
            var id = entity.ID();
            var component = entity.TryGet<Area>();

            var node = entity.TryGet<PhysicsNode>()?.Node;
            if (node == null) continue;

            var sprite = entity.TryGet<Sprite>();
            var scale = entity.TryGet<Scale>();
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

        foreach (var entity in areaEnterEvents.Removed())
        {
            var id = entity.ID();
            var component = entity.TryGet<AreaEnterEvent>();

            var node = entity.TryGet<PhysicsNode>()?.Node;
            var area = node?.GetNodeOrNull<Node2D>("area");
            if (area == null) continue;

            if (area.IsConnected("area_entered", game, nameof(game._Event)))
            {
                area.Disconnect("area_entered", game, nameof(game._Event));
            }
        }

        foreach (var entity in areaEnterEvents.Added().ToArray().Concat(areaEnterEvents.Changed().ToArray()))
        {
            var id = entity.ID();
            var ev = entity.TryGet<AreaEnterEvent>();

            var node = entity.TryGet<PhysicsNode>()?.Node;
            var area = node?.GetNodeOrNull<Node2D>("area");
            if (area == null) continue;

            if (area.IsConnected("area_entered", game, nameof(game._Event)))
            {
                area.Disconnect("area_entered", game, nameof(game._Event));
            }

            area.Connect("area_entered", game, nameof(game._Event), new Godot.Collections.Array() {
                new GodotWrapper(entity), new GodotWrapper(ev)
            });
        }

        foreach (var entity in collisions.Removed())
        {
            var node = entity.TryGet<PhysicsNode>()?.Node;
            var collision = node?.GetNodeOrNull<Node2D>("collision");

            if (collision != null)
            {
                collision.RemoveAndSkip();
                collision.QueueFree();
            }
        }

        foreach (var entity in collisions.Changed().ToArray().Concat(collisions.Added().ToArray()))
        {
            var node = entity.TryGet<PhysicsNode>()?.Node;
            if (node == null) continue;

            var sprite = entity.TryGet<Sprite>();
            var scale = entity.TryGet<Scale>();
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

        foreach (var entity in velocities.GetEntities())
        {
            var id = entity.ID();

            var velocity = entity.TryGet<Velocity>();
            var destination = entity.TryGet<Destination>();
            var position = entity.TryGet<Position>();
            var collision = entity.TryGet<Collision>();
            var area = entity.TryGet<Area>();

            var physics = entity.TryGet<PhysicsNode>()?.Node;
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
                entity.Remove<Destination>();
                entity.Remove<Velocity>();

                if (collided == null)
                {
                    physics.Position = new Vector2(destination.Position.X, destination.Position.Y);
                }
                else
                {
                    var other = (collided.Collider as EntityKinematicBody2D).Entity;

                    var ev = entity.TryGet<CollisionEvent>();
                    if (ev != null)
                    {
                        var queue = (entity, other, ev as Event);

                        world.Set(new EventQueue()
                        {
                            Events = world.TryGet<EventQueue>().Events.With(queue)
                        });
                    }

                    var otherEv = other.TryGet<CollisionEvent>();
                    if (otherEv != null)
                    {
                        var queue = (other, entity, otherEv as Event);

                        world.Set(new EventQueue()
                        {
                            Events = world.TryGet<EventQueue>().Events.With(queue)
                        });
                    }
                }
            }

            var physicsPosition = physics.Position;

            entity.Set(new Position { X = physicsPosition.x, Y = physicsPosition.y });
        }

        areas.Complete();
        areaEnterEvents.Complete();
        collisions.Complete();
        moves.Complete();
        positions.Complete();
        physics.Complete();
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