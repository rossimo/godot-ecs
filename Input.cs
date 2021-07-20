using DefaultEcs;
using Godot;

public record Player;

public record MouseLeft
{
    public bool Pressed;
}

public record MouseRight
{
    public bool Pressed;
}

public class InputEvents
{
    private DefaultEcs.World world;

    public InputEvents(DefaultEcs.World world)
    {
        this.world = world;
    }

    public void System(Game game, InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton mouseButton:
                {
                    if ((mouseButton.ButtonIndex & (int)ButtonList.MaskLeft) != 0)
                    {
                        world.Set(new MouseLeft
                        {
                            Pressed = mouseButton.IsPressed()
                        });
                    }
                    else if ((mouseButton.ButtonIndex & (int)ButtonList.MaskRight) != 0)
                    {
                        world.Set(new MouseRight
                        {
                            Pressed = mouseButton.IsPressed()
                        });
                    }
                }
                break;
        }
    }
}

public class InputMonitor
{
    private DefaultEcs.World world;
    private EntitySet players;

    public InputMonitor(DefaultEcs.World world)
    {
        this.world = world;
        players = world.GetEntities().With<Player>().AsSet();
    }

    public void System(Game game)
    {
        var mouseLeft = world.TryGet<MouseLeft>();
        var mouseRight = world.TryGet<MouseRight>();

        foreach (var entity in players.GetEntities())
        {
            var position = entity.TryGet<Position>();
            var mousePosition = game.ToLocal(game.GetViewport().GetMousePosition());

            if (mouseRight?.Pressed == true)
            {
                var destination = new Position { X = mousePosition.x, Y = mousePosition.y };
                if (position != destination)
                {
                    entity.Set(new Move { Destination = destination });
                }
            }

            if (mouseLeft?.Pressed == true)
            {
                var direction = new Vector2(position.X, position.Y).DirectionTo(mousePosition).Normalized() * 10f;
                if (direction.x != 0 && direction.y != 0)
                {
                    var tick = world.TryGet<Ticks>().Tick;

                    var particle = world.CreateEntity();
                    particle.Set(position);
                    particle.Set(new Sprite { Image = "res://resources/tiles/tile663.png" });
                    particle.Set(new Velocity { X = direction.x, Y = direction.y });
                    particle.Set(new LowRenderPriority());
                    particle.Set(new ExpirationEvent(new RemoveEntity()) with { Tick = Physics.MillisToTicks(500) + tick });
                }
            }
        }
    }
}