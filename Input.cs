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
                        var position = game.ToLocal(mouseButton.Position);

                        foreach (var (id, player) in state.Get<Player>())
                        {
                            state = state.With(id, new Move(Position: new Position(position.x, position.y), Speed: 200f));
                        }
                    }
                }
                break;
        }
        return state;
    }
}