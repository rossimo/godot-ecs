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
        double delta = 0;
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
            node.Modulate = new Godot.Color(flash.Color.Red, flash.Color.Green, flash.Color.Blue);

            modulate.Tween = node.CreateTween();
            modulate.Tween.TweenProperty(node, "modulate",
               new Godot.Color(1, 1, 1),
               .33f);
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
    public double Value;
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

    public void Run(EcsSystems systems, double delta)
    {
        ref var component = ref pool.Get(shared.FrameTime);
        component.Value = delta;
    }
}