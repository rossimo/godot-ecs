using Godot;
using Flecs.NET.Core;

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

public class Notify<C>
{
    public Entity entity;
}

public class RendererSystem
{
    public static Action StartFlash(World world)
    {
        return world.System((ref RenderNode render, ref Flash flash) =>
       {
           var node = render.Node;
           node.Modulate = new Godot.Color(flash.Color.Red, flash.Color.Green, flash.Color.Blue);

           render.Modulate?.Stop();
           render.Modulate = node.CreateTween();
           render.Modulate.TweenProperty(node, "modulate", new Godot.Color(1, 1, 1), .33f);
       });
    }

    public static Action CleanupFlash(World world)
    {
        return world.System((Entity entity, ref Flash flash) =>
        {
            entity.Remove<Flash>();
            entity.Cleanup();
        });
    }
}