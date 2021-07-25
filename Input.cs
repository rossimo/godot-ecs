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
    private EcsPool<Velocity> velocities;
    private EcsPool<Speed> speeds;

    public void Init(EcsSystems systems)
    {
        var world = systems.GetWorld();

        players = world.GetPool<Player>();
        moves = world.GetPool<Move>();
        mouseLefts = world.GetPool<MouseLeft>();
        mouseRights = world.GetPool<MouseRight>();
        ticks = world.GetPool<Ticks>();
        sprites = world.GetPool<Sprite>();
        velocities = world.GetPool<Velocity>();
        speeds = world.GetPool<Speed>();
        positions = world.GetPool<Position>();

        var entity = world.NewEntity();
        mouseLefts.AddOrReplace(entity);
        mouseRights.AddOrReplace(entity);
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
                ref var move = ref moves.AddOrReplace(entity);
                move.Destination = new Position
                {
                    X = mousePosition.x,
                    Y = mousePosition.y
                };
            }

            if (mouseLeft)
            {
                ref var position = ref positions.Get(entity);
                var direction = new Vector2(position.X, position.Y)
                    .DirectionTo(mousePosition)
                    .Normalized();

                var bullet = world.NewEntity();
                ref var sprite = ref sprites.AddEmit(world, bullet);
                sprite.Image = "res://resources/tiles/tile663.png";

                ref var bulletPosition = ref positions.Add(bullet);
                bulletPosition.X = position.X;
                bulletPosition.Y = position.Y;

                ref var velocity = ref velocities.Add(bullet);
                velocity.X = direction.x;
                velocity.Y = direction.y;

                ref var speed = ref speeds.Add(bullet);
                speed.Value = 10f;
            }
        }

        /*
        if (mouseLeft?.Pressed == true)
        {
            var direction = new Vector2(position.X, position.Y).DirectionTo(mousePosition).Normalized() * 10f;
            if (direction.x != 0 && direction.y != 0)
            {
                state = state.With(state.CreateEntityId(),
                    state.Get<Position>(playerId),
                    new Sprite { Image = "res://resources/tiles/tile663.png" },
                    new Velocity { X = direction.x, Y = direction.y },
                    // new LowRenderPriority(),
                    new ExpirationEvent(new RemoveEntity()) with { Tick = Physics.MillisToTicks(1 * 1000) + tick }
                );
            }
        }
        */
    }
}