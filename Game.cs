using DefaultEcs;
using Godot;
using System;
using System.Linq;

public class Game : Godot.YSort
{
    public DefaultEcs.World World = new DefaultEcs.World();

    public override void _Ready()
    {
        var player = World.CreateEntity();
        player.Set<Player>(new Player());
        player.Set<Speed>(new Speed { Value = 2.5f });
        player.Set<Position>(new Position { X = 50, Y = 50 });
        player.Set<Scale>(new Scale { X = 3, Y = 3 });
        player.Set<Area>(new Area());
        player.Set<Sprite>(new Sprite { Image = "res://resources/tiles/tile072.png" });
        player.Set<Collision>(new Collision());

        var fire = World.CreateEntity();
        fire.Set<Position>(new Position { X = 400, Y = 200 });
        fire.Set<Area>(new Area());
        fire.Set<Collision>(new Collision());
        fire.Set<CollisionEvent>(new CollisionEvent(
            new Add<Flash>(new Flash { Color = new Color { Red = 2f, Green = 2f, Blue = 0f } }),
            new Add<Flash>(new Flash { Color = new Color { Red = 1f, Green = 0f, Blue = 0f } }, Target.Other)
        ));
        fire.Set<Scale>(new Scale { X = 2, Y = 2 });
        fire.Set<Sprite>(new Sprite { Image = "res://resources/tiles/tile495.png" });

        var button = World.CreateEntity();
        button.Set<Position>(new Position { X = 300, Y = 300 });
        button.Set<Area>(new Area());
        button.Set<AreaEnterEvent>(new AreaEnterEvent(
            new Add<Flash>(new Flash { Color = new Color { Red = 0.1f, Green = 0.1f, Blue = 0.1f } }),
            new AddEntity(Potion, 11)
        ));
        button.Set<Scale>(new Scale { X = 2, Y = 2 });
        button.Set<Sprite>(new Sprite { Image = "res://resources/tiles/tile481.png" });

        var events = World.CreateEntity();
        events.Set<EventQueue>(new EventQueue());

        var input = World.CreateEntity();

        var physics = World.CreateEntity();
    }

    public static Component[] Potion = new Component[] {
        new Position { X = 200, Y = 300 },
        new Area(),
        new AreaEnterEvent(new RemoveEntity()),
        new Flash { Color = new Color { Red = 2f, Green = 2f, Blue = 2f } },
        new Scale { X = 2, Y = 2 },
        new Sprite { Image = "res://resources/tiles/tile570.png" }
    };

    public override void _Input(InputEvent @event)
    {
        State = InputEvents.System(State, this, @event);
    }

    public override void _PhysicsProcess(float delta)
    {
        State = InputMonitor.System(Previous, State, this);
        State = Events.System(Previous, State);
        State = Combat.System(Previous, State);
        State = Physics.System(Previous, State, this, delta);
        State = Renderer.System(Previous, State, this, delta);

        Previous = State;
    }

    public void QueueEvent(Event @event, int source, int target)
    {
        var entry = (source, target, @event);
        State = State.With(Events.ENTITY, new EventQueue()
        {
            Events = State.EventQueue(Events.ENTITY).Events.With(entry)
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
}
