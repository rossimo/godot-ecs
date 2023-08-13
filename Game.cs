using Godot;
using RelEcs;

public partial class Game : Node2D
{
    private World world = new();
    private SystemGroup systems = new();

    public Game() : base()
    {
        world.AddElement(this);

        systems.Add(new RendererSystem());
        systems.Add(new InputSystem());
        systems.Add(new PhysicsSystem());
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

    public override void _Input(InputEvent @event)
    {
        world.Send(@event);
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

            world.AddComponent(entity, physicsNode);
        }

        if (renderNode != null)
        {
            renderNode.SetEntity(entity);
            world.AddComponent(entity, new RenderNode { Node = renderNode });
        }
    }
}
