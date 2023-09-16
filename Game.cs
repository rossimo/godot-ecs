using Godot;
using Flecs.NET.Core;
using static Flecs.NET.Bindings.Native;

public partial class Game : Node2D
{
	private World world = World.Create(System.Environment.GetCommandLineArgs());
	private List<Action> systems = new List<Action>();

	public Game() : base()
	{
		world.Set(this);
		world.Set(new Time());

		systems.Add(Input.System(world));
		systems.Add(Physics.System(world));
		systems.Add(Renderer.System(world));
		
		world.Import<Ecs.Units>();
		world.Import<Ecs.Monitor>();
		world.Set<EcsRest>(default);

		world.Component<Speed>()
			.Member<float, Ecs.Units.Speeds.MilesPerHour>("Value");
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

	public override void _PhysicsProcess(double frameTime)
	{
		ref var time = ref world.GetMut<Time>();
		time.Delta = frameTime;
		time.Scale = (float)(Physics.PHYSICS_RATIO * (frameTime * Physics.PHYSICS_FPS));
		time.Ticks++;

		world.Progress();
		systems.ForEach(system => system());
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouse)
		{
			world.Entity().Set(mouse);
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

		var entity = world.Entity(node.Name);

		foreach (var component in components)
		{
			entity.DiscoverAndSet(component);
			Console.WriteLine($"Added {component} to entity {node.Name}");
		}

		Node2D renderNode = null;
		CharacterBody2D? physicsNode = null;

		if (node is Node2D found)
		{
			renderNode = found;
		}
		else if (node is CharacterBody2D foundPhysics)
		{
			physicsNode = foundPhysics;
		}

		if (physicsNode == null)
		{
			foreach (var parent in new[] { renderNode })
			{
				var potential = parent?.GetChildren().ToArray().OfType<CharacterBody2D>().FirstOrDefault();
				if (potential != null)
				{
					physicsNode = potential;
					break;
				}
			}
		}

		if (physicsNode != null)
		{
			var position = physicsNode.GlobalPosition;
			if (physicsNode.GetParent() != this)
			{
				physicsNode.GetParent().RemoveChild(physicsNode);
				AddChild(physicsNode);
			}

			physicsNode.GlobalPosition = position;
			physicsNode.Scale *= renderNode?.Scale ?? new Vector2(1, 1);
			physicsNode.Rotation += renderNode?.Rotation ?? 0;

			physicsNode.SetEntity(entity);

			entity.Set(physicsNode);
			entity.Set(new Position { X = physicsNode.Position.X, Y = physicsNode.Position.Y });
		}

		if (renderNode != null)
		{
			renderNode.SetEntity(entity);
			entity.Set(new RenderNode { Node = renderNode });
			entity.Set(new Position { X = renderNode.Position.X, Y = renderNode.Position.Y });
		}
	}
}
