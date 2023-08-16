using Godot;
using Arch.Core;
using Arch.System;
using Arch.Core.Extensions;

[Editor]
public struct Player
{

}

[Editor]
public struct Move
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
            if (mouse.IsPressed())
            {
                switch (mouse.ButtonIndex)
                {
                    case MouseButton.Left:
                        {
                            World.Query(players, (in Entity player) =>
                            {
                                player.Remove<Move>();
                                player.Update(new Position
                                {
                                    X = 0,
                                    Y = 0
                                });
                            });
                        }
                        break;

                    case MouseButton.Right:
                        {
                            var position = Data.ToLocal(Data.GetViewport().GetMousePosition());

                            World.Query(players, (in Entity player) => player.Update(new Move
                            {
                                X = position.X,
                                Y = position.Y
                            }));
                        }
                        break;
                }
            }

            entity.Remove<InputEventMouseButton>();
            World.Cleanup(entity);
        });
    }
}