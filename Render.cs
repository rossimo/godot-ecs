using Godot;
using Flecs.NET.Core;
using System.Xml.Schema;

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

public class Renderer
{
    public static (IEnumerable<Routine>, IEnumerable<Observer>) Routines(World world) =>
        (new[] {
            SyncRender(world)
        },
        new[] {
            Flash(world)
        });

    public static Observer Flash(World world) =>
        world.Observer(
            name: "Flash",
            callback: (Entity entity, ref Flash flash, ref RenderNode render) =>
            {
                var node = render.Node;
                node.Modulate = new Godot.Color(flash.Color.Red, flash.Color.Green, flash.Color.Blue);

                render.Modulate?.Kill();
                render.Modulate = node.CreateTween();
                render.Modulate.TweenProperty(node, "modulate", new Godot.Color(1, 1, 1), .33f);
            });

    public static Routine SyncRender(World world) =>
        world.Routine(
            name: "SyncRender",
            callback: (Entity entity, ref Position position, ref RenderNode render) =>
            {
                var node = render.Node;
                var physics = new Vector2(position.X, position.Y);

                if (!physics.Equals(node.Position))
                {
                    render.PositionTween?.Kill();

                    var target = physics.DistanceTo(node.Position);
                    var speed = entity.Has<Speed>() ? entity.Get<Speed>().Value : 1;
                    var normal = speed * Physics.PHYSICS_SPEED_SCALE;

                    var ratio = Math.Min(target / normal, 1);

                    render.PositionTween = node.CreateTween();
                    render.PositionTween.TweenProperty(node, "position", physics, Physics.PHYSICS_TARGET_FRAMETIME * ratio);
                }
            });
}