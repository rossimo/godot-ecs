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

public struct Reloading
{
    public ulong RemainingTicks;
}

public class InputSystem : IEcsInitSystem, IEcsRunSystem
{
    [EcsWorld] readonly EcsWorld world = default;
    [EcsShared] readonly Shared shared = default;
    [EcsPool] readonly EcsPool<MouseLeft> mouseLefts = default;
    [EcsPool] readonly EcsPool<MouseRight> mouseRights = default;
    [EcsPool] readonly EcsPool<Reloading> reloadings = default;
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
            move.X = position.X;
            move.Y = position.Y;
        }
    }

    public void Run(EcsSystems systems, InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton mouseButton:
                {
                    if (mouseButton.ButtonIndex == MouseButton.Left)
                    {
                        ref var mouseLeft = ref mouseLefts.Get(shared.Input);
                        mouseLeft.Pressed |= mouseButton.IsPressed();
                    }
                    else if (mouseButton.ButtonIndex == MouseButton.Right)
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

        foreach (var entity in world.Filter<Reloading>().End())
        {
            ref var reloading = ref reloadings.Get(entity);
            reloading.RemainingTicks--;

            if (reloading.RemainingTicks <= 0)
            {
                reloadings.Del(entity);
            }
        }

        foreach (var entity in world.Filter<Player>().Inc<PhysicsNode>().Exc<Reloading>().End())
        {
            if (mouseLeft.Pressed)
            {
                var playerNode = physicsNodes.Get(entity).Node;
                var directionVec = playerNode.Position
                    .DirectionTo(mousePosition)
                    .Normalized();

                var node = GD.Load<PackedScene>("res://bullet.tscn").Instantiate<Node2D>();
                node.Position = playerNode.Position;
                game.AddChild(node);

                var bullet = game.DiscoverEntity(node);

                ref var direction = ref directions.Add(bullet);
                direction.X = directionVec.X;
                direction.Y = directionVec.Y;

                ref var expiration = ref expirations.Add(bullet);
                expiration.Tick = PhysicsSystem.MillisToTicks(1 * 1000) + tick;

                ref var reloading = ref reloadings.Ensure(entity);
                reloading = new Reloading()
                {
                    RemainingTicks = PhysicsSystem.MillisToTicks(250)
                }; ;
            }
        }

        mouseLeft.Pressed = Input.IsMouseButtonPressed(MouseButton.Left);
        mouseRight.Pressed = Input.IsMouseButtonPressed(MouseButton.Right);

        if (mouseRight.Pressed)
        {
            QueueMove();
        }
    }
}