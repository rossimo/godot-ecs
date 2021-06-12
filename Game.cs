using Ecs;
using Godot;
using System;

public class Game : Godot.YSort
{
    public State State;
    private State Previous;

    public override void _Ready()
    {
        var hero = new Entity(
            new Player(),
            new Position(X: 50, Y: 50),
            new Click(new AddRotation(36f)),
            new Scale(3, 3),
            new Sprite("res://resources/tiles/tile072.png"));

        var potion = new Entity(
            new Position(X: 200, Y: 200),
            new Collide(),
            new Click(new AddRotation(36f)),
            new Scale(2, 2),
            new Sprite("res://resources/tiles/tile570.png"));

        State = new State() {
            { "hero", hero },
            { "potion", potion }
        };
    }

    public override void _Input(InputEvent @event)
    {
        State = Input.System(State, this, @event);
    }

    public void _Event(string id, GodotWrapper ev)
    {
        State = Event.System(State, id, ev.Get<Component>());
    }

    public override void _PhysicsProcess(float delta)
    {
        State = Movement.System(State);
        State = Items.System(State);
        State = Renderer.System(Previous, State, this);
        Previous = State;
    }

    public void _Collision(Area2D area, string id)
    {
        Console.WriteLine($"{id}");
    }
}
