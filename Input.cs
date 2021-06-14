using Ecs;
using Godot;

public static class Input
{
    public static Ecs.State System(Ecs.State state, Game game, InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton mouseButton:
                {
                    if (mouseButton.IsPressed())
                    {
                        var target = game.ToLocal(mouseButton.Position);

                        foreach (var (id, player) in state.Get<Player>())
                        {
                            var position = state[id].Get<Position>();
                            var source = new Vector2(position.X, position.Y);
                            var velocity = source.DirectionTo(new Vector2(target));
                            state = state.With(id,
                                new Velocity(velocity.x, velocity.y),
                                new Move(Destination: new Position(target.x, target.y)));
                        }
                    }
                }
                break;
        }
        return state;
    }
}