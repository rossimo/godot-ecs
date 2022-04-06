using System;
using System.Threading;
using Leopotam.EcsLite;
using System.Threading.Tasks;

public class Spider : Godot.Sprite
{
    public override void _Ready()
    {
        var game = this.GetParent() as Game;
        var world = game.world;

        this.RunEntityTask(world, Script);
    }

    public static async Task Script(Entity entity, CancellationToken token)
    {
        var origin = entity.Get<PhysicsNode>().Node.Position;
        var originX = origin.x;
        var originY = origin.y;

        var random = new Random();

        while (token.Running())
        {
            entity.Set(new Timer()
            {
                RemainingTicks = PhysicsSystem.MillisToTicks(Convert.ToUInt64(random.NextDouble() * 3000d))
            });
            await entity.Removed<Timer>(token);

            while (token.Running())
            {
                var theta = random.NextDouble() * 2.0d * Math.PI;
                var radius = 100.0d + random.NextDouble() * 100.0d;

                entity.Set(new Move()
                {
                    X = Convert.ToSingle(originX + radius * Math.Cos(theta)),
                    Y = Convert.ToSingle(originY + radius * Math.Sin(theta)),
                });

                var complete = await entity.WhenAny(token)
                    .Added<Collision>()
                    .Removed<Move>()
                    .Task();

                if (complete is Task<Move>) break;
            }
        }
    }
}
