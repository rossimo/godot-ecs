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
    private EcsPool<Move> moves;

    public void Init(EcsSystems systems)
    {
        var world = systems.GetWorld();

        players = world.GetPool<Player>();
        moves = world.GetPool<Move>();
        mouseLefts = world.GetPool<MouseLeft>();
        mouseRights = world.GetPool<MouseRight>();

        var entity = world.NewEntity();
        mouseLefts.Update(entity);
        mouseRights.Update(entity);
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

        foreach (var entity in world.Filter<MouseLeft>().End())
        {
            mouseLeft |= mouseLefts.Get(entity).Pressed;
        }

        foreach (var entity in world.Filter<MouseRight>().End())
        {
            mouseRight |= mouseRights.Get(entity).Pressed;
        }

        if (mouseRight)
        {
            foreach (var entity in world.Filter<Player>().Inc<Position>().End())
            {
                ref var move = ref moves.Update(entity);
                move.Destination = new Position
                {
                    X = mousePosition.x,
                    Y = mousePosition.y
                };
            }
        }

        /*
        var tick = state.Get<Ticks>(Physics.ENTITY).Tick;
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