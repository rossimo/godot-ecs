using Ecs;
using Godot;
using System;

public class Game : Godot.YSort
{
	public State state;

	public override void _Ready()
	{
		var hero = new Entity(
			new Player(),
			new Position(X: 50, Y: 50),
			new Scale(3, 3),
			new Sprite("res://resources/tiles/tile072.png"));

		var potion = new Entity(
			new Position(X: 200, Y: 200),
			new Collide(),
			new Click(new AddRotation(36f)),
			new Scale(2, 2),
			new Sprite("res://resources/tiles/tile570.png"));

		state = new State() {
			{ "hero", hero },
			{ "potion", potion }
		};
	}

	public void _Collision(Area2D area, string id)
	{
		Console.WriteLine($"{id}");
	}

	public override void _Input(InputEvent @event)
	{
		state = Input.System(state, this, @event);
	}

	public void _Event(string id, GodotWrapper ev)
	{
		state = Event.System(state, id, ev.Get<Component>());
	}

	public override void _Process(float delta)
	{
		state = Movement.System(state);
		state = Items.System(state);
		state = Renderer.System(state, this);
	}
}
