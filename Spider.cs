using System;
using Leopotam.EcsLite;
using System.Threading;
using System.Threading.Tasks;

using static System.Threading.Tasks.Task;

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

        while (!token.IsCancellationRequested)
        {
            var delay = Convert.ToInt32(random.NextDouble() * 5000f);
            await Task.Delay(delay);
            if (token.IsCancellationRequested) break;

            while (!token.IsCancellationRequested)
            {
                var theta = random.NextDouble() * 2.0d * Math.PI;
                var radius = 100.0d + random.NextDouble() * 100.0d;

                entity.Set(new Move()
                {
                    X = Convert.ToSingle(originX + radius * Math.Cos(theta)),
                    Y = Convert.ToSingle(originY + radius * Math.Sin(theta)),
                });

                var task = await MovedOrCollided(entity, token);
                if (task is Task<(EcsPackedEntity, Move)>) break;
            }
        }
    }

    private static async Task<Task> MovedOrCollided(Entity entity, CancellationToken token)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(token);

        try
        {
            var result = await WhenAny(
                entity.Removed<Move>(source.Token),
                entity.Added<Collision>(source.Token));

            return result;
        }
        finally
        {
            source.Cancel();
        }
    }
}
