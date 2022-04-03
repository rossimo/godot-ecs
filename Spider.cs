using System;
using System.Threading;
using System.Threading.Tasks;

using static System.Threading.Tasks.Task;

public class Spider : Godot.Sprite
{
    public override void _Ready()
    {
        var game = this.GetParent() as Game;

        Func<Task> task = async () =>
        {
            try
            {
                var entity = await this.AttachEntity(game.world);
                await Script(entity);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        };

        task();
    }

    public static async Task Script(Entity entity)
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

        while (true)
        {
            entity.Set(move2);
            await MoveOrCollide(entity);

            entity.Set(move1);
            await MoveOrCollide(entity);
        }
    }

    private static async Task<Task> MoveOrCollide(Entity entity)
    {
        var source = new CancellationTokenSource();

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
