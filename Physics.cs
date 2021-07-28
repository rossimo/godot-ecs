using Godot;
using System;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Linq;
using System.Collections.Generic;

public struct Ticks
{
    public ulong Tick;
}

public struct Speed
{
    public float Value;
}

public struct Destination
{
    public Position Position;
}

public struct Direction
{
    public float X;
    public float Y;
}

public struct Collision { }

public struct Area { }

/*
public struct AreaEnterEvent : Event
{
    public AreaEnterEvent(params Task[] tasks) => (Tasks) = (tasks);

    public override string ToString() => base.ToString();
}
*/
public struct PhysicsNode
{
    public KinematicBody2D Node;
}

public struct AreaNode
{
    public Area2D Node;
}

public class Physics : IEcsInitSystem, IEcsRunSystem
{
    public static float PHYSICS_FPS = $"{ProjectSettings.GetSetting("physics/common/physics_fps")}".ToFloat();

    public static ulong MillisToTicks(ulong millis)
    {
        return Convert.ToUInt64((Convert.ToSingle(millis) / 1000f) * PHYSICS_FPS);
    }

    [EcsWorld] readonly EcsWorld world = default;
    [EcsShared] readonly Game game = default;
    [EcsPool] readonly EcsPool<Move> moves = default;
    [EcsPool] readonly EcsPool<Speed> speeds = default;
    [EcsPool] readonly EcsPool<Direction> directions = default;
    [EcsPool] readonly EcsPool<Position> positions = default;
    [EcsPool] readonly EcsPool<Notify<Position>> notifyPositions = default;
    [EcsPool] readonly EcsPool<Ticks> ticks = default;
    [EcsPool] readonly EcsPool<PhysicsNode> physicsNodes = default;
    [EcsPool] readonly EcsPool<Area> areas = default;
    [EcsPool] readonly EcsPool<AreaNode> areaNodes = default;
    [EcsPool] readonly EcsPool<Sprite> sprites = default;
    [EcsPool] readonly EcsPool<Scale> scales = default;
    [EcsPool] readonly EcsPool<Collision> collisions = default;
    [EcsPool] readonly EcsPool<Trigger<Collision>> collisionTriggers = default;

    public void Init(EcsSystems systems)
    {
        var world = systems.GetWorld();

        var physics = world.NewEntity();
        ticks.Add(physics);
    }

