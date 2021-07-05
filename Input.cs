using Ecs;
using Godot;

public record Player() : Component;

public record Mouse : Component
{
    public bool LeftPressed;
    public bool RightPressed;
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
                    var mouse = state[ENTITY].Get<Mouse>();

                    if ((mouseButton.ButtonIndex & (int)ButtonList.MaskLeft) != 0)
                    {
                        state = state.With(InputEvents.ENTITY, new Mouse
                        {
                            LeftPressed = mouseButton.IsPressed(),
                            RightPressed = mouse?.RightPressed == true
                        });
                    }
                    else if ((mouseButton.ButtonIndex & (int)ButtonList.MaskRight) != 0)
                    {
                        state = state.With(InputEvents.ENTITY, new Mouse
                        {
                            LeftPressed = mouse?.LeftPressed == true,
                            RightPressed = mouseButton.IsPressed()
                        });
                    }
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

        if (mouse?.RightPressed == true)
        {
            var target = game.ToLocal(game.GetViewport().GetMousePosition());

            foreach (var (id, player) in players)
            {
                var position = state[id].Get<Position>();
                var destination = new Position { X = target.x, Y = target.y };
                if (position != destination)
                {
                    state = state.With(id, new Move { Destination = destination });
                }
            }
        }

        return state;
    }
}