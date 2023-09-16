using Godot;
using Flecs.NET.Core;

public struct Time
{
    public Time() { }
    public double Delta = 1 / Physics.PHYSICS_FPS;
    public float Scale = 1;
    public int Ticks = 0;
}

public struct Position
{
    public float X;
    public float Y;
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

[Editor]
public struct Move
{
    public float X;
    public float Y;
}

[Editor, IsEvent]
public struct Collision
{

}

public class Physics
{
    public static float PHYSICS_FPS = $"{ProjectSettings.GetSetting("physics/common/physics_ticks_per_second")}".ToFloat();
    public static float PHYSICS_SPEED_SCALE = 30f / PHYSICS_FPS;
    public static float PHYSICS_TARGET_FRAMETIME = 1 / PHYSICS_FPS;

    public static ulong MillisToTicks(double millis)
    {
        return Convert.ToUInt64(Convert.ToSingle(millis) / 1000f * PHYSICS_FPS);
    }

    public static Action System(World world)
    {
        var systems = new List<Action>() {
            SyncPhysics(world),
            Move(world),
            CleanupMove(world)
        };

        return () => systems.ForEach(system => system());
    }

    public static Action SyncPhysics(World world) =>
        world.System("SyncPhysics", (ref Position position, ref CharacterBody2D physics) =>
        {
            if (position.X != physics.Position.X || position.Y != physics.Position.Y)
            {
                physics.Position = new Vector2(position.X, position.Y);
            }
        });

    public static Action Move(World world) =>
        world.System("Move", (Entity entity, ref CharacterBody2D physics, ref Move move, ref Speed speed) =>
        {
            var timeScale = world.Get<Time>().Scale;

            var direction = physics.Position
                .DirectionTo(new Vector2(move.X, move.Y))
                .Normalized();

            var travel = direction * speed.Value * timeScale * PHYSICS_SPEED_SCALE;

            var remainingDistance = physics.Position.DistanceTo(new Vector2(move.X, move.Y));
            var travelDistance = physics.Position.DistanceTo(physics.Position + travel);

            if (remainingDistance < travelDistance)
            {
                travel = new Vector2(move.X, move.Y) - physics.Position;
            }

            var collision = physics.MoveAndCollide(travel);
            entity.Set(new Position { X = physics.Position.X, Y = physics.Position.Y });

            if (collision != null)
            {
                entity.Remove<Move>();

                var other = collision.GetCollider().GetEntity(world);
                if (other.IsValid())
                {
                    other.Remove<Move>();

                    if (other.Has<Event<Collision>>())
                    {
                        var ev = other.Get<Event<Collision>>();
                        var target = ev.Target.Resolve(other, entity);
                        target.DiscoverAndSet(ev.Component);
                    }

                    if (entity.Has<Event<Collision>>())
                    {
                        var ev = entity.Get<Event<Collision>>();
                        var target = ev.Target.Resolve(entity, other);
                        target.DiscoverAndSet(ev.Component);
                    }
                }
            }
        });

    public static Action CleanupMove(World world) =>
        world.System("CleanupMove", (Entity entity, ref Position position, ref Move move) =>
        {
            if (position.X == move.X && position.Y == move.Y)
            {
                entity.Remove<Move>();

                if (entity.Has<RenderNode>())
                {
                    ref var render = ref entity.GetMut<RenderNode>();

                    render.PositionTween?.Stop();
                    render.PositionTween = null;
                }
            }
        });
}