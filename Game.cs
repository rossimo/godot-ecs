using Flecs;
using Godot;
using System.Reflection;
using System.Runtime.InteropServices;

public partial class Game : Node2D
{
    private World world;

    public Game() : base()
    {
        NativeLibrary.Load("flecs.dll", Assembly.GetExecutingAssembly(), null);

        world = new World(Array.Empty<string>());

        world.RegisterComponent<Health>();
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

        var entity = world.CreateEntity(node.Name);

        foreach (var component in components)
        {
            Console.WriteLine($"Adding component {component} to {node.Name}");
            entity.UnsafeSet(component);
        }
    }
}
