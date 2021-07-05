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

public record Velocity : Component
{
    public float X;
    public float Y;
}

public record Collision : Component;

public record CollisionEvent : Event
{
    public CollisionEvent(params Task[] tasks)
        => (Tasks) = (tasks);

    public override string ToString() => base.ToString();
}

public record Area : Component;

public record AreaEnterEvent : Event
{
    public AreaEnterEvent(params Task[] tasks)
        => (Tasks) = (tasks);

    public override string ToString() => base.ToString();
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
            var (areas, areaEnterEvents, collisions, moves) = Diff.Compare<Area, AreaEnterEvent, Collision, Move>(previous, state);

            var needPhysics = areas.Added.Select(entry => entry.ID)
                .Concat(areas.Changed.Select(entry => entry.ID))
                .Concat(collisions.Added.Select(entry => entry.ID))
                .Concat(collisions.Changed.Select(entry => entry.ID))
                .Distinct();

            var notNeedPhysics = areas.Removed.Select(entry => entry.ID)
                .Concat(collisions.Removed.Select(entry => entry.ID))
                .Distinct()
                .Where(id => !needPhysics.Contains(id));

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

            foreach (var id in notNeedPhysics)
            {
                var physics = game.GetNodeOrNull<Node2D>($"{id}-physics");
                if (physics == null) continue;

                game.RemoveChild(physics);
                physics.QueueFree();
            }

            foreach (var id in needPhysics)
            {
                var physics = game.GetNodeOrNull<KinematicBody2D>($"{id}-physics");
                if (physics != null) continue;

                var position = state[id].Get<Position>() ?? new Position { X = 0, Y = 0 };

                game.AddChild(new KinematicBody2D()
                {
                    Name = $"{id}-physics",
                    Position = new Vector2(position.X, position.Y)
                });
            }

            foreach (var (id, enter) in areas.Removed)
            {
                var physics = game.GetNodeOrNull<Node2D>($"{id}-physics");
                var area = physics?.GetNodeOrNull<Area2D>("area");
                if (area == null) continue;

                physics.RemoveChild(area);
                area.QueueFree();
            }

            foreach (var (id, component) in areas.Added.Concat(areas.Changed))
            {
                var node = game.GetNodeOrNull<KinematicBody2D>($"{id}-physics");
                if (node == null) continue;

                var (sprite, scale) = state[id].Get<Sprite, Scale>();
                scale = scale ?? new Scale { X = 1, Y = 1 };

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
            }

            foreach (var (id, ev) in areaEnterEvents.Removed)
            {
                var node = game.GetNodeOrNull<Node2D>($"{id}-physics/area");
                if (node == null) continue;

                if (node.IsConnected("area_entered", game, nameof(game._Event)))
                {
                    node.Disconnect("area_entered", game, nameof(game._Event));
                }
            }

            foreach (var (id, ev) in areaEnterEvents.Added.Concat(areaEnterEvents.Changed))
            {
                var node = game.GetNodeOrNull<Node2D>($"{id}-physics/area");
                if (node == null) continue;

                if (node.IsConnected("area_entered", game, nameof(game._Event)))
                {
                    node.Disconnect("area_entered", game, nameof(game._Event));
                }

                node.Connect("area_entered", game, nameof(game._Event), new Godot.Collections.Array() {
                    id, new GodotWrapper(ev)
                });
            }

            foreach (var (id, component) in collisions.Removed)
            {
                var node = game.GetNodeOrNull<KinematicBody2D>($"{id}-physics");
                var collision = node?.GetNodeOrNull<Node2D>("collision");

                if (collision != null)
                {
                    node.RemoveChild(collision);
                    collision.QueueFree();
                }
            }

            foreach (var (id, component) in collisions.Changed.Concat(collisions.Added))
            {
                var entity = state[id];
                var node = game.GetNodeOrNull<KinematicBody2D>($"{id}-physics");
                if (node == null) continue;

                var (sprite, scale) = entity.Get<Sprite, Scale>();
                scale = scale ?? new Scale { X = 1, Y = 1 };

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
                    var collideId = (collided.Collider as Node).Name.Split("-").First();

                    var ev = state[id].Get<CollisionEvent>();
                    if (ev != null)
                    {
                        var queue = (id, collideId, ev as Event);
                        state = state.With(Events.ENTITY, entity => entity.With(new EventQueue()
                        {
                            Events = entity.Get<EventQueue>().Events.With(queue)
                        }));
                    }

                    var otherEv = state.ContainsKey(collideId)
                        ? state[collideId].Get<CollisionEvent>()
                        : null;
                    if (otherEv != null)
                    {
                        var queue = (collideId, id, otherEv as Event);

                        state = state.With(Events.ENTITY, entity => entity.With(new EventQueue()
                        {
                            Events = entity.Get<EventQueue>().Events.With(queue)
                        }));
                    }
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