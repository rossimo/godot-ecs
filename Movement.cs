using Ecs;
using System;
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

            var entity = state[id];
            var move = entity.Get<Move>();
            if (move == null) continue;

            if (move.Position.X == node.Position.x && move.Position.Y == node.Position.y)
            {
                state = state.With(id, state[id].Without<Move>());
            }
        }

        return state;
    }
}