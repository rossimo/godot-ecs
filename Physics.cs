using Godot;
using Arch.Core;
using Arch.System;
using Arch.Core.Extensions;

public struct FrameTime
{
    public double Value;
}

public struct Tick
{
    public ulong Value;
}

[Editor]
public struct Speed
{
    public float Value;
}

[Editor]
public struct Direction
{
    public float X;
    public float Y;
}

public class PhysicsSystem : BaseSystem<World, Game>
{
    public static float TARGET_PHYSICS_FPS = 30f;
    public static float PHYSICS_FPS = $"{ProjectSettings.GetSetting("physics/common/physics_ticks_per_second")}".ToFloat();
    public static float PHYSICS_RATIO = (1 / PHYSICS_FPS) / (1 / TARGET_PHYSICS_FPS);

    public PhysicsSystem(World world) : base(world) { }

    public static ulong MillisToTicks(double millis)
    {
        return Convert.ToUInt64(Convert.ToSingle(millis) / 1000f * PHYSICS_FPS);
    }

    private QueryDescription step = new QueryDescription().WithAll<CharacterBody2D, Move, Speed>();
    private QueryDescription checkMove = new QueryDescription().WithAll<CharacterBody2D, Move>();
    private QueryDescription syncRender = new QueryDescription().WithAll<CharacterBody2D, RenderNode>();

    public override void Update(in Game data)
    {
        World.Query(step, (ref CharacterBody2D physics, ref Move move, ref Speed speed) => Step(ref physics, ref move, ref speed));
        World.Query(checkMove, (in Entity entity, ref CharacterBody2D physics, ref Move move) => CheckMove(in entity, ref physics, ref move));
        World.Query(syncRender, (ref CharacterBody2D physics, ref RenderNode render) => SyncRender(ref physics, ref render));
    }

    public void Step(ref CharacterBody2D physics, ref Move move, ref Speed speed)
    {
        var frameTime = Data.Global.Get<FrameTime>().Value;

        var timeScale = (float)(PHYSICS_RATIO * (frameTime * PHYSICS_FPS));

        var direction = physics.Position
            .DirectionTo(new Vector2(move.X, move.Y))
            .Normalized();

        var travel = direction * speed.Value * timeScale;

        var moveDistance = physics.Position.DistanceTo(new Vector2(move.X, move.Y));
        var travelDistance = physics.Position.DistanceTo(physics.Position + travel);

        if (moveDistance < travelDistance)
        {
            travel = new Vector2(move.X, move.Y) - physics.Position;
        }

        physics.MoveAndCollide(travel);
    }

    public void CheckMove(in Entity entity, ref CharacterBody2D physics, ref Move move)
    {
        var position = physics.Position;

        if (position.X == move.X && position.Y == move.Y)
        {
            entity.Remove<Move>();
        }
    }

    public void SyncRender(ref CharacterBody2D physics, ref RenderNode render)
    {
        if (physics.Position.X != render.Node.Position.X || physics.Position.Y != render.Node.Position.Y)
        {
            render.Node.Position = physics.Position;
        }
    }
}