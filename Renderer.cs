using Godot;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

public struct Sprite
{
    public string Image;
}

public struct RenderNode
{
    public Godot.Node2D Node;
    public Godot.Tween ModulateTween;
}

[EditorComponent]
public struct LowRenderPriority { }

[EditorComponent]
public struct Rotation
{
    public float Degrees;
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
    [EcsPool] readonly EcsPool<RenderNode> renders = default;
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

        foreach (var entity in world.Filter<RenderNode>().Inc<Notify<Flash>>().End())
        {
            ref var spriteNode = ref renders.Get(entity);

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

        foreach (int entity in world.Filter<RenderNode>().Inc<DeleteEntity>().End())
        {
            ref var sprite = ref renders.Get(entity);

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