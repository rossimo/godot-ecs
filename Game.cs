using Godot;
using System;
using SimpleEcs;
using System.Linq;

public class Game : Godot.YSort
{
    public State State;
    private State Previous = new State();

    public override void _Ready()
    {
        State = new State() { LoggingIgnore = new[] { typeof(Ticks).Name.GetHashCode() } }
            .With(10,
                new Player(),
                new Speed { Value = 3f },
                new Inventory { },
                new Position { X = 50, Y = 50 },
                new Scale { X = 3, Y = 3 },
                new Area(),
                new Sprite { Image = "res://resources/tiles/tile072.png" },
                new Collision())
            .With(11, Potion)
            .With(12,
                new Position { X = 400, Y = 200 },
                new Area(),
                new Collision(),
                new CollisionEvent(
                    new Add(new Flash { Color = new Color { Red = 2f, Green = 2f, Blue = 0f } }),
                    new Add(new Flash { Color = new Color { Red = 1f, Green = 0f, Blue = 0f } }, Target.Other)
                ),
                new Scale { X = 2, Y = 2 },
                new Sprite { Image = "res://resources/tiles/tile495.png" })
            .With(13,
                new Position { X = 300, Y = 300 },
                new Area(),
                new AreaEnterEvent(
                    new Add(new Flash { Color = new Color { Red = 0.1f, Green = 0.1f, Blue = 0.1f } }),
                    new AddEntity(Potion, 11)
                ),
                new Scale { X = 2, Y = 2 },
                new Sprite { Image = "res://resources/tiles/tile481.png" })
            .With(Events.ENTITY, new EventQueue())
            .With(InputEvents.ENTITY)
            .With(Physics.ENTITY, new Ticks { Tick = 0 });

        if (State.Logging)
        {
            Logger.Log(new State(), State);
        }
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
}
