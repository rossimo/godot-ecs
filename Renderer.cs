using Ecs;
using Godot;
using System.Linq;

public record Sprite(string Image) : Component;

public record Position(float X, float Y, bool Self = false) : Component;

public record Rotation(float Degrees) : Component;

public record Scale(float X, float Y) : Component;

public record Move(Position Position, float Speed) : Component;

public class Renderer
{
    public static void System(State previous, State state, Game game)
    {
        if (previous == state) return;

        var (sprites, scales, rotations, clicks, collides, positions, moves) =
            Diff.Compare<Sprite, Scale, Rotation, Click, Collide, Position, Move>(previous, state);

        foreach (var (id, sprite) in sprites.Removed)
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            if (node == null) continue;

            game.RemoveChild(node);
            node.QueueFree();
        }

        foreach (var (id, sprite) in sprites.Added)
        {
            var node = game.GetNodeOrNull<ClickableSprite>(id);
            if (node != null) continue;

            node = new ClickableSprite()
            {
                Name = id,
                Texture = GD.Load<Texture>(sprite.Image)
            };

            node.AddChild(new Tween()
            {
                Name = "move"
            });

            var area = new Area2D()
            {
                Name = "area"
            };
            node.AddChild(area);

            area.AddChild(new CollisionShape2D()
            {
                Shape = new RectangleShape2D()
                {
                    Extents = node.GetRect().Size / 2f
                }
            });

            game.AddChild(node);
        }

        foreach (var (id, sprite) in sprites.Changed)
        {
            var node = game.GetNodeOrNull<ClickableSprite>(id);
            if (node == null) continue;

            node.Texture = GD.Load<Texture>(sprite.Image);

            var area = node.GetNode("area");

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

        foreach (var (id, scale) in scales.Removed)
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            if (node == null) continue;
            node.Scale = new Vector2(1, 1);
        }

        foreach (var (id, scale) in scales.Added.Concat(scales.Changed))
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            if (node == null) continue;
            node.Scale = new Vector2(scale.X, scale.Y);
        }

        foreach (var (id, rotation) in rotations.Removed)
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            if (node == null) continue;
            node.RotationDegrees = 0;
        }

        foreach (var (id, rotation) in rotations.Added.Concat(rotations.Changed))
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            if (node == null) continue;
            node.RotationDegrees = rotation.Degrees;
        }

        foreach (var (id, click) in clicks.Removed)
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            if (node == null) continue;

            if (HasConnection(node, "pressed", nameof(game._Event)))
            {
                node.Disconnect("pressed", game, nameof(game._Event));
            }
        }

        foreach (var (id, click) in clicks.Added)
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            if (node == null) continue;

            if (!HasConnection(node, "pressed", nameof(game._Event)))
            {
                node.Connect("pressed", game, nameof(game._Event), new Godot.Collections.Array() { id, new GodotWrapper(click) });
            }
        }

        foreach (var (id, click) in clicks.Changed)
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            if (node == null) continue;

            if (HasConnection(node, "pressed", nameof(game._Event)))
            {
                node.Disconnect("pressed", game, nameof(game._Event));
            }

            node.Connect("pressed", game, nameof(game._Event), new Godot.Collections.Array() { id, new GodotWrapper(click) });
        }

        foreach (var (id, collide) in collides.Removed)
        {
            var node = game.GetNodeOrNull<Node2D>($"{id}/area");
            if (node == null) continue;

            if (HasConnection(node, "area_entered", nameof(game._Event)))
            {
                node.Disconnect("area_entered", game, nameof(game._Event));
            }
        }

        foreach (var (id, collide) in collides.Added)
        {
            var node = game.GetNodeOrNull<Node2D>($"{id}/area");
            if (node == null) continue;

            if (!HasConnection(node, "area_entered", nameof(game._Event)))
            {
                node.Connect("area_entered", game, nameof(game._Event), new Godot.Collections.Array() { id, new GodotWrapper(collide) });
            }
        }

        foreach (var (id, collide) in collides.Changed)
        {
            var node = game.GetNodeOrNull<Node2D>($"{id}/area");
            if (node == null) continue;

            if (HasConnection(node, "area_entered", nameof(game._Event)))
            {
                node.Disconnect("area_entered", game, nameof(game._Event));
            }

            node.Connect("area_entered", game, nameof(game._Event), new Godot.Collections.Array() { id, new GodotWrapper(collide) });
        }

        foreach (var (id, move) in moves.Removed)
        {
            var node = game.GetNodeOrNull<Tween>($"{id}/move");
            if (node == null) continue;

            node.StopAll();
        }

        foreach (var (id, move) in moves.Added.Concat(moves.Changed))
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            var tween = game.GetNodeOrNull<Tween>($"{id}/move");

            if (node == null || tween == null) continue;

            tween.StopAll();

            var entity = state[id];
            var position = entity?.Get<Position>() ?? new Position(0, 0);

            var start = new Vector2(node.Position.x, node.Position.y);
            var end = new Vector2(move.Position.X, move.Position.Y);
            var distance = start.DistanceTo(end);
            var duration = distance / move.Speed;
            tween.StopAll();
            tween.InterpolateProperty(node, "position", start, end, duration);
            tween.Start();
        }

        foreach (var (id, position) in positions.Added.Concat(positions.Changed))
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            if (node == null) continue;

            if (!position.Self)
            {
                node.Position = new Vector2(position.X, position.Y);
            }
        }
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