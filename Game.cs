using DefaultEcs;
using Godot;
using System;
using System.Linq;

public class Game : Godot.YSort
{
    public DefaultEcs.World World;
    public Renderer Renderer;
    public Events Events;
    public Physics Physics;
    public InputEvents InputEvents;
    public InputMonitor InputMonitor;
    public Combat Combat;

    public override void _Ready()
    {
        World = new DefaultEcs.World();
        Renderer = new Renderer(World);
        Events = new Events(World);
        Physics = new Physics(World);
        InputEvents = new InputEvents(World);
        InputMonitor = new InputMonitor(World);
        Combat = new Combat(World);

        var player = World.CreateEntity();
        player.Set(new Player());
        player.Set(new Speed { Value = 2.5f });
        player.Set(new Position { X = 50, Y = 50 });
        player.Set(new Scale { X = 3, Y = 3 });
        player.Set(new Area());
        player.Set(new Sprite { Image = "res://resources/tiles/tile072.png" });
        player.Set(new Collision());

        var fire = World.CreateEntity();
        fire.Set(new Position { X = 400, Y = 200 });
        fire.Set(new Area());
        fire.Set(new Collision());
        fire.Set(new CollisionEvent(
            new Add(new Flash { Color = new Color { Red = 2f, Green = 2f, Blue = 0f } }),
            new Add(new Flash { Color = new Color { Red = 1f, Green = 0f, Blue = 0f } }) with { Target = Target.Other }
        ));
        fire.Set(new Scale { X = 2, Y = 2 });
        fire.Set(new Sprite { Image = "res://resources/tiles/tile495.png" });

        var button = World.CreateEntity();
        button.Set(new Position { X = 300, Y = 300 });
        button.Set(new Area());
        button.Set(new AreaEnterEvent(
            new Add(new Flash { Color = new Color { Red = 0.1f, Green = 0.1f, Blue = 0.1f } })
        //new AddEntity(Potion, 11)
        ));
        button.Set(new Scale { X = 2, Y = 2 });
        button.Set(new Sprite { Image = "res://resources/tiles/tile481.png" });
    }

    /*
		public static Component[] Potion = new Component[] {
			new Position { X = 200, Y = 300 },
			new Area(),
			new AreaEnterEvent(new RemoveEntity()),
			new Flash { Color = new Color { Red = 2f, Green = 2f, Blue = 2f } },
			new Scale { X = 2, Y = 2 },
			new Sprite { Image = "res://resources/tiles/tile570.png" }
		};
	*/
    public override void _Input(InputEvent @event)
    {
        InputEvents.System(this, @event);
    }

    public override void _PhysicsProcess(float delta)
    {
        InputMonitor.System(this);
        Events.System();
        Combat.System();
        Physics.System(this, delta);
        Renderer.System(this, delta);
    }

    public void QueueEvent(Event @event, Entity source, Entity target)
    {
        var entry = (source, target, @event);

        World.Set(new EventQueue()
        {
            Events = World.Get<EventQueue>().Events.With(entry)
        });
    }

    public void _Event(GodotWrapper entity, GodotWrapper @event)
    {
        QueueEvent(@event.Get<Event>(), entity.Get<Entity>(), World.SingleOrDefault());
    }

    public void _Event(Node node, GodotWrapper source, GodotWrapper @event)
    {
        if (node is EntityNode entityNode)
        {
            QueueEvent(@event.Get<Event>(), source.Get<Entity>(), entityNode.Entity);
        }
        else
        {
            Console.WriteLine($"{node.Name} does not contain entity!");
        }
    }
}
