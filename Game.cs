using Godot;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

public class Game : Godot.YSort
{
    public EcsWorld world;
    public EcsSystems systems;
    public Input input;
    public DeltaSystem delta;

    public override void _Ready()
    {
        world = new EcsWorld();
        delta = new DeltaSystem();
        input = new Input();

        systems = new EcsSystems(world, new Shared()
        {
            Game = this
        });

        systems
            .Add(delta)
            .Add(input)
            .Add(new Events())
            .Add(new Combat())
            .Add(new Physics())
            .Add(new Renderer())
            .Add(new ComponentDelete<Notify<Sprite>>())
            .Add(new ComponentDelete<Notify<Position>>())
            .Add(new ComponentDelete<Notify<Scale>>())
            .Add(new ComponentDelete<Notify<Flash>>())
            .Add(new ComponentDelete<Notify<Collision>>())
            .Add(new ComponentDelete<Notify<Area>>())
            .Add(new EntityDelete())
            .Inject()
            .Init();

        var sprites = world.GetPool<Sprite>();
        var positions = world.GetPool<Position>();
        var scales = world.GetPool<Scale>();
        var players = world.GetPool<Player>();
        var speeds = world.GetPool<Speed>();
        var collisions = world.GetPool<Collision>();
        var collisionTriggers = world.GetPool<Trigger<Collision>>();
        var areas = world.GetPool<Area>();
        var areaTriggers = world.GetPool<Trigger<Area>>();

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
            triggers.Tasks = new Task[] {
                new AddNotifySelf<Flash>() {
                    Component = new Flash() {
                        Color = new Color() { Red = 2f, Green = 2f, Blue = 2f }
                    }
                },
                new AddNotifyOther<Flash>() {
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
            triggers.Tasks = new Task[] {
                new AddNotifySelf<Flash>() {
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
        }

        systems.Init();
    }

    public override void _Input(InputEvent @event)
    {
        input.Run(systems, @event);
    }

    public override void _PhysicsProcess(float deltaValue)
    {
        delta.Run(systems, deltaValue);
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

    public void _Event(Node otherNode, GodotWrapper sourceWrapper, GodotWrapper tasksWrapper)
    {
        var selfEntity = -1;
        var otherEntity = -1;
        var tasks = tasksWrapper.Get<Task[]>();

        sourceWrapper.Get<EcsPackedEntity>().Unpack(world, out selfEntity);

        if (otherNode is EntityNode otherEntityNode)
        {
            otherEntityNode.Entity.Unpack(world, out otherEntity);
        }

        tasks.Run(world, selfEntity, otherEntity);
    }
}
