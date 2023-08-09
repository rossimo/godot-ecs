using Godot;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

public partial class Game : Godot.Node2D
{

    public Game() : base()
    {

    }

    public override void _Ready()
    {
        foreach (var node in GetChildren().OfType<Godot.Node>())
        {
            try
            {
                DiscoverEntity(node);
            }
            catch (System.Exception e)
            {
                Console.WriteLine($"Unable to discover entity for {node.Name}");
                Console.WriteLine(e);
            }
        }
    }

    public void DiscoverEntity(Node node)
    {
        var components = node.ToComponents("components/");

        Console.WriteLine($"Discovered {components.Count()} components for {node.Name}");
    }
}
