using Ecs;
using System;
using Godot;
using System.Linq;

public record Scale(float X, float Y) : Component;

public record Sprite(string Image) : Component;

public record Click(Component Event) : Component;

public class Renderer
{
    private static readonly Position NO_POSITION = new Position(0, 0);

    public static Ecs.State System(State state, Game game)
    {
        var unused = game.GetChildren().OfType<Node>().Select(node => node.Name).ToHashSet();

        foreach (var (id, sprite) in state.Get<Sprite>())
        {
            var entity = state[id];
            var node = game.GetNodeOrNull<ClickableSprite>(id);

            if (node == null)
            {
                node = new ClickableSprite() { Name = id };

                node.AddChild(new Area2D()
                {
                    Name = "area"
                });

                node.AddChild(new Tween()
                {
                    Name = "move"
                });

                game.AddChild(node);
            }

            var area = node.GetNode<Area2D>("area");
            var moveTween = node.GetNode<Tween>("move");

            unused.ExceptWith(new[] { id });

            var image = GetMetaSafely(node, "image") as string;
            if (image != sprite.Image)
            {
                node.SetMeta("image", sprite.Image);

                node.Texture = sprite.Image?.Count() > 0
                    ? GD.Load<Texture>(sprite.Image)
                    : null;

                foreach (Node child in area.GetChildren())
                {
                    area.RemoveChild(child);
                    child.QueueFree();
                }

                area.AddChild(new CollisionShape2D()
                {
                    Shape = new RectangleShape2D()
                    {
                        Extents = node.GetRect().Size / 2f
                    }
                });
            }

            var scale = entity.Get<Scale>();
            if (node.Scale.x != scale.X || node.Scale.y != scale.Y)
            {
                node.Scale = new Vector2(scale.X, scale.Y);
            }

            var rotation = entity.Get<Rotation>()?.Degrees ?? 0;
            if (node.RotationDegrees != rotation)
            {
                node.RotationDegrees = rotation;
            }

            var click = entity.Get<Click>();
            var clickMeta = GetMetaSafely(node, "click");
            if (clickMeta != click?.ToString())
            {
                node.SetMeta("click", click?.ToString());

                if (HasConnection(node, "pressed", nameof(game._Event)))
                {
                    node.Disconnect("pressed", game, nameof(game._Event));
                }

                if (click != null)
                {
                    node.Connect("pressed", game, nameof(game._Event), new Godot.Collections.Array() { id, new GodotWrapper(click.Event) });
                }
            }

            var collide = entity.Get<Collide>();
            var collideMeta = GetMetaSafely(area, "collide");
            if (collideMeta != collide?.ToString())
            {
                area.SetMeta("collide", collide?.ToString());

                if (HasConnection(area, "area_entered", nameof(game._Collision)))
                {
                    area.Disconnect("area_entered", game, nameof(game._Collision));
                }

                if (collide != null)
                {
                    area.Connect("area_entered", game, nameof(game._Collision), new Godot.Collections.Array() { id });
                }
            }

            var move = entity.Get<Move>();
            var position = entity.Get<Position>() ?? NO_POSITION;
            if (move != null)
            {
                if (move.Position.X == node.Position.x && move.Position.Y == node.Position.y)
                {
                    state = state.With(id, state[id].Without<Move>());
                }
                else
                {
                    var moveMeta = GetMetaSafely(moveTween, "interpolate");
                    if (moveMeta != move.ToString())
                    {
                        moveTween.SetMeta("interpolate", move.ToString());

                        var start = new Vector2(position.X, position.Y);
                        var end = new Vector2(move.Position.X, move.Position.Y);
                        var distance = start.DistanceTo(end);
                        var duration = distance / move.Speed;
                        moveTween.StopAll();
                        moveTween.InterpolateProperty(node, "position", start, end, duration);
                        moveTween.Start();
                    }
                }
            }
            else
            {
                if (position.X != node.Position.x || position.Y != node.Position.y)
                {
                    node.Position = new Vector2(position.X, position.Y);
                }
            }

            if (position.X != node.Position.x || position.Y != node.Position.y)
            {
                state = state.With(id, new Position(node.Position.x, node.Position.y));
            }
        }

        foreach (var name in unused)
        {
            var child = game.FindNode(name);
            game.RemoveChild(child);
            child.QueueFree();
        }

        return state;
    }

    public static bool HasConnection(Node node, string name, string method)
    {
        foreach (Godot.Collections.Dictionary signal in node.GetSignalList())
        {
            foreach (Godot.Collections.Dictionary connection in node.GetSignalConnectionList(signal["name"] as string))
            {
                if (signal["name"] as string == name && connection["method"] as string == method)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static string GetMetaSafely(Godot.Object node, string name)
    {
        if (node.HasMeta(name))
        {
            return node.GetMeta(name) as string;
        }

        return null;
    }
}

public class GodotWrapper : Godot.Object
{
    private object _value { get; set; }

    public GodotWrapper(object value)
    {
        _value = value;
    }

    public T Get<T>()
    {
        return (T)_value;
    }
}