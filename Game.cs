using System.Collections;
using Godot;
using System.Reflection;
using RelEcs;

public partial class Game : Node2D
{
	private World world = new();
	private SystemGroup systems = new();

	public Game() : base()
	{
		systems.Add(new RendererSystem());
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

	public override void _Process(double deltaValue)
	{
		systems.Run(world);
		world.Tick();
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

		Node2D renderNode = null;

		if (node is Node2D found)
		{
			renderNode = found;
		}

		if (renderNode != null)
		{
			renderNode.SetEntity(entity);
			world.AddComponent(entity, new RenderNode { Node = renderNode });
			world.AddComponent(entity, new ModulateTween());
		}
	}
}
