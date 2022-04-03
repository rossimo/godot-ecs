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
            await Script(await this.AttachEntity(game.world));
        };

        task().ContinueWith(action =>
        {
            Console.WriteLine(action.Exception);
            return action;
        }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted);
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
        return await WhenAny(
            entity.Removed<Move>(),
            entity.Removed<Collision>());
    }
}
