using Ecs;
using Godot;

public static class Movement
{
    public static State System(State state, Game game)
    {
        foreach (var (id, position) in state.Get<Position>())
        {
            var node = game.GetNodeOrNull<Node2D>(id);
            if (node == null) continue;

            if (position.X != node.Position.x || position.Y != node.Position.y)
            {
                state = state.With(id, new Position(node.Position.x, node.Position.y, true));
            }
        }

        return state;
    }
}