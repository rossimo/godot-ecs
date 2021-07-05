using Ecs;
using Godot;
using System;
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
    public static string ENTITY = "input";

    public static Ecs.State System(Ecs.State state, Game game, InputEvent @event)
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
    public static string ENTITY = InputEvents.ENTITY;

    public static State System(State previous, State state, Game game)
    {
        var tick = state["physics"].Get<Ticks>().Tick;

        var mouseLeft = Diff.Compare<MouseLeft>(previous, state);
        var mouseRight = state[ENTITY].Get<MouseRight>();

        var playerId = state.Get<Player>().FirstOrDefault().Item1;
        var player = state[playerId];

        if (mouseRight?.Pressed == true)
        {
            var target = game.ToLocal(game.GetViewport().GetMousePosition());

            var position = player.Get<Position>();
            var destination = new Position { X = target.x, Y = target.y };
            if (position != destination)
            {
                state = state.With(playerId, new Move { Destination = destination });
            }
        }

        foreach (var (id, component) in mouseLeft.Added.Concat(mouseLeft.Changed))
        {
            if (component.Pressed)
            {
                state = state.With("projectile-" + Guid.NewGuid().ToString(), new Entity(
                   player.Get<Position>(),
                   new Sprite { Image = "res://resources/tiles/tile663.png" },
                   new Velocity { X = 5, Y = 0 },
                   new ExpirationEvent(new RemoveEntity()) with { Tick = Physics.MillisToTicks(2 * 1000) + tick }
                ));
            }
        }

        return state;
    }
}