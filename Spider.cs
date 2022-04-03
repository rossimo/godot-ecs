using System;
using System.Threading;
using System.Threading.Tasks;

using static System.Threading.Tasks.Task;

public class Spider : Godot.Sprite
{
    public override void _Ready()
    {
        var game = this.GetParent() as Game;
        var world = game.world;

        this.Run(world, Script);
    }

    public static async Task Script(Entity entity, CancellationToken token)
    {
        var position = entity.Get<PhysicsNode>().Node.Position;

        var move1 = new Move
        {
            X = position.x,
            Y = position.y
        };

        var move2 = new Move
        {
            X = position.x - 300,
            Y = position.y
        };

        while (!token.IsCancellationRequested)
        {
            entity.Set(move2);
            await MoveOrCollide(entity, token);

            if (token.IsCancellationRequested) break;

            entity.Set(move1);
            await MoveOrCollide(entity, token);
        }
    }

    private static async Task<Task> MoveOrCollide(Entity entity, CancellationToken token)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(token);

        try
        {
            var result = await WhenAny(
                entity.Removed<Move>(source.Token),
                entity.Removed<Collision>(source.Token));

            return result;
        }
        finally
        {
            source.Cancel();
        }
    }
}
