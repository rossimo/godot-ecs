using Godot;
using Leopotam.EcsLite;

public struct Player { }

public struct MouseLeft
{
    public bool Pressed;
}

public struct MouseRight
{
    public bool Pressed;
}

public struct Move
{
    public Position Destination;
}

public class Input : IEcsInitSystem, IEcsRunSystem
{
    private EcsPool<MouseLeft> mouseLefts;
    private EcsPool<MouseRight> mouseRights;
    private EcsPool<Player> players;
    private EcsPool<Position> positions;
    private EcsPool<Move> moves;
    private EcsPool<Ticks> ticks;
    private EcsPool<Sprite> sprites;
    private EcsPool<Direction> directions;
    private EcsPool<Speed> speeds;
    private EcsPool<Expiration> expirations;
    private EcsPool<LowRenderPriority> lowPriorities;

    public void Init(EcsSystems systems)
    {
        var world = systems.GetWorld();

        players = world.GetPool<Player>();
        moves = world.GetPool<Move>();
        mouseLefts = world.GetPool<MouseLeft>();
        mouseRights = world.GetPool<MouseRight>();
        ticks = world.GetPool<Ticks>();
        sprites = world.GetPool<Sprite>();
        directions = world.GetPool<Direction>();
        speeds = world.GetPool<Speed>();
        positions = world.GetPool<Position>();
        expirations = world.GetPool<Expiration>();
        lowPriorities = world.GetPool<LowRenderPriority>();

        var entity = world.NewEntity();
        mouseLefts.Add(entity);
        mouseRights.Add(entity);
    }

    public void Run(EcsSystems systems, InputEvent @event)
    {
        var world = systems.GetWorld();
        var game = systems.GetShared<Game>();

        switch (@event)
        {
            case InputEventMouseButton mouseButton:
                {
                    if ((mouseButton.ButtonIndex & (int)ButtonList.MaskLeft) != 0)
                    {
                        foreach (var entity in world.Filter<MouseLeft>().End())
                        {
                            ref var mouseLeft = ref mouseLefts.Get(entity);
                            mouseLeft.Pressed = mouseButton.IsPressed();
                        }
                    }
                    else if ((mouseButton.ButtonIndex & (int)ButtonList.MaskRight) != 0)
                    {
                        foreach (var entity in world.Filter<MouseRight>().End())
                        {
                            ref var mouseRight = ref mouseRights.Get(entity);
                            mouseRight.Pressed = mouseButton.IsPressed();
                        }
                    }
                }
                break;
        }
    }

    public void Run(EcsSystems systems)
    {
        var world = systems.GetWorld();
        var game = systems.GetShared<Game>();
        var mousePosition = game.ToLocal(game.GetViewport().GetMousePosition());

        var mouseLeft = false;
        var mouseRight = false;
        ulong tick = 0;

        foreach (var entity in world.Filter<Ticks>().End())
        {
            tick = ticks.Get(entity).Tick;
        }

        foreach (var entity in world.Filter<MouseLeft>().End())
        {
            mouseLeft |= mouseLefts.Get(entity).Pressed;
        }

        foreach (var entity in world.Filter<MouseRight>().End())
        {
            mouseRight |= mouseRights.Get(entity).Pressed;
        }

        foreach (var entity in world.Filter<Player>().Inc<Position>().End())
        {
            if (mouseRight)
            {
                ref var move = ref moves.Replace(entity);
                move.Destination = new Position
                {
                    X = mousePosition.x,
                    Y = mousePosition.y
                };
            }

            if (mouseLeft)
            {
                ref var playerPosition = ref positions.Get(entity);
                var directionVec = new Vector2(playerPosition.X, playerPosition.Y)
                    .DirectionTo(mousePosition)
                    .Normalized();

                var bullet = world.NewEntity();
                ref var sprite = ref sprites.AddPublish(bullet);
                sprite.Image = "res://resources/tiles/tile663.png";

                ref var position = ref positions.AddPublish(bullet);
                position.X = playerPosition.X;
                position.Y = playerPosition.Y;

                ref var direction = ref directions.Add(bullet);
                direction.X = directionVec.x;
                direction.Y = directionVec.y;

                ref var speed = ref speeds.Add(bullet);
                speed.Value = 10f;

                ref var expiration = ref expirations.Add(bullet);
                expiration.Tick = Physics.MillisToTicks(1 * 1000) + tick;

                lowPriorities.Add(bullet);
            }
        }
    }
}