using System.Threading.Tasks;
using System;
using Leopotam.EcsLite;
using System.Threading;
using System.Threading.Tasks;

using static Utils;

public class Spider : Godot.Sprite
{
    private Entity Entity;

    public override void _Ready()
    {
        var game = this.GetParent() as Game;

        Game.GodoTasks.Run(async () =>
        {
            await Script(await this.AttachEntity(game.world));
        });
    }

    public override void _ExitTree()
    {
        Entity?.Cancel();
    }

    public async Task Script(Entity entity)
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

            await When(
                entity.Removed<Move>(),
                entity.Added<Collision>());

            entity.Set(start);

            await When(
                entity.Removed<Move>(),
                entity.Added<Collision>());
        } while (true);
    }
}
