using Godot;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

public class Game : Godot.YSort
{
    public EcsWorld world;
    public EcsSystems systems;
    public Shared shared;
    public InputSystem input;
    public FrameTimeSystem frameTime;
    public EventSystem events;

    public static void ReflectionAdd<T>(EcsPool<T> pool, int entity, T value)
        where T : struct
    {
        ref var reference = ref pool.Add(entity);
        reference = value;
    }

    public override void _Ready()
    {
        world = new EcsWorld();

        shared = new Shared() { Game = this };

        frameTime = new FrameTimeSystem();
        input = new InputSystem();
        events = new EventSystem();

        systems = new EcsSystems(world, shared);

        systems
            .Add(frameTime)
            .Add(input)
            .Add(events)
            .Add(new CombatSystem())
            .Add(new PhysicsSystem())
            .Add(new RendererSystem())
            .Add(new DeleteComponentSystem<Notify<Sprite>>())
            .Add(new DeleteComponentSystem<Notify<Position>>())
            .Add(new DeleteComponentSystem<Notify<Scale>>())
            .Add(new DeleteComponentSystem<Notify<Flash>>())
            .Add(new DeleteComponentSystem<Notify<Collision>>())
            .Add(new DeleteComponentSystem<Notify<Area>>())
            .Add(new DeleteEntitySystem())
            .Inject()
            .Init();

        foreach (Godot.Node child in GetChildren())
        {
            var components = child.ToComponents();
            if (components.Length == 0) continue;

            var entity = world.NewEntity();

            foreach (var component in components)
            {
                var getPool = world.GetType().GetMethod("GetPool")
                    .MakeGenericMethod(component.GetType());
                var pool = getPool.Invoke(world, null);

                var add = typeof(Game).GetMethod("ReflectionAdd")
                    .MakeGenericMethod(component.GetType());
                add.Invoke(null, new[] { pool, entity, component });
            };
        }

        /*
		var sprites = world.GetPool<Sprite>();
		var positions = world.GetPool<Position>();
		var scales = world.GetPool<Scale>();
		var players = world.GetPool<Player>();
		var speeds = world.GetPool<Speed>();
		var collisions = world.GetPool<Collision>();
		var collisionTriggers = world.GetPool<EventTrigger<Collision>>();
		var areas = world.GetPool<Area>();
		var areaTriggers = world.GetPool<EventTrigger<Area>>();

		{
			var player = world.NewEntity();

			players.Add(player);

			ref var sprite = ref sprites.AddNotify(player);
			sprite.Image = "res://resources/tiles/tile072.png";

			ref var position = ref positions.AddNotify(player);
			position.X = 50;
			position.Y = 50;

			ref var scale = ref scales.AddNotify(player);
			scale.X = 3;
			scale.Y = 3;

			ref var speed = ref speeds.Add(player);
			speed.Value = 3f;

			areas.AddNotify(player);

			collisions.AddNotify(player);
		}

		{
			var fire = world.NewEntity();

			ref var sprite = ref sprites.AddNotify(fire);
			sprite.Image = "res://resources/tiles/tile495.png";

			ref var position = ref positions.AddNotify(fire);
			position.X = 400;
			position.Y = 200;

			ref var scale = ref scales.AddNotify(fire);
			scale.X = 2;
			scale.Y = 2;

			collisions.AddNotify(fire);

			ref var triggers = ref collisionTriggers.AddNotify(fire);
			triggers.Tasks = new EventTask[] {
				new Add<Flash>() {
					Notify = true,
					Component = new Flash() {
						Color = new Color() { Red = 2f, Green = 2f, Blue = 2f }
					}
				},
				new Add<Flash>() {
					Notify = true,
					Target = Target.Other,
					Component = new Flash() {
						Color = new Color() { Red = 2f, Green = 0f, Blue = 0f }
					}
				}
			};
		}

		{
			var button = world.NewEntity();

			ref var sprite = ref sprites.AddNotify(button);
			sprite.Image = "res://resources/tiles/tile481.png";

			ref var position = ref positions.AddNotify(button);
			position.X = 300;
			position.Y = 300;

			ref var scale = ref scales.AddNotify(button);
			scale.X = 2;
			scale.Y = 2;

			areas.AddNotify(button);

			ref var triggers = ref areaTriggers.AddNotify(button);
			triggers.Tasks = new EventTask[] {
				new Add<Flash>() {
					Notify = true,
					Component = new Flash() {
						Color = new Color() { Red = 0.33f, Green = 0.33f, Blue = 0.33f }
					}
				}
			};
		}

		{
			var potion = world.NewEntity();

			ref var sprite = ref sprites.AddNotify(potion);
			sprite.Image = "res://resources/tiles/tile570.png";

			ref var position = ref positions.AddNotify(potion);
			position.X = 200;
			position.Y = 300;

			ref var scale = ref scales.AddNotify(potion);
			scale.X = 2;
			scale.Y = 2;

			areas.AddNotify(potion);

			ref var triggers = ref areaTriggers.AddNotify(potion);
			triggers.Tasks = new EventTask[] {
				new Add<DeleteEntity>()
			};
		}
		*/

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

    public void _Event(Node targetNode, GodotWrapper sourceWrapper, GodotWrapper tasksWrapper)
    {
        var tasks = tasksWrapper.Get<EventTask[]>();
        var source = sourceWrapper.Get<EcsPackedEntity>();
        var target = targetNode is EntityNode entityNode ? entityNode.Entity : default;

        foreach (var task in tasks)
        {
            events.Queue(new Event()
            {
                Task = task,
                Source = source,
                Target = target
            });
        }
    }
}
