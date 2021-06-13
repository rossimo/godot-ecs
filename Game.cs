using Ecs;
using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public class Game : Godot.YSort
{
    public State State;
    private State Previous;

    public override void _Ready()
    {
        State = new State() {
            { "hero", new Entity(
                new Player(),
                new Inventory(),
                new Position(X: 50, Y: 50),
                new Click(new AddRotation(Degrees: -36f)),
                new Scale(3, 3),
                new Sprite("res://resources/tiles/tile072.png")) },
            { "potion", new Entity(
                new Position(X: 200, Y: 300),
                new Collide(new RemoveEntity(), new AddItem(TargetOther: true, Item: new Component())),
                new Click(new AddRotation(Degrees: 36f)),
                new Scale(2, 2),
                new Sprite("res://resources/tiles/tile570.png")) },
            { "fire", new Entity(
                new Position(X: 400, Y: 200),
                new Collide(new RemoveEntity(TargetOther: true)),
                new Scale(2, 2),
                new Sprite("res://resources/tiles/tile495.png")) }
        };
    }

    public override void _Input(InputEvent @event)
    {
        State = Input.System(State, this, @event);
    }

    public void _Event(string id, GodotWrapper ev)
    {
        State = Events.System(State, id, null, ev.Get<Component>());
    }

    public void _Event(Node other, string id, GodotWrapper ev)
    {
        State = Events.System(State, id, other.GetParent().Name, ev.Get<Component>());
    }

    public override void _PhysicsProcess(float delta)
    {
        State = Movement.System(State, this);

        Renderer.System(Previous, State, this);

        Log();
        Previous = State;
    }

    void Log()
    {
        var diffs = new[] {
             Diff.Compare<Sprite>(Previous, State).To<Component>(),
             Diff.Compare<Scale>(Previous, State).To<Component>(),
             Diff.Compare<Rotation>(Previous, State).To<Component>(),
             Diff.Compare<Click>(Previous, State).To<Component>(),
             Diff.Compare<Sprite>(Previous, State).To<Component>(),
             Diff.Compare<Collide>(Previous, State).To<Component>(),
             Diff.Compare<Position>(Previous, State).To<Component>(),
             Diff.Compare<Move>(Previous, State).To<Component>(),
             Diff.Compare<Inventory>(Previous, State).To<Component>()
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
