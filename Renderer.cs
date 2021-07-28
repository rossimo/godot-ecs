using Godot;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

public struct Sprite
{
    public string Image;
}

public struct SpriteNode
{
    public Godot.Sprite Node;
    public Godot.Tween PositionTween;
    public Godot.Tween ModulateTween;
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
    [EcsWorld] readonly EcsWorld world = default;
    [EcsShared] readonly Game game = default;
    [EcsPool] readonly EcsPool<Sprite> sprites = default;
    [EcsPool] readonly EcsPool<Scale> scales = default;
    [EcsPool] readonly EcsPool<SpriteNode> spriteNodes = default;
    [EcsPool] readonly EcsPool<Position> positions = default;
    [EcsPool] readonly EcsPool<Delta> deltas = default;
    [EcsPool] readonly EcsPool<LowRenderPriority> lowPriority = default;
    [EcsPool] readonly EcsPool<Flash> flashes = default;

    public void Run(EcsSystems systems)
    {
        float delta = 0;
        foreach (var entity in world.Filter<Delta>().End())
        {
            delta = deltas.Get(entity).Value;
        }

        foreach (int entity in world.Filter<Notify<Sprite>>().End())
        {
            ref var sprite = ref sprites.Get(entity);

            var node = new Godot.Sprite()
            {
                Name = $"{entity}",
                Texture = GD.Load<Texture>(sprite.Image)
            };

            var positionTween = new Tween()
            {
                Name = "position"
            };
            node.AddChild(positionTween);

            var modulateTween = new Tween()
            {
                Name = "modulate"
            };
            node.AddChild(modulateTween);

            game.AddChild(node);

            ref var spriteNode = ref spriteNodes.Add(entity);
            spriteNode.Node = node;
            spriteNode.PositionTween = positionTween;
            spriteNode.ModulateTween = modulateTween;

            if (positions.Has(entity))
            {
                ref var position = ref positions.Get(entity);
                node.Position = new Vector2(position.X, position.Y);
            }
        }

        foreach (int entity in world.Filter<SpriteNode>().Inc<Notify<Position>>().End())
        {
            ref var spriteNode = ref spriteNodes.Get(entity);
            ref var position = ref positions.Get(entity);

            var node = spriteNode.Node;


            if (lowPriority.Has(entity))
            {
                node.Position = new Vector2(position.X, position.Y);
            }
            else
            {
                var tween = spriteNode.PositionTween;

                tween.InterpolateProperty(node, "position",
                    node.Position,
                    new Vector2(position.X, position.Y),
                    delta);

                tween.Start();
            }
        }

        foreach (int entity in world.Filter<SpriteNode>().Inc<Notify<Scale>>().End())
        {
            ref var scale = ref scales.Get(entity);

            var node = game.GetNodeOrNull<Godot.Sprite>($"{entity}");
            node.Scale = new Vector2(scale.X, scale.Y);
        }

        foreach (var entity in world.Filter<SpriteNode>().Inc<Notify<Flash>>().End())
        {
            ref var spriteNode = ref spriteNodes.Get(entity);

            var flash = flashes.Get(entity);
            flashes.Del(entity);

            var node = spriteNode.Node;
            var tween = spriteNode.ModulateTween;

            tween.InterpolateProperty(node, "modulate",
                new Godot.Color(flash.Color.Red, flash.Color.Green, flash.Color.Blue),
                new Godot.Color(1, 1, 1),
                .33f);

            tween.Start();
        }

        foreach (int entity in world.Filter<SpriteNode>().Inc<Delete>().End())
        {
            ref var sprite = ref spriteNodes.Get(entity);

            var node = sprite.Node;
            game.RemoveChild(node);
            node.QueueFree();
        }

        /*=
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
        */
    }
}

public struct Delta
{
    public float Value;
}

public class DeltaSystem : IEcsInitSystem
{
    private EcsPool<Delta> _pool;
    private EcsFilter _filter;

    public void Init(EcsSystems systems)
    {
        var world = systems.GetWorld();

        _pool = world.GetPool<Delta>();
        _filter = world.Filter<Delta>().End();

        _pool.Add(world.NewEntity());
    }

    public void Run(EcsSystems systems, float delta)
    {
        var world = systems.GetWorld();

        foreach (var entity in world.Filter<Delta>().End())
        {
            ref var component = ref _pool.Get(entity);
            component.Value = delta;
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