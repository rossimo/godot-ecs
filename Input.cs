using Godot;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

[EditorComponent]
public struct Player { }

public struct MouseLeft
{
    public bool Pressed;
}

public struct MouseRight
{
    public bool Pressed;
}

[EditorComponent]
public struct Move
{
    public Destination Destination;
}

public class InputSystem : IEcsInitSystem, IEcsRunSystem
{
    [EcsWorld] readonly EcsWorld world = default;
    [EcsShared] readonly Shared shared = default;
    [EcsPool] readonly EcsPool<MouseLeft> mouseLefts = default;
    [EcsPool] readonly EcsPool<MouseRight> mouseRights = default;
    [EcsPool] readonly EcsPool<Move> moves = default;
    [EcsPool] readonly EcsPool<Tick> ticks = default;
    [EcsPool] readonly EcsPool<PositionTween> positionTweens = default;
    [EcsPool] readonly EcsPool<Direction> directions = default;
    [EcsPool] readonly EcsPool<Speed> speeds = default;
    [EcsPool] readonly EcsPool<Expiration> expirations = default;
    [EcsPool] readonly EcsPool<Node2DComponent> node2ds = default;

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
        var game = shared.Game;
        var mousePosition = game.ToLocal(game.GetViewport().GetMousePosition());

        var mouseLeft = false;
        var mouseRight = false;

        var tick = ticks.Get(shared.Physics).Value;

        foreach (var entity in world.Filter<MouseLeft>().End())
        {
            mouseLeft |= mouseLefts.Get(entity).Pressed;
        }

        foreach (var entity in world.Filter<MouseRight>().End())
        {
            mouseRight |= mouseRights.Get(entity).Pressed;
        }

        foreach (var entity in world.Filter<Player>().Inc<Node2DComponent>().End())
        {
            if (mouseRight)
            {
                ref var move = ref moves.Ensure(entity);
                move.Destination.X = mousePosition.x;
                move.Destination.Y = mousePosition.y;
            }

            if (mouseLeft)
            {
                var playerNode = node2ds.Get(entity).Node;
                var directionVec = playerNode.Position
                    .DirectionTo(mousePosition)
                    .Normalized();

                var bullet = world.NewEntity();

                ref var node2dComponent = ref node2ds.Add(bullet);
                node2dComponent.Node = new Godot.Sprite()
                {
                    Texture = GD.Load<Texture>("res://resources/tiles/tile663.png"),
                    Position = playerNode.Position
                };
                game.AddChild(node2dComponent.Node);

                ref var positionTweenComponent = ref positionTweens.Add(bullet);
                positionTweenComponent.Tween = new Tween() { Name = "position" };
                node2dComponent.Node.AddChild(positionTweenComponent.Tween);

                ref var direction = ref directions.Add(bullet);
                direction.X = directionVec.x;
                direction.Y = directionVec.y;

                ref var speed = ref speeds.Add(bullet);
                speed.Value = 10f;

                ref var expiration = ref expirations.Add(bullet);
                expiration.Tick = PhysicsSystem.MillisToTicks(1 * 1000) + tick;
            }
        }
    }
}