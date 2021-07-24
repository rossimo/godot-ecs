using Godot;
using System;
using Leopotam.EcsLite;
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

public struct Velocity
{
    public float X;
    public float Y;
}

public struct Collision { }

/*
public struct CollisionEvent : Event
{
    public CollisionEvent(params Task[] tasks) => (Tasks) = (tasks);

    public override string ToString() => base.ToString();
}
*/

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

public class Physics : IEcsRunSystem
{
    public static float PHYSICS_FPS = $"{ProjectSettings.GetSetting("physics/common/physics_fps")}".ToFloat();

    public static ulong MillisToTicks(ulong millis)
    {
        return Convert.ToUInt64((Convert.ToSingle(millis) / 1000f) * PHYSICS_FPS);
    }


    public Physics()
    {

    }

    public void Run(EcsSystems systems)
    {
        var world = systems.GetWorld();
        var game = systems.GetShared<Game>();

        var ticks = world.GetPool<Ticks>();
        foreach (var entity in world.Filter<Ticks>().End())
        {
            ticks.Get(entity).Tick++;
        }

        var needPhysics = new HashSet<int>();
        foreach (var entity in world.Filter<Area>().Exc<PhysicsNode>().End())
            needPhysics.Add(entity);
        foreach (var entity in world.Filter<Position>().Exc<PhysicsNode>().End())
            needPhysics.Add(entity);
        foreach (var entity in world.Filter<Collision>().Exc<PhysicsNode>().End())
            needPhysics.Add(entity);

        var physicsNodes = world.GetPool<PhysicsNode>();
        foreach (var entity in needPhysics)
        {
            var node = new KinematicBody2D()
            {
                Name = entity + "-physics"
            };

            ref var component = ref physicsNodes.Add(entity);
            component.Node = node;

            game.AddChild(node);
        }

        var moves = world.GetPool<Move>();
        var speeds = world.GetPool<Speed>();
        var velocities = world.GetPool<Velocity>();
        var positions = world.GetPool<Position>();

        foreach (var entity in world.Filter<Move>().Inc<Position>().Inc<Speed>().End())
        {
            ref var move = ref moves.Get(entity);
            ref var position = ref positions.Get(entity);
            ref var speed = ref speeds.Get(entity);

            var newVelocity = new Vector2(position.X, position.Y)
                .DirectionTo(new Vector2(move.Destination.X, move.Destination.Y))
                .Normalized() * speed.Value;

            ref var velocity = ref velocities.Update(entity);
            velocity.X = newVelocity.x;
            velocity.Y = newVelocity.y;
        }

        foreach (var entity in world.Filter<Velocity>().Inc<PhysicsNode>().Inc<Position>().Inc<Move>().End())
        {
            ref var velocity = ref velocities.Get(entity);
            ref var position = ref positions.Get(entity);
            ref var move = ref moves.Get(entity);
            ref var physicsNode = ref physicsNodes.Get(entity);
            var node = physicsNode.Node;

            var movement = new Vector2(velocity.X, velocity.Y) * (60f / PHYSICS_FPS);
            var update = new Vector2(position.X, position.Y) + movement;

            node.Position = update;

            var moveDistance = movement.DistanceTo(new Vector2(0, 0));
            var remainingDistance = new Vector2(position.X, position.Y)
                .DistanceTo(new Vector2(move.Destination.X, move.Destination.Y));

            var withinReach = remainingDistance < moveDistance;
            positions.UpdateEmit(world, entity);

            if (withinReach)
            {
                position.X = move.Destination.X;
                position.Y = move.Destination.Y;

                moves.Del(entity);
                velocities.Del(entity);
            }
            else
            {
                position.X = update.x;
                position.Y = update.y;
            }
        }

        foreach (var entity in world.Filter<Velocity>().Inc<PhysicsNode>().Inc<Position>().Inc<Speed>().End())
        {
            ref var velocity = ref velocities.Get(entity);
            ref var physicsNode = ref physicsNodes.Get(entity);
            ref var position = ref positions.Get(entity);
            ref var speed = ref speeds.Get(entity);
            var node = physicsNode.Node;

            var movement = new Vector2(velocity.X, velocity.Y) * (60f / PHYSICS_FPS) * speed.Value;
            var update = new Vector2(position.X, position.Y) + movement;

            node.Position = update;

            positions.UpdateEmit(world, entity);
            position.X = update.x;
            position.Y = update.y;
        }

        /*
        foreach (var id in notNeedPhysics)
        {
            var node = game.GetNodeOrNull<KinematicBody2D>(id + "-physics");
            if (node == null) continue;

            node.RemoveAndSkip();
            node.QueueFree();

            state = state.Without<PhysicsNode>(id);
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
            var node = state.Get<PhysicsNode>(id)?.Node;
            if (node == null) continue;

            var sprite = state.Get<Sprite>(id);
            var scale = state.Get<Scale>(id);
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

        foreach (var (id, component) in collisions.Changed.Concat(collisions.Added))
        {
            var node = state.Get<PhysicsNode>(id)?.Node;
            if (node == null) continue;

            var sprite = state.Get<Sprite>(id);
            var scale = state.Get<Scale>(id);
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

        var positionBatch = new Dictionary<int, Position>();

        foreach (var (id, component) in state.Get<Velocity>())
        {
            var velocity = component as Velocity;
            var destination = state.Get<Destination>(id);
            var position = state.Get<Position>(id);
            var collision = state.Get<Collision>(id);
            var area = state.Get<Area>(id);

            var physics = state.Get<PhysicsNode>(id)?.Node;
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
                state = state.Without<Destination>(id);
                state = state.Without<Velocity>(id);

                if (collided == null)
                {
                    physics.Position = new Vector2(destination.Position.X, destination.Position.Y);
                }
                else
                {
                    var collideId = Convert.ToInt32((collided.Collider as Node).Name.Split("-").First());

                    var ev = state.Get<CollisionEvent>(id);
                    if (ev != null)
                    {
                        var queue = (id, collideId, ev as Event);
                        state = state.With(Events.ENTITY, new EventQueue()
                        {
                            Events = state.Get<EventQueue>(Events.ENTITY).Events.With(queue)
                        });
                    }

                    var otherEv = state.Get<CollisionEvent>(collideId);
                    if (otherEv != null)
                    {
                        var queue = (collideId, id, otherEv as Event);

                        state = state.With(Events.ENTITY, new EventQueue()
                        {
                            Events = state.Get<EventQueue>(Events.ENTITY).Events.With(queue)
                        });
                    }
                }
            }

            var physicsPosition = physics.Position;
            positionBatch.Add(id, new Position { X = physicsPosition.x, Y = physicsPosition.y });
        }

        state = state.Batch<Position>(positionBatch);
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