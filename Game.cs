using Godot;
using Leopotam.EcsLite;

public class Game : Godot.YSort
{
    public EcsWorld world;
    public EcsSystems systems;

    public override void _Ready()
    {
        world = new EcsWorld();
        systems = new EcsSystems(world, this)
            .Add(new Renderer(world))
            .Add(new Physics());
        systems.Init();

        var sprites = world.GetPool<Sprite>();
        var positions = world.GetPool<Position>();
        var scales = world.GetPool<Scale>();
        var velocities = world.GetPool<Velocity>();

        {
            var player = world.NewEntity();

            ref var sprite = ref sprites.AddEmit(world, player);
            sprite.Image = "res://resources/tiles/tile072.png";

            ref var position = ref positions.AddEmit(world, player);
            position.X = 50;
            position.Y = 50;

            ref var scale = ref scales.AddEmit(world, player);
            scale.X = 3;
            scale.Y = 3;

            ref var velocity = ref velocities.AddEmit(world, player);
            velocity.X = 3;
            velocity.Y = 3;
        }

        {
            var fire = world.NewEntity();

            ref var sprite = ref sprites.AddEmit(world, fire);
            sprite.Image = "res://resources/tiles/tile495.png";

            ref var position = ref positions.AddEmit(world, fire);
            position.X = 400;
            position.Y = 200;

            ref var scale = ref scales.AddEmit(world, fire);
            scale.X = 2;
            scale.Y = 2;
        }

        {
            var button = world.NewEntity();

            ref var sprite = ref sprites.AddEmit(world, button);
            sprite.Image = "res://resources/tiles/tile481.png";

            ref var position = ref positions.AddEmit(world, button);
            position.X = 300;
            position.Y = 300;

            ref var scale = ref scales.AddEmit(world, button);
            scale.X = 2;
            scale.Y = 2;
        }

        {
            var potion = world.NewEntity();

            ref var sprite = ref sprites.AddEmit(world, potion);
            sprite.Image = "res://resources/tiles/tile570.png";

            ref var position = ref positions.AddEmit(world, potion);
            position.X = 200;
            position.Y = 300;

            ref var scale = ref scales.AddEmit(world, potion);
            scale.X = 2;
            scale.Y = 2;
        }

        systems.Init();
    }

    public override void _Input(InputEvent @event)
    {
        //State = InputEvents.System(State, this, @event);
    }

    public override void _PhysicsProcess(float delta)
    {
        systems.Run();
        //State = InputMonitor.System(Previous, State, this);
        //State = Events.System(Previous, State);
        //State = Combat.System(Previous, State);
        //State = Physics.System(Previous, State, this, delta);
        //State = Renderer.System(Previous, State, this, delta);
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
