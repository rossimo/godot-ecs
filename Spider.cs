using System;
using System.Threading;
using System.Threading.Tasks;

using static Utils;

public class Spider : Godot.Sprite
{
    private Entity Entity;

    public override void _Ready()
    {
        var game = this.GetParent() as Game;

        Game.GodotTasks.Run(async () =>
        {
            await Script(await this.AttachEntity(game.world));
        });
    }

    public async Task Walk(Entity entity)
    {
        var position = entity.Get<PhysicsNode>().Node.Position;

        var start = new Move
        {
            X = position.x,
            Y = position.y
        };

        var end = new Move
        {
            X = position.x - 300,
            Y = position.y
        };

        do
        {
            entity.Set(end);

            await UntilAny(
                entity.Removed<Move>(),
                entity.Added<Collision>());

            entity.Set(start);


            await UntilAny(
                entity.Removed<Move>(),
                entity.Added<Collision>());
        } while (true);
    }

    public async Task Script(Entity entity)
    {
        var walk = Walk(entity);
        var dead = entity.Added<Delete>();

        var result = await UntilAny(walk, dead);
        if (result == dead)
        {
            Console.WriteLine("dead");
        }
    }
}
