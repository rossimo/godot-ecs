using Godot;
using RelEcs;

public class Sprite
{
    public string Image;
}

public class RenderNode
{
    public Node2D Node;
}

public class ModulateTween
{
    public Tween Tween;
}


[Editor]
public class LowRenderPriority { }

[Editor]
public class Rotation
{
    public float Degrees;
}

[Editor]
public class Color
{
    public float Red;
    public float Green;
    public float Blue;
}

[Editor]
public class Flash
{
    public Color Color = new();
}

// public record ClickEvent : Event;

public class Notify<C>
{
    public Entity entity;
}

public class RendererSystem : ISystem
{
    public void Run(World world)
    {
        foreach (var (render, modulate, flash) in world.Query<RenderNode, ModulateTween, Flash>().Build())
        {
            var node = render.Node;
            node.Modulate = new Godot.Color(flash.Color.Red, flash.Color.Green, flash.Color.Blue);

            if (modulate.Tween != null)
            {
                modulate.Tween.Stop();
            }

            node.Modulate = new Godot.Color(flash.Color.Red, flash.Color.Green, flash.Color.Blue);

            modulate.Tween = node.CreateTween();
            modulate.Tween.TweenProperty(node, "modulate",
               new Godot.Color(1, 1, 1),
               .33f);
        }

        foreach (var entity in world.Query().Has<Flash>().Build())
        {
            world.RemoveComponent<Flash>(entity);
        }
    }
}