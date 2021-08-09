using Godot;
using System;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Linq;

public struct Tick
{
    public ulong Value;
}

[EditorComponent]
public struct Speed
{
    public float Value;
}

[EditorComponent]
public struct Destination
{
    public float X;
    public float Y;
}

[EditorComponent]
public struct Direction
{
    public float X;
    public float Y;
}

[EditorComponent]
public struct Collision { }

[EditorComponent]
public struct Area { }

public struct PhysicsNode
{
    public KinematicBody2D Node;
}

public struct PositionTween
{
    public Tween Tween;
}

public struct AreaNode
{
    public Area2D Node;
}

public class PhysicsSystem : IEcsInitSystem, IEcsRunSystem
{
    public static float PHYSICS_FPS = $"{ProjectSettings.GetSetting("physics/common/physics_fps")}".ToFloat();

    public static ulong MillisToTicks(ulong millis)
    {
        return Convert.ToUInt64((Convert.ToSingle(millis) / 1000f) * PHYSICS_FPS);
    }

    [EcsWorld] readonly EcsWorld world = default;
    [EcsShared] readonly Shared shared = default;
    [EcsPool] readonly EcsPool<Move> moves = default;
    [EcsPool] readonly EcsPool<Speed> speeds = default;
    [EcsPool] readonly EcsPool<Direction> directions = default;
    [EcsPool] readonly EcsPool<Tick> ticks = default;
    [EcsPool] readonly EcsPool<PhysicsNode> physicsNodes = default;
    [EcsPool] readonly EcsPool<RenderNode> renders = default;
    [EcsPool] readonly EcsPool<PositionTween> positionTweens = default;
    [EcsPool] readonly EcsPool<Area> areas = default;
    [EcsPool] readonly EcsPool<AreaNode> areaNodes = default;
    [EcsPool] readonly EcsPool<Sprite> sprites = default;
    [EcsPool] readonly EcsPool<Collision> collisions = default;
    [EcsPool] readonly EcsPool<EventTrigger<Collision>> collisionTriggers = default;
    [EcsPool] readonly EcsPool<EventTrigger<Area>> areaTriggers = default;
    [EcsPool] readonly EcsPool<EventQueue> eventQueues = default;

    public void Init(EcsSystems systems)
    {
        var world = systems.GetWorld();
        var shared = systems.GetShared<Shared>();

        var physics = shared.Physics = world.NewEntity();
        ticks.Add(physics);
    }

    public void Run(EcsSystems systems)
    {
        var game = shared.Game;
        var ratio = (60f / PHYSICS_FPS);

        ref var ticks = ref this.ticks.Get(shared.Physics);
        ticks.Value++;

        foreach (var entity in world.Filter<PhysicsNode>().Inc<Move>().End())
        {
            ref var nodeComponent = ref physicsNodes.Get(entity);
            ref var move = ref moves.Get(entity);

            var directionVec = nodeComponent.Node.Position
                .DirectionTo(new Vector2(move.Destination.X, move.Destination.Y))
                .Normalized();

            ref var direction = ref directions.Ensure(entity);
            direction.X = directionVec.x;
            direction.Y = directionVec.y;
        }

        foreach (var entity in world.Filter<RenderNode>().Inc<Direction>().End())
        {
            ref var direction = ref directions.Get(entity);
            ref var render = ref renders.Get(entity);
            var speed = speeds.Has(entity)
                ? speeds.Get(entity)
                : new Speed() { Value = 1f };

            var renderNode = render.Node;

            var travel = new Vector2(direction.X, direction.Y) * speed.Value * ratio;
            var percentage = 1f;

            if (moves.Has(entity))
            {
                ref var move = ref moves.Get(entity);

                var tickDistance = travel.DistanceTo(new Vector2(0, 0));
                var moveDistance = renderNode.Position
                    .DistanceTo(new Vector2(move.Destination.X, move.Destination.Y));

                if (moveDistance < tickDistance)
                {
                    percentage = moveDistance / tickDistance;
                    travel *= percentage;

                    moves.Del(entity);
                    directions.Del(entity);
                }
            }

            if (physicsNodes.Has(entity))
            {
                ref var physics = ref physicsNodes.Get(entity);

                var collision = physics.Node.MoveAndCollide(travel);

                travel = physics.Node.Position - render.Node.Position;

                if (collision != null)
                {
                    moves.Del(entity);
                    directions.Del(entity);

                    var target = -1;

                    if (collision.Collider is EntityNode otherNode)
                    {
                        otherNode.Entity.Unpack(world, out target);
                    }

                    ref var eventQueue = ref eventQueues.Get(shared.Events);

                    if (collisionTriggers.Has(entity))
                    {
                        ref var trigger = ref collisionTriggers.Get(entity);
                        eventQueue.Events.AddRange(trigger.Tasks.Select(task => new Event()
                        {
                            Task = task,
                            Source = world.PackEntity(entity),
                            Target = target == -1 ? default : world.PackEntity(target)
                        }));
                    }

                    if (collisionTriggers.Has(target))
                    {
                        ref var trigger = ref collisionTriggers.Get(target);
                        eventQueue.Events.AddRange(trigger.Tasks.Select(task => new Event()
                        {
                            Task = task,
                            Source = target == -1 ? default : world.PackEntity(target),
                            Target = world.PackEntity(entity)
                        }));
                    }
                }
            }

            if (positionTweens.Has(entity))
            {
                ref var tweenComponent = ref positionTweens.Get(entity);

                tweenComponent.Tween.InterpolateProperty(renderNode, "global_position",
                    renderNode.Position,
                    renderNode.Position + travel,
                    (1f / PHYSICS_FPS) * percentage);

                tweenComponent.Tween.Start();
            }
            else
            {
                renderNode.Position = renderNode.Position + travel;
            }
        }

        foreach (var entity in world.Filter<AreaNode>().Inc<Notify<EventTrigger<Area>>>().End())
        {
            ref var areaNode = ref areaNodes.Get(entity);
            ref var trigger = ref areaTriggers.Get(entity);
            var node = areaNode.Node;

            if (node.IsConnected("area_entered", game, nameof(game._Event)))
            {
                node.Disconnect("area_entered", game, nameof(game._Event));
            }

            node.Connect("area_entered", game, nameof(game._Event), new Godot.Collections.Array() {
                new GodotWrapper(world.PackEntity(entity)), new GodotWrapper(trigger.Tasks)
            });
        }

        foreach (int entity in world.Filter<AreaNode>().Inc<DeleteEntity>().End())
        {
            ref var node = ref areaNodes.Get(entity).Node;
            node.GetParent()?.RemoveChild(node);
            node.QueueFree();
        }

        foreach (int entity in world.Filter<PhysicsNode>().Inc<DeleteEntity>().End())
        {
            ref var node = ref physicsNodes.Get(entity).Node;
            node.GetParent()?.RemoveChild(node);
            node.QueueFree();
        }
    }
}