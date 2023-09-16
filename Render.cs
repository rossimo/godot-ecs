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

public class Renderer
{
    public static Action System(World world)
    {
        var systems = new List<Action>() {
            Flash(world),
            CleanupFlash(world),
            SyncRender(world)
        };

        return () => systems.ForEach(system => system());
    }

    public static Action Flash(World world)
    {
        return world.System("Flash", (ref RenderNode render, ref Flash flash) =>
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
        return world.System("CleanupFlash", (Entity entity, ref Flash flash) =>
        {
            entity.Remove<Flash>();
            entity.Cleanup();
        });
    }

    public static Action SyncRender(World world) =>
    world.System("SyncRender", (Entity entity, ref Position position, ref RenderNode render) =>
    {
        var time = world.Get<Time>();

        if (position.X != render.Node.Position.X || position.Y != render.Node.Position.Y)
        {
            render.PositionTween?.Stop();

            if (entity.Has<Speed>())
            {
                var target = new Vector2(position.X, position.Y).DistanceTo(render.Node.Position);
                var normal = entity.Get<Speed>().Value * Physics.PHYSICS_SPEED_SCALE;

                var ratio = normal > target ? target / normal : normal / target;

                render.PositionTween = render.Node.CreateTween();
                render.PositionTween.TweenProperty(render.Node, "position", new Vector2(position.X, position.Y), Physics.PHYSICS_TARGET_FRAMETIME * ratio);
            }
            else
            {
                render.Node.Position = new Vector2(position.X, position.Y);
            }
        }
    });
}