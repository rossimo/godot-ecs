using Godot;
using Flecs.NET.Core;

public struct Time
{
    public Time() { }
    public double Delta = 1 / PhysicsSystem.PHYSICS_FPS;
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

public class PhysicsSystem
{
    public static float TARGET_PHYSICS_FPS = 30f;
    public static float PHYSICS_FPS = $"{ProjectSettings.GetSetting("physics/common/physics_ticks_per_second")}".ToFloat();
    public static float PHYSICS_RATIO = 1 / PHYSICS_FPS / (1 / TARGET_PHYSICS_FPS);

    public static ulong MillisToTicks(double millis)
    {
        return Convert.ToUInt64(Convert.ToSingle(millis) / 1000f * PHYSICS_FPS);
    }

    public static Action SyncPhysics(World world)
    {
        return world.System((ref Position position, ref CharacterBody2D physics) =>
        {
            if (position.X != physics.Position.X || position.Y != physics.Position.Y)
            {
                physics.Position = new Vector2(position.X, position.Y);
            }
        });
    }

    public static Action Move(World world)
    {
        return world.System((Entity entity, ref CharacterBody2D physics, ref Move move, ref Speed speed) =>
        {
            var timeScale = world.Get<Time>().Scale;

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

            entity.Set(new Position { X = physics.Position.X, Y = physics.Position.Y });
        });
    }

    public static Action SyncRender(World world)
    {
        return world.System((Entity entity, ref Position position, ref RenderNode render) =>
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
        });
    }

    public static Action CleanupMove(World world)
    {
        return world.System((Entity entity, ref Position position, ref Move move) =>
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
}