    public void Run(EcsSystems systems)
    {
        var ratio = (60f / PHYSICS_FPS);

        foreach (var entity in world.Filter<Ticks>().End())
        {
            ref var component = ref ticks.Get(entity);
            component.Tick++;
        }

        var needPhysics = new HashSet<int>();
        foreach (var entity in world.Filter<Area>().Exc<PhysicsNode>().End())
            needPhysics.Add(entity);
        foreach (var entity in world.Filter<Position>().Exc<PhysicsNode>().End())
            needPhysics.Add(entity);
        foreach (var entity in world.Filter<Collision>().Exc<PhysicsNode>().End())
            needPhysics.Add(entity);

        foreach (var entity in needPhysics)
        {
            var node = new KinematicBody2D()
            {
                Name = entity + "-physics"
            };

            if (!areas.Has(entity) && !collisions.Has(entity))
            {
                node.CollisionMask = 0;
            }

            if (positions.Has(entity))
            {
                ref var position = ref positions.Get(entity);
                node.Position = new Vector2(position.X, position.Y);
            }

            ref var component = ref physicsNodes.Add(entity);
            component.Node = node;

            game.AddChild(node);
        }

        foreach (var entity in world.Filter<PhysicsNode>().Inc<Notify<Collision>>().Inc<Sprite>().End())
        {
            ref var physicsNode = ref physicsNodes.Get(entity);
            ref var sprite = ref sprites.Get(entity);

            var node = physicsNode.Node;

            var scale = scales.Has(entity)
                ? scales.Get(entity)
                : new Scale { X = 1, Y = 1 };

            var collision = node.GetNodeOrNull<Node2D>("collision");
            if (collision != null)
            {
                collision.RemoveAndSkip();
                collision.QueueFree();
            }

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

        foreach (var entity in world.Filter<PhysicsNode>().Inc<Move>().Inc<Position>().Inc<Speed>().End())
        {
            ref var position = ref positions.Get(entity);
            ref var move = ref moves.Get(entity);
            ref var speed = ref speeds.Get(entity);

            var directionVec = new Vector2(position.X, position.Y)
                .DirectionTo(new Vector2(move.Destination.X, move.Destination.Y))
                .Normalized();

            ref var direction = ref directions.Ensure(entity);
            direction.X = directionVec.x;
            direction.Y = directionVec.y;
        }

        foreach (var entity in world.Filter<PhysicsNode>().Inc<Direction>().Inc<Position>().Inc<Speed>().End())
        {
            ref var direction = ref directions.Get(entity);
            ref var physicsNode = ref physicsNodes.Get(entity);
            ref var position = ref positions.Get(entity);
            ref var speed = ref speeds.Get(entity);

            var node = physicsNode.Node;

            var velocity = new Vector2(direction.X, direction.Y) * speed.Value * ratio;

            if (moves.Has(entity))
            {
                ref var move = ref moves.Get(entity);

                var tickDistance = velocity.DistanceTo(new Vector2(0, 0));
                var moveDistance = new Vector2(position.X, position.Y)
                    .DistanceTo(new Vector2(move.Destination.X, move.Destination.Y));

                if (moveDistance < tickDistance)
                {
                    velocity *= moveDistance / tickDistance;

                    moves.Del(entity);
                    directions.Del(entity);
                }
            }

            var collision = node.MoveAndCollide(velocity);

            var positionVec = node.Position;
            notifyPositions.Ensure(entity);
            position.X = positionVec.x;
            position.Y = positionVec.y;

            if (collision != null)
            {
                moves.Del(entity);
                directions.Del(entity);

                var other = Convert.ToInt32((collision.Collider as Node).Name.Split('-').First());

                if (collisionTriggers.Has(entity))
                {
                    ref var trigger = ref collisionTriggers.Get(entity);
                    foreach (var task in trigger.Tasks)
                    {
                        task.Run(world, entity, other);
                    }
                }

                if (collisionTriggers.Has(other))
                {
                    ref var trigger = ref collisionTriggers.Get(other);
                    foreach (var task in trigger.Tasks)
                    {
                        task.Run(world, other, entity);
                    }
                }
            }
        }

        foreach (var entity in world.Filter<PhysicsNode>().Inc<Area>().Inc<Sprite>().Exc<AreaNode>().End())
        {
            ref var physicsNode = ref physicsNodes.Get(entity);
            ref var area = ref areas.Get(entity);
            ref var sprite = ref sprites.Get(entity);
            var scale = scales.Has(entity)
                ? scales.Get(entity)
                : new Scale { X = 1, Y = 1 };

            var node = new Area2D()
            {
                Name = "area"
            };
            physicsNode.Node.AddChild(node);

            var texture = GD.Load<Texture>(sprite.Image);

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
                Name = "outline",
                Rect = new Rect2(0, 0, texture.GetHeight() * scale.X, texture.GetWidth() * scale.Y),
                Color = new Godot.Color(0, 0, 1)
            });

            ref var areaNode = ref areaNodes.Add(entity);
            areaNode.Node = node;
        }

        foreach (int entity in world.Filter<PhysicsNode>().Inc<AreaNode>().Inc<Delete>().End())
        {
            ref var physicsNode = ref physicsNodes.Get(entity);
            ref var areaNode = ref areaNodes.Get(entity);

            var node = areaNode.Node;
            physicsNode.Node.RemoveChild(node);
            node.QueueFree();
        }

        foreach (int entity in world.Filter<PhysicsNode>().Inc<Delete>().End())
        {
            ref var physicsNode = ref physicsNodes.Get(entity);

            var node = physicsNode.Node;
            game.RemoveChild(node);
            node.QueueFree();
        }

        /*

        foreach (var (id, ev) in areaEnterEvents.Removed)
        {
            var node = state.Get<PhysicsNode>(id)?.Node;
            var area = node?.GetNodeOrNull<Node2D>("area");
            if (area == null) continue;

            if (area.IsConnected("area_entered", game, nameof(game._Event)))
            {
                area.Disconnect("area_entered", game, nameof(game._Event));
            }
        }

        foreach (var (id, ev) in areaEnterEvents.Added.Concat(areaEnterEvents.Changed))
        {
            var node = state.Get<PhysicsNode>(id)?.Node;
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
            var node = state.Get<PhysicsNode>(id)?.Node;
            var collision = node?.GetNodeOrNull<Node2D>("collision");

            if (collision != null)
            {
                collision.RemoveAndSkip();
                collision.QueueFree();
            }
        }
        */
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