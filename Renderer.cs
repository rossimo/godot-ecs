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
}

public struct ModulateTween
{
    public Tween Tween;
}


[Editor]
public struct LowRenderPriority { }

[Editor]
public struct Rotation
{
    public float Degrees;
}

[Editor]
public struct Color
{
    public float Red;
    public float Green;
    public float Blue;
}

[Editor]
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
    [EcsPool] readonly EcsPool<Flash> flashes = default;
    [EcsPool] readonly EcsPool<ModulateTween> modulates = default;

    public void Run(EcsSystems systems)
    {
        var game = shared.Game;
        float delta = 0;
        foreach (var entity in world.Filter<FrameTime>().End())
        {
            delta = deltas.Get(entity).Value;
        }

        foreach (var entity in world.Filter<RenderNode>().Inc<ModulateTween>().Inc<Notify<Flash>>().End())
        {
            ref var render = ref renders.Get(entity);
            ref var modulate = ref modulates.Get(entity);
            var flash = flashes.Get(entity);
            
            flashes.Del(entity);

            var node = render.Node;
            var tween = modulate.Tween;

            tween.InterpolateProperty(node, "modulate",
                new Godot.Color(flash.Color.Red, flash.Color.Green, flash.Color.Blue),
                new Godot.Color(1, 1, 1),
                .33f);

            tween.Start();
        }

        foreach (int entity in world.Filter<RenderNode>().Inc<Delete>().End())
        {
            ref var sprite = ref renders.Get(entity);

            var node = sprite.Node;
            node.GetParent()?.RemoveChild(node);
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
    [EcsShared] readonly Shared shared = default;
    [EcsPool] readonly EcsPool<FrameTime> pool = default;

    public void Init(EcsSystems systems)
    {
        var shared = systems.GetShared<Shared>();
        var world = systems.GetWorld();

        shared.FrameTime = world.NewEntity();
        world.GetPool<FrameTime>().Add(shared.FrameTime);
    }

    public void Run(EcsSystems systems, float delta)
    {
        ref var component = ref pool.Get(shared.FrameTime);
        component.Value = delta;
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