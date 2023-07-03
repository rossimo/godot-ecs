using System;
using System.Threading;
using Leopotam.EcsLite;
using System.Threading.Tasks;

public partial class Spider : Godot.Sprite2D
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
        var originX = origin.X;
        var originY = origin.Y;

        var random = new Random();

        while (token.Running())
        {
            entity.Set(new Timer()
            {
                RemainingTicks = PhysicsSystem.MillisToTicks(random.Within(3000))
            });
            await entity.Removed<Timer>(token);

            while (token.Running())
            {
                var theta = random.Within(2d * Math.PI);
                var radius = random.Within(100, 200);

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
