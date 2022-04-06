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
public struct Direction
{
    public float X;
    public float Y;
}

[Editor, IsMany, IsEvent]
public struct Collision { }

[Editor, IsMany, IsEvent]
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

public struct Timer
{
    public ulong RemainingTicks;
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
    [EcsPool] readonly EcsPool<Timer> timers = default;
    [EcsPool] readonly EcsPool<PhysicsNode> physicsNodes = default;
    [EcsPool] readonly EcsPool<RenderNode> renders = default;
    [EcsPool] readonly EcsPool<PositionTween> positionTweens = default;
    [EcsPool] readonly EcsPool<AreaNode> areaNodes = default;
    [EcsPool] readonly EcsPool<Many<Event<Area>>> areaEvents = default;
    [EcsPool] readonly EcsPool<Collision> collisionEvents = default;
    [EcsPool] readonly EcsPool<Many<Event<Collision>>> collisionEventsTriggers = default;

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

        foreach (var entity in world.Filter<Timer>().End())
        {
            ref var timer = ref timers.Get(entity);
            timer.RemainingTicks--;

            if (timer.RemainingTicks <= 0)
            {
                timers.Del(entity);
            }
        }

        foreach (var entity in world.Filter<PhysicsNode>().Inc<Move>().End())
        {
            ref var nodeComponent = ref physicsNodes.Get(entity);
            ref var move = ref moves.Get(entity);

            if (move.X == nodeComponent.Node.Position.x &&
                move.Y == nodeComponent.Node.Position.y)
            {
                moves.Del(entity);
                directions.Del(entity);
                continue;
            }

            var directionVec = nodeComponent.Node.Position
                .DirectionTo(new Vector2(move.X, move.Y))
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
                    .DistanceTo(new Vector2(move.X, move.Y));

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
                    var other = collision.Collider.GetEntity(world);

                    collisionEvents.Ensure(entity);
                    if (other != -1)
                    {
                        collisionEvents.Ensure(other);
                    }

                    moves.Del(entity);
                    directions.Del(entity);

                    if (collisionEventsTriggers.Has(entity))
                    {
                        ref var events = ref collisionEventsTriggers.Get(entity);
                        foreach (var ev in events)
                        {
                            ev.Add(world, entity, other);
                        }
                    }

                    if (other != -1 && collisionEventsTriggers.Has(other))
                    {
                        ref var events = ref collisionEventsTriggers.Get(other);
                        foreach (var ev in events)
                        {
                            ev.Add(world, other, entity);
                        }
                    }
                }
            }

            if (positionTweens.Has(entity))
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

        foreach (var entity in world.Filter<Many<Event<Area>>>().Inc<Notify<AreaNode>>().End())
        {
            ref var area = ref areaNodes.Get(entity);
            ref var ev = ref areaEvents.Get(entity);
            var node = area.Node;

            if (node.IsConnected("area_entered", game, nameof(game.AreaEvent)))
            {
                node.Disconnect("area_entered", game, nameof(game.AreaEvent));
            }

            var packed = world.PackEntity(entity);
            node.Connect("area_entered", game, nameof(game.AreaEvent), new Godot.Collections.Array() {
                packed.Id,
                packed.Gen
            });
        }

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