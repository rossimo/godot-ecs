using Godot;
using RelEcs;

public struct Tick
{
    public ulong Value;
}

[Editor]
public class Speed
{
    public float Value;
}

[Editor]
public class Direction
{
    public float X;
    public float Y;
}

public class PhysicsSystem : ISystem
{
    public static float TARGET_PHYSICS_FPS = 30f;
    public static float PHYSICS_FPS = $"{ProjectSettings.GetSetting("physics/common/physics_fps")}".ToFloat();

    public static ulong MillisToTicks(ulong millis)
    {
        return Convert.ToUInt64(Convert.ToSingle(millis) / 1000f * PHYSICS_FPS);
    }

    public static ulong MillisToTicks(double millis)
    {
        return MillisToTicks(Convert.ToUInt64(millis));
    }

    public void Run(World world)
    {
        foreach (var (physics, move, speed) in world.Query<CharacterBody2D, Move, Speed>().Build())
        {
            var direction = physics.Position
                .DirectionTo(new Vector2(move.X, move.Y))
                .Normalized();

            var travel = direction * speed.Value;

            var moveDistance = physics.Position.DistanceTo(new Vector2(move.X, move.Y));
            var travelDistance = physics.Position.DistanceTo(physics.Position + travel);

            if (moveDistance < travelDistance)
            {
                travel = new Vector2(move.X, move.Y) - physics.Position;
            }

            physics.MoveAndCollide(travel);
        };

        foreach (var entity in world.Query().Has<CharacterBody2D>().Has<Move>().Build())
        {

            var physics = world.GetComponent<CharacterBody2D>(entity);
            var move = world.GetComponent<Move>(entity);

            var position = physics.Position;

            if (position.X == move.X && position.Y == move.Y)
            {
                world.RemoveComponent<Move>(entity);
            }
        };

        foreach (var (physics, render) in world.Query<CharacterBody2D, RenderNode>().Build())
        {
            if (physics.Position.X != render.Node.Position.X || physics.Position.Y != render.Node.Position.Y)
            {
                render.Node.Position = physics.Position;
            }
        };
    }
}