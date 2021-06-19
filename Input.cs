using Ecs;
using Godot;
using System.Linq;

public record Player() : Component;

public record Move() : Component
{
    public Position Destination;
}

public static class Input
{
    public static (string id, Event Event)[] System(Ecs.State state, Game game, InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton mouseButton:
                {
                    if (mouseButton.IsPressed())
                    {
                        var target = game.ToLocal(mouseButton.Position);

                        return state.Get<Player>().Select(entry =>
                        {
                            var (id, player) = entry;

                            var position = state[id].Get<Position>();
                            var source = new Vector2(position.X, position.Y);
                            var velocity = source.DirectionTo(new Vector2(target));
                            return (id, new Event(new Add(new Move() { Destination = new Position { X = target.x, Y = target.y } })));
                        }).ToArray();
                    }
                }
                break;
        }

        return new (string id, Event Event)[] { };
    }
}