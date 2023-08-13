using Godot;
using RelEcs;

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

public class InputSystem : ISystem
{
    public void Run(World world)
    {
        var game = world.GetElement<Game>();

        foreach (var ev in world.Receive<InputEvent>(this))
        {
            switch (ev)
            {
                case InputEventMouseButton mouseButton:
                    {
                        if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.IsPressed())
                        {
                            var position = game.ToLocal(game.GetViewport().GetMousePosition());

                            foreach (var player in world.Query().Has<Player>().Build())
                            {
                                world.UpdateComponent<Move>(player, new Move
                                {
                                    X = position.X,
                                    Y = position.Y
                                });
                            }
                        }
                    }
                    break;
            }
        }

    }
}