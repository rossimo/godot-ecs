using Godot;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

public struct Sprite
{
    public string Image;
}

public struct Node2DComponent
{
    public Godot.Node2D Node;
    public Godot.Tween PositionTween;
    public Godot.Tween ModulateTween;
}

[EditorComponent]
public struct Position
{
    public float X;
    public float Y;
}

[EditorComponent]
public struct LowRenderPriority { }

[EditorComponent]
public struct Rotation
{
    public float Degrees;
}

[EditorComponent]
public struct Scale
{
    public float X;
    public float Y;
}

[EditorComponent]
public struct Color
{
    public float Red;
    public float Green;
    public float Blue;
}

[EditorComponent]
public struct Flash
{
    public Color Color;
}

// public record ClickEvent : Event;

public class RendererSystem : IEcsRunSystem
{
    [EcsWorld] readonly EcsWorld world = default;
    [EcsShared] readonly Shared shared = default;
    [EcsPool] readonly EcsPool<Scale> scales = default;
    [EcsPool] readonly EcsPool<Sprite> sprites = default;
    [EcsPool] readonly EcsPool<Node2DComponent> node2DComponents = default;
    [EcsPool] readonly EcsPool<Position> positions = default;
    [EcsPool] readonly EcsPool<FrameTime> deltas = default;
    [EcsPool] readonly EcsPool<LowRenderPriority> lowPriority = default;
    [EcsPool] readonly EcsPool<Flash> flashes = default;

    public void Run(EcsSystems systems)
    {
        var game = shared.Game;
        float delta = 0;
        foreach (var entity in world.Filter<FrameTime>().End())
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

            ref var spriteNode = ref node2DComponents.Add(entity);
            spriteNode.Node = node;
            spriteNode.PositionTween = positionTween;
            spriteNode.ModulateTween = modulateTween;

            if (positions.Has(entity))
            {
                ref var position = ref positions.Get(entity);
                node.Position = new Vector2(position.X, position.Y);
            }
        }

        foreach (int entity in world.Filter<Node2DComponent>().Inc<Notify<Position>>().End())
        {
            ref var spriteNode = ref node2DComponents.Get(entity);
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

        foreach (int entity in world.Filter<Node2DComponent>().Inc<Notify<Scale>>().End())
        {
            ref var scale = ref scales.Get(entity);

            var node = game.GetNodeOrNull<Godot.Sprite>($"{entity}");
            node.Scale = new Vector2(scale.X, scale.Y);
        }

        foreach (var entity in world.Filter<Node2DComponent>().Inc<Notify<Flash>>().End())
        {
            ref var spriteNode = ref node2DComponents.Get(entity);

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

        foreach (int entity in world.Filter<Node2DComponent>().Inc<DeleteEntity>().End())
        {
            ref var sprite = ref node2DComponents.Get(entity);

            var node = sprite.Node;
            game.RemoveChild(node);
            node.QueueFree();
        }
    }
}

public struct FrameTime
{
    public float Value;
}

public class FrameTimeSystem : IEcsInitSystem
{
    private EcsPool<FrameTime> _pool;
    private EcsFilter _filter;

    public void Init(EcsSystems systems)
    {
        var world = systems.GetWorld();

        _pool = world.GetPool<FrameTime>();
        _filter = world.Filter<FrameTime>().End();

        _pool.Add(world.NewEntity());
    }

    public void Run(EcsSystems systems, float delta)
    {
        var world = systems.GetWorld();

        foreach (var entity in world.Filter<FrameTime>().End())
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