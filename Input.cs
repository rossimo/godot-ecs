using Godot;
using Flecs.NET.Core;

[Editor]
public struct Player
{
    public int Index;
}

public struct MouseEvent
{
    public InputEventMouseButton mouse;
    public Vector2 position;
}

public class Input
{
    public static Action System(World world)
    {
        var systems = new List<Action>() {
            Update(world),
        };

        return () => systems.ForEach(system => system());
    }

    public static Action Update(World world)
    {
        var players = world.Query(filter: world.FilterBuilder().Term<Player>());

        return world.System("Input.Update", (Entity entity, ref MouseEvent @event) =>
        {
            if (@event.mouse.IsPressed())
            {
                switch (@event.mouse.ButtonIndex)
                {
                    case MouseButton.Left:
                        {
                            players.Each(player =>
                            {
                                player.Remove<Move>();
                                player.Set(new Position
                                {
                                    X = 0,
                                    Y = 0
                                });
                            });
                        }
                        break;

                    case MouseButton.Right:
                        {
                            var scene = world.Get<Game>();
                            var position = @event.position;

                            players.Each(player =>
                            {
                                player.Set(new Move
                                {
                                    X = position.X,
                                    Y = position.Y
                                });
                            });
                        }
                        break;
                }
            }

            entity.Remove<MouseEvent>();
            entity.Cleanup();
        });
    }
}