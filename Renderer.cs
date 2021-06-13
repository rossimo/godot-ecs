using Ecs;
using Godot;
using System.Linq;

public record Sprite(string Image) : Component;

public record Position(float X, float Y, bool Self = false) : Component;

public record Rotation(float Degrees) : Component;

public record Scale(float X, float Y) : Component;

public class Renderer
{
    public static void System(State previous, State state, Game game)
    {
        if (previous == state) return;

        var (sprites, scales, rotations, clicks, collides, positions) =
            Diff.Compare<Sprite, Scale, Rotation, Click, Collide, Position>(previous, state);

        foreach (var (id, sprite) in sprites.Removed)
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            if (node == null) continue;

            game.RemoveChild(node);
            node.QueueFree();
        }

        foreach (var (id, component) in sprites.Added)
        {
            var node = game.GetNodeOrNull<ClickableKinematicBody2D>(id);
            if (node != null) continue;

            node = new ClickableKinematicBody2D()
            {
                Name = id
            };
            game.AddChild(node);

            var sprite = new Godot.Sprite()
            {
                Name = "sprite",
                Texture = GD.Load<Texture>(component.Image)
            };
            node.AddChild(sprite);
            node.Rect = sprite.GetRect();

            var area = new Area2D()
            {
                Name = "area"
            };
            node.AddChild(area);

            area.AddChild(new CollisionShape2D()
            {
                Shape = new RectangleShape2D()
                {
                    Extents = sprite.GetRect().Size / 2f
                }
            });
        }

        foreach (var (id, component) in sprites.Changed)
        {
            var node = game.GetNodeOrNull<ClickableKinematicBody2D>(id);
            var sprite = node?.GetNode<Godot.Sprite>("sprite");
            if (node == null || sprite == null) continue;

            sprite.Texture = GD.Load<Texture>(component.Image);
            node.Rect = sprite.GetRect();

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
                    Extents = sprite.GetRect().Size / 2f
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