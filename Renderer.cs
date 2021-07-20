using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
public record Sprite
{
    public string Image;
}

public record LowRenderPriority
{
}

public record Position
{
    public float X;
    public float Y;
}

public record Rotation
{
    public float Degrees;
}

public record Scale
{
    public float X;
    public float Y;
}

public record Color
{
    public float Red;
    public float Green;
    public float Blue;
}

public record Flash
{
    public Color Color;
}

public record ClickEvent;

public class Diff<T>
{
    private DefaultEcs.EntitySet added;
    private DefaultEcs.EntitySet changed;
    private DefaultEcs.EntitySet removed;

    private List<String> disposed = new List<string>();

    public Diff(DefaultEcs.World world)
    {
        added = world.GetEntities().WhenAdded<T>().AsSet();
        changed = world.GetEntities().WhenChanged<T>().AsSet();
        removed = world.GetEntities().WhenRemoved<T>().AsSet();

        world.SubscribeEntityDisposed(EntityDisabledHandler);
    }

    public void EntityDisabledHandler(in DefaultEcs.Entity entity)
    {
        disposed.Add(entity.ID());
    }

    public ReadOnlySpan<DefaultEcs.Entity> Added()
    {
        return added.GetEntities();
    }

    public ReadOnlySpan<DefaultEcs.Entity> Changed()
    {
        return changed.GetEntities();
    }

    public ReadOnlySpan<DefaultEcs.Entity> RemovedComponent()
    {
        return removed.GetEntities();
    }

    public IEnumerable<String> Removed()
    {
        return removed.GetEntities().ToArray().Select(entity => entity.ID()).Concat(disposed).Distinct();
    }

    public void Complete()
    {
        added.Complete();
        changed.Complete();
        removed.Complete();
        disposed.Clear();
    }
}

public class Renderer
{
    private Diff<Sprite> sprites;
    private Diff<Scale> scales;
    private Diff<Rotation> rotations;
    private Diff<ClickEvent> clicks;
    private Diff<Position> positions;
    private Diff<Flash> flashes;

    public Renderer(DefaultEcs.World world)
    {
        sprites = new Diff<Sprite>(world);
        scales = new Diff<Scale>(world);
        rotations = new Diff<Rotation>(world);
        clicks = new Diff<ClickEvent>(world);
        positions = new Diff<Position>(world);
        flashes = new Diff<Flash>(world);
    }

    public void System(Game game, float delta)
    {
        foreach (var entityId in sprites.Removed())
        {
            var node = game.GetNodeOrNull(entityId);
            if (node == null) continue;

            node.RemoveAndSkip();
            node.QueueFree();
        }

        foreach (var entity in sprites.Added())
        {
            var sprite = entity.TryGet<Sprite>();
            var position = entity.TryGet<Position>();
            position = position ?? new Position { X = 0, Y = 0 };

            var node = game.GetNodeOrNull(entity.ID());
            if (node != null) continue;

            node = new Godot.Sprite()
            {
                Name = entity.ID(),
                Texture = GD.Load<Texture>(sprite.Image),
                Position = new Vector2(position.X, position.Y)
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

        foreach (var entity in sprites.Changed())
        {
            var id = entity.ID();
            var component = entity.TryGet<Sprite>();

            var node = game.GetNodeOrNull<Godot.Sprite>(id);
            if (node == null) continue;

            node.Texture = GD.Load<Texture>(component.Image);
        }

        foreach (var id in scales.Removed())
        {
            var node = game.GetNodeOrNull<Godot.Sprite>(id);
            if (node == null) continue;
            node.Scale = new Vector2(1, 1);
        }

        foreach (var entity in scales.Added().ToArray().Concat(scales.Changed().ToArray()))
        {
            var id = entity.ID();
            var scale = entity.TryGet<Scale>();

            var node = game.GetNodeOrNull<Godot.Sprite>(id);
            if (node == null) continue;

            node.Scale = new Vector2(scale.X, scale.Y);
        }

        foreach (var id in rotations.Removed())
        {
            var node = game.GetNodeOrNull<Godot.Sprite>(id);
            if (node == null) continue;

            node.RotationDegrees = 0;
        }

        foreach (var entity in rotations.Added().ToArray().Concat(rotations.Changed().ToArray()))
        {
            var id = entity.ID();
            var rotation = entity.TryGet<Rotation>();

            var node = game.GetNodeOrNull<Godot.Sprite>(id);
            if (node == null) continue;

            node.RotationDegrees = rotation.Degrees;
        }

        foreach (var entity in flashes.Added().ToArray().Concat(flashes.Changed().ToArray()))
        {
            var flash = entity.TryGet<Flash>();
            var position = entity.TryGet<Position>();
            position = position ?? new Position { X = 0, Y = 0 };

            entity.Remove<Flash>();

            var node = game.GetNodeOrNull(entity.ID());
            var tween = node.GetNodeOrNull<Tween>("modulate");

            if (node == null) continue;

            tween.RemoveAll();

            // if (tween.IsConnected("tween_all_completed", game, nameof(game._Event)))
            // {
            //    tween.Disconnect("tween_all_completed", game, nameof(game._Event));
            // }

            tween.InterpolateProperty(node, "modulate",
                new Godot.Color(flash.Color.Red, flash.Color.Green, flash.Color.Blue),
                new Godot.Color(1, 1, 1),
                .33f);

            tween.Start();
        }

        foreach (var entity in positions.Changed())
        {
            var id = entity.ID();
            var position = entity.TryGet<Position>();

            var sprite = entity.TryGet<Sprite>();
            var lowPriority = entity.TryGet<LowRenderPriority>();

            var node = game.GetNodeOrNull<Godot.Sprite>(id);
            if (node == null) continue;

            if (position.X != node.Position.x || position.Y != node.Position.y)
            {
                if (lowPriority == null)
                {
                    var tween = node.GetNodeOrNull<Tween>("move");
                    if (tween == null)
                    {
                        tween = new Tween()
                        {
                            Name = "move"
                        };
                        node.AddChild(tween);
                    }

                    tween.InterpolateProperty(node, "position",
                        node.Position,
                        new Vector2(position.X, position.Y),
                        delta);

                    tween.Start();
                }
                else
                {
                    node.Position = new Vector2(position.X, position.Y);
                }
            }
        }

        sprites.Complete();
        scales.Complete();
        rotations.Complete();
        clicks.Complete();
        positions.Complete();
        flashes.Complete();
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