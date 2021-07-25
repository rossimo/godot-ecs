using Godot;
using Leopotam.EcsLite;

public struct Sprite
{
    public string Image;
}

public struct SpriteNode
{
    public Godot.Sprite Node;
}

public struct Position
{
    public float X;
    public float Y;
}

public struct LowRenderPriority { }

public struct Rotation
{
    public float Degrees;
}

public struct Scale
{
    public float X;
    public float Y;
}

public struct Color
{
    public float Red;
    public float Green;
    public float Blue;
}

public struct Flash
{
    public Color Color;
}

// public record ClickEvent : Event;

public class Renderer : IEcsRunSystem
{
    private AddEvents<Sprite> _spriteAdds;
    private AddEvents<Scale> _scaleAdds;

    private EcsPool<SpriteNode> spriteNodes;
    private EcsPool<Position> positions;
    private EcsPool<Delete> deletes;

    public Renderer(EcsWorld world)
    {
        _spriteAdds = new AddEvents<Sprite>(world);
        _scaleAdds = new AddEvents<Scale>(world);

        spriteNodes = world.GetPool<SpriteNode>();
        positions = world.GetPool<Position>();
        deletes = world.GetPool<Delete>();
    }

    public void Run(EcsSystems systems)
    {
        var world = systems.GetWorld();
        var game = systems.GetShared<Game>();

        foreach (int entity in world.Filter<Delete>().Inc<SpriteNode>().End())
        {
            ref var sprite = ref spriteNodes.Get(entity);

            var node = sprite.Node;
            game.RemoveChild(node);
            node.QueueFree();
        }

        foreach (int entity in _spriteAdds)
        {
            ref var sprite = ref _spriteAdds.Get(entity);

            var node = new Godot.Sprite()
            {
                Name = $"{entity}",
                Texture = GD.Load<Texture>(sprite.Image)
            };

            game.AddChild(node);

            ref var spriteNode = ref spriteNodes.Add(entity);
            spriteNode.Node = node;
        }

        foreach (int entity in world.Filter<Event<Position, Add>>().Inc<SpriteNode>().End())
        {
            ref var spriteNode = ref spriteNodes.Get(entity);
            ref var position = ref positions.Get(entity);

            spriteNode.Node.Position = new Vector2(position.X, position.Y);
        }

        foreach (int entity in _scaleAdds)
        {
            ref var scale = ref _scaleAdds.Get(entity);

            var node = game.GetNodeOrNull<Godot.Sprite>($"{entity}");
            node.Scale = new Vector2(scale.X, scale.Y);
        }

        /*

        var scales = Diff<Scale>.Compare(previous, state);

        foreach (var (id, sprite) in sprites.Removed)
        {
            var node = sprite.Node;
            if (node == null) continue;

            node.RemoveAndSkip();
            node.QueueFree();
        }

        foreach (var (id, component) in sprites.Updated)
        {
            var position = state.Get<Position>(id);
            position = position ?? new Position { X = 0, Y = 0 };

            var node = game.GetNodeOrNull<ClickableSprite>($"{id}");
            if (node != null) continue;

            node = new ClickableSprite()
            {
                Name = $"{id}",
                Texture = GD.Load<Texture>(component.Image),
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

            if (component.Node != node)
            {
                state = state.With(id, component with
                {
                    Node = node
                });
            }
        }

        foreach (var (id, component) in sprites.Changed)
        {
            var node = game.GetNodeOrNull<ClickableSprite>($"{id}");
            if (node == null) continue;

            node.Texture = GD.Load<Texture>(component.Image);

            if (node != component.Node)
            {
                state = state.With(id, component with
                {
                    Node = node
                });
            }
        }

        foreach (var (id, scale) in scales.Removed)
        {
            var node = state.Get<Sprite>(id)?.Node;
            if (node == null) continue;
            node.Scale = new Vector2(1, 1);
        }

        foreach (var (id, scale) in scales.Updated.Concat(scales.Changed))
        {
            var node = state.Get<Sprite>(id)?.Node;
            if (node == null) continue;
            node.Scale = new Vector2(scale.X, scale.Y);
        }

        foreach (var (id, rotation) in rotations.Removed)
        {
            var node = state.Get<Sprite>(id)?.Node;
            if (node == null) continue;
            node.RotationDegrees = 0;
        }

        foreach (var (id, rotation) in rotations.Updated.Concat(rotations.Changed))
        {
            var node = state.Get<Sprite>(id)?.Node;
            if (node == null) continue;
            node.RotationDegrees = rotation.Degrees;
        }

        foreach (var (id, click) in clicks.Removed)
        {
            var node = state.Get<Sprite>(id)?.Node;
            if (node == null) continue;

            if (node.IsConnected("pressed", game, nameof(game._Event)))
            {
                node.Disconnect("pressed", game, nameof(game._Event));
            }
        }

        foreach (var (id, click) in clicks.Updated.Concat(clicks.Changed))
        {
            var node = state.Get<Sprite>(id)?.Node;
            if (node == null) continue;

            if (node.IsConnected("pressed", game, nameof(game._Event)))
            {
                node.Disconnect("pressed", game, nameof(game._Event));
            }
            node.Connect("pressed", game, nameof(game._Event), new Godot.Collections.Array() {
                id, new GodotWrapper(click)
            });
        }

        foreach (var (id, flash) in flashes.Updated.Concat(flashes.Changed))
        {
            state = state.Without<Flash>(id);

            var position = state.Get<Position>(id);
            position = position ?? new Position { X = 0, Y = 0 };

            var node = state.Get<Sprite>(id)?.Node;
            var tween = game.GetNodeOrNull<Tween>($"{id}/modulate");

            if (node == null) continue;

            tween.RemoveAll();

            if (tween.IsConnected("tween_all_completed", game, nameof(game._Event)))
            {
                tween.Disconnect("tween_all_completed", game, nameof(game._Event));
            }

            tween.InterpolateProperty(node, "modulate",
                new Godot.Color(flash.Color.Red, flash.Color.Green, flash.Color.Blue),
                new Godot.Color(1, 1, 1),
                .33f);

            tween.Start();
        }

        foreach (var (id, position) in positions.Changed)
        {
            var sprite = state.Get<Sprite>(id);
            var lowPriority = state.Get<LowRenderPriority>(id);
            var node = sprite?.Node;
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
        */
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