using Godot;
using System;
using SimpleEcs;
using System.Linq;

public record Player() : Component;

public record MouseLeft : Component
{
    public bool Pressed;
}

public record MouseRight : Component
{
    public bool Pressed;
}

public record Move : Component
{
    public Position Destination;
}

public static class InputEvents
{
    public static int ENTITY = 3;

    public static State System(State state, Game game, InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton mouseButton:
                {
                    if ((mouseButton.ButtonIndex & (int)ButtonList.MaskLeft) != 0)
                    {
                        state = state.With(InputEvents.ENTITY, new MouseLeft
                        {
                            Pressed = mouseButton.IsPressed()
                        });
                    }
                    else if ((mouseButton.ButtonIndex & (int)ButtonList.MaskRight) != 0)
                    {
                        state = state.With(InputEvents.ENTITY, new MouseRight
                        {
                            Pressed = mouseButton.IsPressed()
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
    public static int ENTITY = InputEvents.ENTITY;

    private static Random rnd = new Random();

    public static State System(State previous, State state, Game game)
    {
        var tick = state.Ticks(Physics.ENTITY).Tick;

        var mouseLeft = state.MouseLeft(ENTITY);
        var mouseRight = state.MouseRight(ENTITY);

        var playerId = state.Player().FirstOrDefault().Key;

        var position = state.Position(playerId);
        var mousePosition = game.ToLocal(game.GetViewport().GetMousePosition());

        if (mouseRight?.Pressed == true)
        {
            var destination = new Position { X = mousePosition.x, Y = mousePosition.y };
            if (position != destination)
            {
                state = state.With(playerId, new Move { Destination = destination });
            }
        }

        if (mouseLeft?.Pressed == true)
        {
            var direction = new Vector2(position.X, position.Y).DirectionTo(mousePosition).Normalized() * 10f;
            if (direction.x != 0 && direction.y != 0)
            {
                state = state.With(rnd.Next(1000, 200000), 
                   state.Position(playerId),
                   new Sprite { Image = "res://resources/tiles/tile663.png" },
                   new Velocity { X = direction.x, Y = direction.y },
                   new LowRenderPriority(),
                   new ExpirationEvent(new RemoveEntity()) with { Tick = Physics.MillisToTicks(1 * 1000) + tick }
                );
            }
        }

        return state;
    }
}