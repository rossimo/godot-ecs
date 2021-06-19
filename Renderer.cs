using Ecs;
using Godot;
using System.Linq;

public record Sprite : Component
{
    public string Image;
}

public record Position : Component
{
    public float X;
    public float Y;
}

public record Rotation : Component
{
    public float Degrees;
}

public record Scale : Component
{
    public float X;
    public float Y;
}

public record Color : Component
{
    public float Red;
    public float Green;
    public float Blue;
}

public record TickComponent : Component
{
    public int Tick;
}

public record Flash : TickComponent
{
    public Color Color;
}

public record ClickEvent : Event;

public class Renderer
{
    public static void System(State previous, State state, Game game)
    {
        if (previous == state) return;

        var (sprites, scales, rotations, clicks, positions, flashes) =
            Diff.Compare<Sprite, Scale, Rotation, ClickEvent, Position, Flash>(previous, state);

        foreach (var (id, sprite) in sprites.Removed)
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            if (node == null) continue;

            game.RemoveChild(node);
            node.QueueFree();
        }

        foreach (var (id, component) in sprites.Added)
        {
            var node = game.GetNodeOrNull<ClickableSprite>(id);
            if (node != null) continue;

            var entity = state[id];
            var position = entity.Get<Position>();

            node = new ClickableSprite()
            {
                Name = id,
                Texture = GD.Load<Texture>(component.Image),
                Position = position == null ? new Vector2(0, 0) : new Vector2(position.X, position.Y)
            };
            game.AddChild(node);

            var path = new Tween()
            {
                Name = "path"
            };
            node.AddChild(path);

            var modulate = new Tween()
            {
                Name = "modulate"
            };
            node.AddChild(modulate);
        }

        foreach (var (id, component) in sprites.Changed)
        {
            var node = game.GetNodeOrNull<ClickableSprite>(id);
            if (node == null) continue;

            node.Texture = GD.Load<Texture>(component.Image);
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

            if (node.IsConnected("pressed", game, nameof(game._Event)))
            {
                node.Disconnect("pressed", game, nameof(game._Event));
            }
        }

        foreach (var (id, click) in clicks.Added.Concat(clicks.Changed))
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            if (node == null) continue;

            if (node.IsConnected("pressed", game, nameof(game._Event)))
            {
                node.Disconnect("pressed", game, nameof(game._Event));
            }
            node.Connect("pressed", game, nameof(game._Event), new Godot.Collections.Array() {
                id, new GodotWrapper(click)
            });
        }

        foreach (var (id, flash) in flashes.Removed)
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            var tween = game.GetNodeOrNull<Tween>($"{id}/modulate");
            if (node == null || tween == null) continue;

            tween.RemoveAll();
        }

        foreach (var (id, flash) in flashes.Added.Concat(flashes.Changed))
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            var tween = game.GetNodeOrNull<Tween>($"{id}/modulate");

            if (node == null) continue;

            tween.RemoveAll();

            if (tween.IsConnected("tween_all_completed", game, nameof(game._Event)))
            {
                tween.Disconnect("tween_all_completed", game, nameof(game._Event));
            }

            var entity = state[id];
            var position = entity?.Get<Position>() ?? new Position { X = 0, Y = 0 };

            tween.InterpolateProperty(node, "modulate",
                new Godot.Color(flash.Color.Red, flash.Color.Green, flash.Color.Blue),
                new Godot.Color(1, 1, 1),
                .33f);

            tween.Start();

            tween.Connect("tween_all_completed", game, nameof(game._Event), new Godot.Collections.Array() {
                id, new GodotWrapper(new Event(new Remove(typeof(Flash))))
            });
        }
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