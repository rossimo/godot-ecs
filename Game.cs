using Godot;
using System;
using System.Linq;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Collections.Generic;

public class Game : Godot.YSort
{
	public EcsWorld world = new EcsWorld();
	public EcsSystems systems;
	public Shared shared;
	public InputSystem input;
	public FrameTimeSystem frameTime;

	public interface RunListener
	{
		void Run();
		void Cancel();
	}

	private EcsPool<PhysicsNode> physicsComponents;
	private EcsPool<AreaNode> areaComponents;
	private EcsPool<PositionTween> positions;
	private EcsPool<ModulateTween> modulates;
	private EcsPool<RenderNode> renders;
	public List<RunListener> RunListeners = new List<RunListener>();

	public override void _Ready()
	{
		physicsComponents = world.GetPool<PhysicsNode>();
		areaComponents = world.GetPool<AreaNode>();
		positions = world.GetPool<PositionTween>();
		modulates = world.GetPool<ModulateTween>();
		renders = world.GetPool<RenderNode>();

		shared = new Shared() { Game = this };

		frameTime = new FrameTimeSystem();
		input = new InputSystem();

		systems = new EcsSystems(world, shared);

		systems
			.Add(frameTime)
			.Add(input)
			.Add(new CombatSystem())
			.Add(new HealthSystem())
			.Add(new PhysicsSystem())
			.Add(new RendererSystem())
			.Add(new DeleteComponentSystem<Notify<AreaNode>>())
			.Add(new DeleteComponentSystem<Notify<Sprite>>())
			.Add(new DeleteComponentSystem<Notify<Flash>>())
			.Add(new DeleteComponentSystem<Notify<Area>>())
			.Add(new DeleteComponentSystem<Collision>())
			.Add(new DeleteEntitySystem())
			.Inject()
			.Init();

		foreach (var node in GetChildren().OfType<Godot.Node>())
		{
			DiscoverEntity(node);
		}

		systems.Init();
	}

	public override void _ExitTree()
	{
		foreach (var listener in RunListeners)
		{
			listener.Cancel();
		}
	}

	public int DiscoverEntity(Node node)
	{
		var components = node.ToComponents("components/");
		if (components.Length == 0) return -1;

		var entity = world.NewEntity();

		foreach (var component in components)
		{
			world.AddNotify(entity, component);
		}

		Node2D renderNode = null;
		KinematicBody2D physicsNode = null;
		Area2D areaNode = null;

		if (node is KinematicBody2D foundPhysics)
		{
			physicsNode = foundPhysics;
		}
		if (node is Area2D foundArea)
		{
			areaNode = foundArea;
		}
		else if (node is Node2D found)
		{
			renderNode = found;
		}

		if (areaNode == null)
		{
			foreach (var parent in new[] { physicsNode, renderNode })
			{
				var potential = parent?.GetChildren().ToArray<Godot.Node>().OfType<Area2D>().FirstOrDefault();
				if (potential != null)
				{
					areaNode = potential;
					break;
				}
			}
		}

		if (physicsNode == null)
		{
			foreach (var parent in new[] { renderNode })
			{
				var potential = parent?.GetChildren().ToArray<Godot.Node>().OfType<KinematicBody2D>().FirstOrDefault();
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
			physicsNode.Scale *= renderNode.Scale;
			physicsNode.Rotation += renderNode.Rotation;

			physicsNode.SetEntity(world, entity);

			ref var physicsComponent = ref physicsComponents.Add(entity);
			physicsComponent.Node = physicsNode;
		}

		if (areaNode != null)
		{
			if (physicsNode != null && areaNode.GetParent() != physicsNode)
			{
				var position = areaNode.GlobalPosition;

				areaNode.GetParent().RemoveChild(areaNode);
				physicsNode.AddChild(areaNode);

				areaNode.GlobalPosition = position;
				areaNode.Scale *= renderNode.Scale;
				areaNode.Rotation += renderNode.Rotation;
			}

			areaNode.SetEntity(world, entity);

			ref var areaComponent = ref areaComponents.Add(entity);
			areaComponents.Notify(entity);
			areaComponent.Node = areaNode;
		}

		if (renderNode != null)
		{
			renderNode.SetEntity(world, entity);

			ref var render = ref renders.Add(entity);
			render.Node = renderNode;

			ref var position = ref positions.Add(entity);
			position.Tween = new Tween() { Name = "position" };
			renderNode.AddChild(position.Tween);

			ref var modulate = ref modulates.Add(entity);
			modulate.Tween = new Tween() { Name = "modulate" };
			renderNode.AddChild(modulate.Tween);
		}

		return entity;
	}

	public override void _Input(InputEvent @event)
	{
		input.Run(systems, @event);
	}

	public override void _PhysicsProcess(float deltaValue)
	{
		frameTime.Run(systems, deltaValue);
		systems.Run();
		foreach (var listener in RunListeners)
		{
			listener.Run();
		}
	}

	public void AreaEvent(Node targetNode, GodotWrapper sourceWrapper)
	{
		var packedSource = sourceWrapper.Get<EcsPackedEntity>();
		int entity = -1;
		packedSource.Unpack(world, out entity);

		int other = targetNode.GetEntity(world);

		var pool = world.GetPool<Many<Event<Area>>>();
		if (pool.SafeHas(entity))
		{
			ref var events = ref pool.Get(entity);
			foreach (var ev in events)
			{
				ev.Add(world, entity, other);
			}
		}

		if (pool.SafeHas(other))
		{
			ref var events = ref pool.Get(other);
			foreach (var ev in events)
			{
				ev.Add(world, other, entity);
			}
		}
	}
}
