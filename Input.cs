using Godot;
using Arch.Core;
using Arch.System;
using Arch.Core.Extensions;

[Editor]
public class Player
{

}

[Editor]
public class Move
{
    public float X;
    public float Y;
}

public class InputSystem : BaseSystem<World, Game>
{
    private QueryDescription inputEvents = new QueryDescription().WithAll<InputEventMouseButton>();
    private QueryDescription players = new QueryDescription().WithAll<Player>();

    public InputSystem(World world) : base(world) { }

    public override void Update(in Game data)
    {
        World.Query(inputEvents, (in Entity entity, ref InputEventMouseButton mouse) =>
        {
            if (mouse.ButtonIndex == MouseButton.Right && mouse.IsPressed())
            {
                var position = Data.ToLocal(Data.GetViewport().GetMousePosition());

                World.Query(players, (in Entity player) => player.Add(new Move
                {
                    X = position.X,
                    Y = position.Y
                }));
            }

            entity.Remove<InputEventMouseButton>();
        });
    }
}