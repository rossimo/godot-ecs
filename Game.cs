using Godot;
using System.Linq;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

public class Game : Godot.YSort
{
    public EcsWorld world;
    public EcsSystems systems;
    public Shared shared;
    public InputSystem input;
    public FrameTimeSystem frameTime;

    public override void _Ready()
    {
        world = new EcsWorld();

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
            .Add(new DeleteComponentSystem<Notify<Sprite>>())
            .Add(new DeleteComponentSystem<Notify<Flash>>())
            .Add(new DeleteComponentSystem<Notify<Area>>())
            .Add(new DeleteEntitySystem())
            .Inject()
            .Init();

        var physicsComponents = world.GetPool<PhysicsNode>();
        var positionTweens = world.GetPool<PositionTween>();
        var renders = world.GetPool<RenderNode>();

        foreach (var node in GetChildren().OfType<Godot.Node>())
        {
            var components = node.ToComponents("components/");
            if (components.Length == 0) continue;

            var entity = world.NewEntity();

            foreach (var component in components)
            {
                world.AddNotify(entity, component);
            }

            Node2D renderNode = null;
            KinematicBody2D physicsNode = null;

            if (node is KinematicBody2D physics)
            {
                physicsNode = physics;
            }
            else if (node is Node2D render)
            {
                renderNode = render;

                physicsNode = renderNode.GetChildren().ToArray<Godot.Node>()
                    .OfType<KinematicBody2D>().FirstOrDefault();

                if (physicsNode != null)
                {
                    var position = physicsNode.GlobalPosition;
                    renderNode.RemoveChild(physicsNode);
                    AddChild(physicsNode);

                    physicsNode.GlobalPosition = position;
                    physicsNode.Scale *= renderNode.Scale;
                    physicsNode.Rotation += renderNode.Rotation;
                }
            }

            if (physicsNode != null)
            {
                physicsNode.SetEntity(world, entity);

                ref var physicsComponent = ref physicsComponents.Add(entity);
                physicsComponent.Node = physicsNode;
            }

            if (renderNode != null)
            {
                renderNode.SetEntity(world, entity);

                ref var render = ref renders.Add(entity);
                render.Node = renderNode;

                ref var positionTweenComponent = ref positionTweens.Add(entity);
                positionTweenComponent.Tween = new Tween() { Name = "position" };
                renderNode.AddChild(positionTweenComponent.Tween);
            }
        }

        systems.Init();
    }

    public override void _Input(InputEvent @event)
    {
        input.Run(systems, @event);
    }

    public override void _PhysicsProcess(float deltaValue)
    {
        frameTime.Run(systems, deltaValue);
        systems.Run();
    }

    /*
	public void QueueEvent(Event @event, int source, int target)
	{

		var entry = (source, target, @event);
		State = State.With(Events.ENTITY, new EventQueue()
		{
			Events = State.Get<EventQueue>(Events.ENTITY).Events.With(entry)
		});
	}

	public void _Event(string source, GodotWrapper @event)
	{
		QueueEvent(@event.Get<Event>(), Convert.ToInt32(source.Split("-").FirstOrDefault()), -2);
	}
	*/

    public void AreaEvent(Node targetNode, GodotWrapper sourceWrapper)
    {
        var packedSource = sourceWrapper.Get<EcsPackedEntity>();
        int entity = -1;
        packedSource.Unpack(world, out entity);

        int other = targetNode.GetEntity(world);

        var pool = world.GetPool<Many<Event<Area>>>();
        if (pool.Has(entity))
        {
            ref var events = ref pool.Get(entity);
            foreach (var ev in events)
            {
                ev.Add(world, entity, other);
            }
        }

        if (pool.Has(other))
        {
            ref var events = ref pool.Get(other);
            foreach (var ev in events)
            {
                ev.Add(world, other, entity);
            }
        }
    }
}
