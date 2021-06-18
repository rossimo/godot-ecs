using Ecs;
using Godot;
using System;
using System.Linq;

public class Game : Godot.YSort
{
    public State State;
    private State Previous;
    private int Tick;

    public override void _Ready()
    {
        State = new State() {
            { "hero", new Entity(
                new Player { },
                new Speed { Value = 6f },
                new Inventory { },
                new Position{ X = 50, Y = 50 },
                new Scale { X = 3, Y = 3 },
                new CollideEvent(),
                new Sprite { Image = "res://resources/tiles/tile072.png" })},
            { "potion", Potion },
            { "fire", new Entity(
                new Position { X = 400, Y = 200 },
                new CollideEvent(
                    new Add(new Flash { Color = new Color { Red = 1f, Green = 0f, Blue = 0f } }, Target.Other),
                    new Add(new Flash { Color = new Color { Red = 2f, Green = 2f, Blue = 0f } })
                ),
                new Scale { X = 2, Y = 2 },
                new Sprite { Image = "res://resources/tiles/tile495.png" })},
            { "button", new Entity(
                new Position { X = 300, Y = 300 },
                new CollideEvent(
                    new Add(new Flash { Color = new Color { Red = 0.1f, Green = 0.1f, Blue = 0.1f } }),
                    new AddEntity(Potion, "potion")
                ),
                new Scale { X = 2, Y = 2 },
                new Sprite { Image = "res://resources/tiles/tile481.png" })}
        };
    }

    public static Entity Potion = new Entity(
        new Position { X = 200, Y = 300 },
        new CollideEvent(new RemoveEntity()),
        new Flash { Color = new Color { Red = 2f, Green = 2f, Blue = 2f } },
        new Scale { X = 2, Y = 2 },
        new Sprite { Image = "res://resources/tiles/tile570.png" });

    public override void _Input(InputEvent @event)
    {
        State = Input.System(State, this, @event);
    }

    public void Event(string id, string otherId, Event ev)
    {
        ev = ev with
        {
            Tasks = ev.Tasks.Select(task =>
                task is Add add && add.Component is TickComponent tickComponent
                    ? add with { Component = tickComponent with { Tick = Tick } }
                    : task
            ).ToArray()
        };

        State = Events.System(Tick, State, id, otherId, ev);
    }

    public void _Event(string id, GodotWrapper ev)
    {
        Event(id, null, ev.Get<Event>());
    }

    public void _Event(Node other, string id, GodotWrapper ev)
    {
        Event(id, other.GetParent().Name, ev.Get<Event>());
    }

    public override void _PhysicsProcess(float delta)
    {
        Renderer.System(Previous, State, this);
        State = Physics.System(Previous, State, this, delta);

        Previous = State;
        Tick = Tick + 1;
    }
}
