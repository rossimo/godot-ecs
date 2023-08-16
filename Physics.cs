using Godot;
using Arch.Core;
using Arch.System;
using Arch.Core.Extensions;

public struct Time
{
    public Time() { }
    public double Delta = 1 / PhysicsSystem.PHYSICS_FPS;
    public float Scale = 1;
    public int Ticks = 0;
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

public struct Position
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

    private QueryDescription syncPhysics = new QueryDescription().WithAll<Position, CharacterBody2D>();
    private QueryDescription step = new QueryDescription().WithAll<CharacterBody2D, Move, Speed>();
    private QueryDescription checkMove = new QueryDescription().WithAll<Position, Move>();
    private QueryDescription syncRender = new QueryDescription().WithAll<Position, RenderNode>();

    public override void Update(in Game data)
    {
        World.Query(syncPhysics, (ref Position position, ref CharacterBody2D physics) =>
            SyncPhysics(ref position, ref physics));
        World.Query(step, (in Entity entity, ref CharacterBody2D physics, ref Move move, ref Speed speed) =>
            Move(in entity, ref physics, ref move, ref speed));
        World.Query(syncRender, (in Entity entity, ref Position physics, ref RenderNode render) =>
            SyncRender(in entity, ref physics, ref render));
        World.Query(checkMove, (in Entity entity, ref Position position, ref Move move) =>
            CleanupMove(in entity, ref position, ref move));
    }


    public void SyncPhysics(ref Position position, ref CharacterBody2D physics)
    {
        if (position.X != physics.Position.X || position.Y != physics.Position.Y)
        {
            physics.Position = new Vector2(position.X, position.Y);
        }
    }

    public void Move(in Entity entity, ref CharacterBody2D physics, ref Move move, ref Speed speed)
    {
        var timeScale = Data.Global.Get<Time>().Scale;

        var direction = physics.Position
            .DirectionTo(new Vector2(move.X, move.Y))
            .Normalized();

        var travel = direction * speed.Value * timeScale;

        var remainingDistance = physics.Position.DistanceTo(new Vector2(move.X, move.Y));
        var travelDistance = physics.Position.DistanceTo(physics.Position + travel);

        if (remainingDistance < travelDistance)
        {
            travel = new Vector2(move.X, move.Y) - physics.Position;
        }

        physics.MoveAndCollide(travel);

        entity.Update(new Position { X = physics.Position.X, Y = physics.Position.Y });
    }

    public void SyncRender(in Entity entity, ref Position position, ref RenderNode render)
    {
        if (position.X != render.Node.Position.X || position.Y != render.Node.Position.Y)
        {
            render.PositionTween?.Stop();
            if (entity.Has<Move>())
            {
                render.PositionTween = render.Node.CreateTween();
                render.PositionTween.TweenProperty(render.Node, "position", new Vector2(position.X, position.Y), 1 / PHYSICS_FPS);
            }
            else
            {
                render.Node.Position = new Vector2(position.X, position.Y);
            }
        }
    }

    public void CleanupMove(in Entity entity, ref Position position, ref Move move)
    {
        if (position.X == move.X && position.Y == move.Y)
        {
            entity.Remove<Move>();
        }
    }
}