using System.Collections;
using Godot;
using System.Reflection;
using RelEcs;

public partial class Game : Node2D
{
    private World world = new World();

    public Game() : base()
    {

    }

    public override void _Ready()
    {
        foreach (var node in GetChildren().OfType<Node>())
        {
            try
            {
                DiscoverEntity(node);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to discover entity for {node.Name}");
                Console.WriteLine(e);
            }
        }
    }

    public void DiscoverEntity(Node node)
    {
        var components = node.ToComponents("components/");

        Console.WriteLine($"Discovered {components.Length} components for {node.Name}");

        if (!components.Any())
        {
            return;
        }

        var entity = world.Spawn().Id();
        foreach (var component in components)
        {
            world.UnsafeAddComponent(entity, component);
            Console.WriteLine($"Added {component} to entity {entity}");
        }
    }
}
