using Ecs;
using Godot;
using System;
using System.Linq;

public class Game : Godot.YSort
{
    public State State;
    private State Previous;

    public override void _Ready()
    {
        State = new State() {
            { "hero", new Entity(
                new Player { },
                new Speed { Value = 2.5f },
                new Inventory { },
                new Position{ X = 50, Y = 50 },
                new Scale { X = 3, Y = 3 },
                new EnterEvent(),
                new Sprite { Image = "res://resources/tiles/tile072.png" },
                new Collision())},
            { "potion", Potion },
            { "fire", new Entity(
                new Position { X = 400, Y = 200 },
                new EnterEvent(
                    new Add(new Flash { Color = new Color { Red = 1f, Green = 0f, Blue = 0f } }, Target.Other),
                    new Add(new Flash { Color = new Color { Red = 2f, Green = 2f, Blue = 0f } })
                ),
                new Scale { X = 2, Y = 2 },
                new Sprite { Image = "res://resources/tiles/tile495.png" })},
            { "button", new Entity(
                new Position { X = 300, Y = 300 },
                new EnterEvent(
                    new Add(new Flash { Color = new Color { Red = 0.1f, Green = 0.1f, Blue = 0.1f } }),
                    new AddEntity(Potion, "potion")
                ),
                new Scale { X = 2, Y = 2 },
                new Sprite { Image = "res://resources/tiles/tile481.png" })},
            { "input", new Entity() { }},
            { "physics", new Entity(new Ticks { Tick = 0 })}
        };

        State.Log(null, State);
    }

    public static Entity Potion = new Entity(
        new Position { X = 200, Y = 300 },
        new EnterEvent(new RemoveEntity()),
        new Flash { Color = new Color { Red = 2f, Green = 2f, Blue = 2f } },
        new Scale { X = 2, Y = 2 },
        new Sprite { Image = "res://resources/tiles/tile570.png" });

    public override void _Input(InputEvent @event)
    {
        State = InputEvents.System(State, this, @event);
    }

    public override void _Process(float delta)
    {
        State = InputMonitor.System(Previous, State, this);
    }

    public override void _PhysicsProcess(float delta)
    {
        State = Physics.System(Previous, State, this, delta);
        State = Renderer.System(Previous, State, this);

        Previous = State;
        GC.Collect();
    }

    public void Event(Event ev, string id = null, string otherId = null)
    {
        State = Events.System(State, id, otherId?.Split("-").FirstOrDefault(), ev);
    }

    public void _Event(string id, GodotWrapper ev)
    {
        Event(ev.Get<Event>(), id);
    }

    public void _Event(Node other, string id, GodotWrapper ev)
    {
        Event(ev.Get<Event>(), id, other.GetParent().Name);
    }
}