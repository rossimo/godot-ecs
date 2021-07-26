using Godot;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

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
    [EcsWorld] readonly EcsWorld world = default;
    [EcsShared] readonly Game game = default;
    [EcsPool] readonly EcsPool<MouseLeft> mouseLefts = default;
    [EcsPool] readonly EcsPool<MouseRight> mouseRights = default;
    [EcsPool] readonly EcsPool<Position> positions = default;
    [EcsPool] readonly EcsPool<Move> moves = default;
    [EcsPool] readonly EcsPool<Ticks> ticks = default;
    [EcsPool] readonly EcsPool<Sprite> sprites = default;
    [EcsPool] readonly EcsPool<Direction> directions = default;
    [EcsPool] readonly EcsPool<Speed> speeds = default;
    [EcsPool] readonly EcsPool<Expiration> expirations = default;
    [EcsPool] readonly EcsPool<LowRenderPriority> lowPriorities = default;

    public void Init(EcsSystems systems)
    {
        var world = systems.GetWorld();

        var entity = world.NewEntity();
        mouseLefts.Add(entity);
        mouseRights.Add(entity);
    }

    public void Run(EcsSystems systems, InputEvent @event)
    {
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
                ref var sprite = ref sprites.AddNotify(bullet);
                sprite.Image = "res://resources/tiles/tile663.png";

                ref var position = ref positions.AddNotify(bullet);
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