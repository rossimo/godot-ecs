using Godot;
using Arch;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;

public partial class Game : Node2D
{
    private World world = World.Create();
    private Group<Game> systems = new Group<Game>();
    public Entity Global;

    public Game() : base()
    {
        Global = world.Create();
        Global.Add(new FrameTime { Value = 1 / PhysicsSystem.PHYSICS_FPS });

        systems.Add(new RendererSystem(world));
        systems.Add(new InputSystem(world));
        systems.Add(new PhysicsSystem(world));
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

        systems.Initialize();
    }

    public override void _PhysicsProcess(double deltaValue)
    {
        ref var frameTime = ref Global.Get<FrameTime>();
        frameTime.Value = deltaValue;

        systems.BeforeUpdate(this);
        systems.Update(this);
        systems.AfterUpdate(this);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouse)
        {
            world.Create(mouse);
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

        var entity = world.Create();
        foreach (var component in components)
        {
            entity.Add(component);
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

            entity.Add(physicsNode);
        }

        if (renderNode != null)
        {
            renderNode.SetEntity(entity);
            entity.Add(new RenderNode { Node = renderNode });
        }
    }
}
