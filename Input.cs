using Ecs;
using Godot;

public record Player() : Component;

public record Mouse : Component
{
    public bool Pressed;
}

public record Move : Component
{
    public Position Destination;
}

public static class InputEvents
{
    public static string ENTITY = "input";

    public static Ecs.State System(Ecs.State state, Game game, InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton mouseButton:
                {
                    state = state.With(InputEvents.ENTITY, new Mouse { Pressed = mouseButton.IsPressed() });
                }
                break;
        }
        return state;
    }
}

public static class InputMonitor
{
    public static State System(State previous, State state, Game game)
    {
        var mouse = state[InputEvents.ENTITY].Get<Mouse>();
        var players = state.Get<Player>();

        if (mouse?.Pressed == true)
        {
            var target = game.ToLocal(game.GetViewport().GetMousePosition());

            foreach (var (id, player) in players)
            {
                var position = state[id].Get<Position>();
                var source = new Vector2(position.X, position.Y);
                var velocity = source.DirectionTo(new Vector2(target));
                state = state.With(id, new Move { Destination = new Position { X = target.x, Y = target.y } });
            }
        }

        return state;
    }
}