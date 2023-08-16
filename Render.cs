using Godot;
using Arch.Core;
using Arch.System;
using Arch.Core.Extensions;

public struct Sprite
{
    public string Image;
}

public struct RenderNode
{
    public Node2D Node;
    public Tween Modulate;
    public Tween PositionTween;
}

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

public class Notify<C>
{
    public Entity entity;
}

public class RendererSystem : BaseSystem<World, Game>
{
    private QueryDescription renderFlashes = new QueryDescription().WithAll<RenderNode, Flash>();
    private QueryDescription flashes = new QueryDescription().WithAll<Flash>();

    public RendererSystem(World world) : base(world) { }

    public override void Update(in Game data)
    {
        World.Query(renderFlashes, (ref RenderNode render, ref Flash flash) =>
       {
           var node = render.Node;
           node.Modulate = new Godot.Color(flash.Color.Red, flash.Color.Green, flash.Color.Blue);

           render.Modulate?.Stop();
           render.Modulate = node.CreateTween();
           render.Modulate.TweenProperty(node, "modulate", new Godot.Color(1, 1, 1), .33f);
       });

        World.Query(flashes, (in Entity entity) =>
        {
            entity.Remove<Flash>();
            World.Cleanup(entity);
        });
    }
}