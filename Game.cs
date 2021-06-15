using Ecs;
using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public class Game : Godot.YSort
{
    public State State;
    private State Previous;
    private int Tick;

    public override void _Ready()
    {
        State = new State() {
            { "hero", new Entity(
                new Player(),
                new Speed(3f),
                new Inventory(),
                new Position(X: 50, Y: 50),
                new Scale(3, 3),
                new Sprite("res://resources/tiles/tile072.png")) },
            { "potion", POTION },
            { "fire", new Entity(
                new Position(X: 400, Y: 200),
                new CollideEvent(
                    new FlashTask(Color: new Color(1f, 0f, 0f), Target: Task.TARGET_OTHER),
                    new FlashTask(Color: new Color(2f, 2f, 0f))),
                new Scale(2, 2),
                new Sprite("res://resources/tiles/tile495.png")) },
            { "button", new Entity(
                new Position(X: 300, Y: 300),
                new CollideEvent(
                    new AddEntityTask(ID: "potion", Entity: POTION),
                    new FlashTask(Color: new Color(0.1f, 0.1f, 0.1f))),
                new Scale(2, 2),
                new Sprite("res://resources/tiles/tile481.png")) }
        };
    }

    public static Entity POTION = new Entity(
        new Position(X: 200, Y: 300),
        new CollideEvent(new RemoveEntityTask()),
        new FlashTask(Color: new Color(2f, 2f, 2f)),
        new Scale(2, 2),
        new Sprite("res://resources/tiles/tile570.png"));

    public override void _Input(InputEvent @event)
    {
        State = Input.System(State, this, @event);
    }

    public void Event(string id, string otherId, Event ev)
    {
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
        State = Physics.System(Previous, State, this);
        Renderer.System(Previous, State, this);

        Log();
        Previous = State;
        Tick = Tick + 1;
    }

    void Log()
    {
        var diffs = new[] {
            Diff.Compare<Sprite>(Previous, State).To<Component>(),
            Diff.Compare<Scale>(Previous, State).To<Component>(),
            Diff.Compare<Rotation>(Previous, State).To<Component>(),
            Diff.Compare<ClickEvent>(Previous, State).To<Component>(),
            Diff.Compare<CollideEvent>(Previous, State).To<Component>(),
            Diff.Compare<Position>(Previous, State).To<Component>(),
            Diff.Compare<PathEvent>(Previous, State).To<Component>(),
            Diff.Compare<Inventory>(Previous, State).To<Component>(),
            Diff.Compare<Move>(Previous, State).To<Component>(),
            Diff.Compare<Velocity>(Previous, State).To<Component>(),
            Diff.Compare<FlashTask>(Previous, State).To<Component>()
        };

        IEnumerable<(string, string)> all = new List<(string, string)>();
        foreach (var (Added, Removed, Changed) in diffs)
        {
            all = all
                .Concat(Removed.Select(entry => (entry.Item1, $"- {entry}")))
                .Concat(Added.Select(entry => (entry.Item1, $"+ {entry}")))
                .Concat(Changed.Select(entry => (entry.Item1, $"~ {entry}")));
        }

        foreach (var entry in all.OrderBy(entry => entry.Item1))
        {
            Console.WriteLine(entry.Item2);
        }
    }
}
