using Godot;
using System;
using System.Linq;
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

public class InputEvents : IEcsInitSystem
{
    private EcsPool<MouseLeft> mouseLefts;
    private EcsPool<MouseRight> mouseRights;

    public void Init(EcsSystems systems)
    {
        var world = systems.GetWorld();

        var entity = world.NewEntity();

        mouseLefts = world.GetPool<MouseLeft>();
        mouseRights = world.GetPool<MouseRight>();

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
}

public class InputMonitor : IEcsRunSystem
{
    public void Run(EcsSystems systems)
    {
        var mouseLeft = false;
        var mouseRight = false;

        var playerId = state.Get<Player>().FirstOrDefault().Key;

        var position = state.Get<Position>(playerId);
        var mousePosition = game.ToLocal(game.GetViewport().GetMousePosition());

        if (mouseRight?.Pressed == true)
        {
            var destination = new Position { X = mousePosition.x, Y = mousePosition.y };
            if (position != destination)
            {
                state = state.With(playerId, new Move { Destination = destination });
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
        return state;
    }
}