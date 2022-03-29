using Godot;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

[Editor]
public struct Player { }

public struct MouseLeft
{
    public bool Pressed;
}

public struct MouseRight
{
    public bool Pressed;
}

[Editor]
public struct Move
{
    public float X;
    public float Y;
}

public class InputSystem : IEcsInitSystem, IEcsRunSystem
{
    [EcsWorld] readonly EcsWorld world = default;
    [EcsShared] readonly Shared shared = default;
    [EcsPool] readonly EcsPool<MouseLeft> mouseLefts = default;
    [EcsPool] readonly EcsPool<MouseRight> mouseRights = default;
    [EcsPool] readonly EcsPool<Move> moves = default;
    [EcsPool] readonly EcsPool<Tick> ticks = default;
    [EcsPool] readonly EcsPool<Direction> directions = default;
    [EcsPool] readonly EcsPool<Expiration> expirations = default;
    [EcsPool] readonly EcsPool<PhysicsNode> physicsNodes = default;

    public void Init(EcsSystems systems)
    {
        var world = systems.GetWorld();
        var shared = systems.GetShared<Shared>();

        shared.Input = world.NewEntity();
        mouseLefts.Add(shared.Input);
        mouseRights.Add(shared.Input);
    }

    public void QueueMove()
    {
        var game = shared.Game;
        var position = game.ToLocal(game.GetViewport().GetMousePosition());

        foreach (var entity in world.Filter<Player>().Inc<RenderNode>().End())
        {
            ref var move = ref moves.Ensure(entity);
            move.X = position.x;
            move.Y = position.y;
        }
    }

    public void Run(EcsSystems systems, InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton mouseButton:
                {
                    if ((mouseButton.ButtonIndex & (int)ButtonList.MaskLeft) != 0)
                    {
                        ref var mouseLeft = ref mouseLefts.Get(shared.Input);
                        mouseLeft.Pressed |= mouseButton.IsPressed();
                    }
                    else if ((mouseButton.ButtonIndex & (int)ButtonList.MaskRight) != 0)
                    {
                        ref var mouseRight = ref mouseRights.Get(shared.Input);
                        mouseRight.Pressed |= mouseButton.IsPressed();

                        if (mouseRight.Pressed)
                        {
                            QueueMove();
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

        ref var mouseLeft = ref mouseLefts.Get(shared.Input);
        ref var mouseRight = ref mouseRights.Get(shared.Input);

        var tick = ticks.Get(shared.Physics).Value;

        foreach (var entity in world.Filter<Player>().Inc<PhysicsNode>().End())
        {
            if (mouseLeft.Pressed)
            {
                var playerNode = physicsNodes.Get(entity).Node;
                var directionVec = playerNode.Position
                    .DirectionTo(mousePosition)
                    .Normalized();

                var node = GD.Load<PackedScene>("res://bullet.tscn").Instance<Node2D>();
                node.Position = playerNode.Position;
                game.AddChild(node);

                var bullet = game.DiscoverEntity(node);

                ref var direction = ref directions.Add(bullet);
                direction.X = directionVec.x;
                direction.Y = directionVec.y;

                ref var expiration = ref expirations.Add(bullet);
                expiration.Tick = PhysicsSystem.MillisToTicks(1 * 1000) + tick;
            }
        }

        mouseLeft.Pressed = Input.IsMouseButtonPressed((int)ButtonList.MaskLeft);
        mouseRight.Pressed = Input.IsMouseButtonPressed((int)ButtonList.MaskRight);

        if (mouseRight.Pressed)
        {
            QueueMove();
        }
    }
}