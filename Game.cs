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

        systems = new EcsSystems(world, this);
        systems
            .Add(delta)
            .Add(input)
            .Add(new Combat())
            .Add(new Physics())
            .Add(new Renderer())
            .Add(new ComponentDelete<Publish<Sprite>>())
            .Add(new ComponentDelete<Publish<Position>>())
            .Add(new ComponentDelete<Publish<Scale>>())
            .Add(new EntityDelete())
            .Inject()
            .Init();

        var sprites = world.GetPool<Sprite>();
        var positions = world.GetPool<Position>();
        var scales = world.GetPool<Scale>();
        var players = world.GetPool<Player>();
        var speeds = world.GetPool<Speed>();

        {
            var player = world.NewEntity();

            players.Add(player);

            ref var sprite = ref sprites.AddPublish(player);
            sprite.Image = "res://resources/tiles/tile072.png";

            ref var position = ref positions.AddPublish(player);
            position.X = 50;
            position.Y = 50;

            ref var scale = ref scales.AddPublish(player);
            scale.X = 3;
            scale.Y = 3;

            ref var speed = ref speeds.Add(player);
            speed.Value = 3f;
        }

        {
            var fire = world.NewEntity();

            ref var sprite = ref sprites.AddPublish(fire);
            sprite.Image = "res://resources/tiles/tile495.png";

            ref var position = ref positions.AddPublish(fire);
            position.X = 400;
            position.Y = 200;

            ref var scale = ref scales.AddPublish(fire);
            scale.X = 2;
            scale.Y = 2;
        }

        {
            var button = world.NewEntity();

            ref var sprite = ref sprites.AddPublish(button);
            sprite.Image = "res://resources/tiles/tile481.png";

            ref var position = ref positions.AddPublish(button);
            position.X = 300;
            position.Y = 300;

            ref var scale = ref scales.AddPublish(button);
            scale.X = 2;
            scale.Y = 2;
        }

        {
            var potion = world.NewEntity();

            ref var sprite = ref sprites.AddPublish(potion);
            sprite.Image = "res://resources/tiles/tile570.png";

            ref var position = ref positions.AddPublish(potion);
            position.X = 200;
            position.Y = 300;

            ref var scale = ref scales.AddPublish(potion);
            scale.X = 2;
            scale.Y = 2;
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

	public void _Event(Node target, string source, GodotWrapper @event)
	{
		QueueEvent(@event.Get<Event>(),
			Convert.ToInt32(source.Split("-").FirstOrDefault()),
			Convert.ToInt32(target.GetParent().Name?.Split("-").FirstOrDefault()));
	}
	*/
}
