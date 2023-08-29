using Godot;
using Flecs.NET.Core;

[Editor]
public struct Player
{
    public int Test;
}

public class InputSystem
{
    public static Action Update(World world)
    {
        var players = world.Query(filter: world.FilterBuilder().Term<Player>());
        
        return world.System((Entity entity, ref InputEventMouseButton mouse) =>
        {
            if (mouse.IsPressed())
            {
                switch (mouse.ButtonIndex)
                {
                    case MouseButton.Left:
                        {
                            players.Iter(it =>
                            {
                                foreach (int i in it)
                                {
                                    var player = it.Entity(i);
                                    player.Remove<Move>();
                                    player.Set(new Position
                                    {
                                        X = 0,
                                        Y = 0
                                    });
                                }
                            });
                        }
                        break;

                    case MouseButton.Right:
                        {
                            var scene = world.Get<Game>();
                            var position = scene.ToLocal(scene.GetViewport().GetMousePosition());

                            players.Iter(it =>
                            {
                                foreach (int i in it)
                                {
                                    var player = it.Entity(i);
                                    player.Set(new Move
                                    {
                                        X = position.X,
                                        Y = position.Y
                                    });
                                }
                            });
                        }
                        break;
                }
            }

            entity.Remove<InputEventMouseButton>();
            entity.Cleanup();
        });
    }
}