using Godot;
using System;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

public struct Tick
{
    public ulong Value;
}

[Editor]
public struct Speed
{
    public float Value;
}

[Editor]
public struct Destination
{
    public float X;
    public float Y;
}

[Editor]
public struct Direction
{
    public float X;
    public float Y;
}

[Editor, IsMany, Listened]
public struct Collision { }

[Editor]
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
    public static float TARGET_PHYSICS_FPS = 60f;
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
    [EcsPool] readonly EcsPool<AreaNode> areaNodes = default;
    [EcsPool] readonly EcsPool<Many<Listener<Collision>>> collisionListeners = default;

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
            var travel = new Vector2(direction.X, direction.Y) * speed.Value
                * (TARGET_PHYSICS_FPS / PHYSICS_FPS);

            var intentDistance = travel.DistanceTo(new Vector2(0, 0));

            if (moves.Has(entity))
            {
                ref var move = ref moves.Get(entity);

                var moveDistance = renderNode.Position
                    .DistanceTo(new Vector2(move.Destination.X, move.Destination.Y));

                if (moveDistance < intentDistance)
                {
                    travel *= moveDistance / intentDistance;

                    moves.Del(entity);
                    directions.Del(entity);
                }
            }

            var startPosition = physicsNodes.Has(entity)
                ? physicsNodes.Get(entity).Node.Position
                : renderNode.Position;

            var endPosition = startPosition + travel;

            if (physicsNodes.Has(entity))
            {
                ref var physics = ref physicsNodes.Get(entity);

                var collision = physics.Node.MoveAndCollide(travel);
                endPosition = physics.Node.Position;

                if (collision != null)
                {
                    moves.Del(entity);
                    directions.Del(entity);

                    var other = -1;
                    if (collision.Collider is EntityNode otherNode)
                    {
                        otherNode.Entity.Unpack(world, out other);
                    }

                    if (collisionListeners.Has(entity))
                    {
                        ref var listeners = ref collisionListeners.Get(entity);
                        listeners.Run(world, entity, other);
                    }

                    /*
                    if (target != -1 && collisionTriggers.Has(target))
                    {
                        ref var trigger = ref collisionTriggers.Get(target);
                        eventQueue.Events.AddRange(trigger.Tasks.Select(task => new Event()
                        {
                            Task = task,
                            Source = target == -1 ? default : world.PackEntity(target),
                            Target = world.PackEntity(entity)
                        }));
                    }
                    */
                }
            }

            if (positionTweens.Has(entity) && Engine.GetFramesPerSecond() >= PHYSICS_FPS)
            {
                ref var tweenComponent = ref positionTweens.Get(entity);

                var deltaRatio = startPosition.DistanceTo(endPosition) / intentDistance;

                tweenComponent.Tween.InterpolateProperty(renderNode, "position",
                    startPosition,
                    endPosition,
                    (1f / PHYSICS_FPS) * deltaRatio);

                tweenComponent.Tween.Start();
            }
            else
            {
                renderNode.Position = endPosition;
            }
        }

        /*
        foreach (var entity in world.Filter<AreaNode>().Inc<Notify<Trigger<Area>>>().End())
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
        */

        foreach (int entity in world.Filter<AreaNode>().Inc<Delete>().End())
        {
            ref var node = ref areaNodes.Get(entity).Node;
            node.GetParent()?.RemoveChild(node);
            node.QueueFree();
        }

        foreach (int entity in world.Filter<PhysicsNode>().Inc<Delete>().End())
        {
            ref var node = ref physicsNodes.Get(entity).Node;
            node.GetParent()?.RemoveChild(node);
            node.QueueFree();
        }
    }
}