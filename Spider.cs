using System;
using System.Threading.Tasks;

using static EventLoop;
using static System.Threading.Tasks.Task;

public class Spider : Godot.Sprite
{
    public override void _Ready()
    {
        var game = this.GetParent() as Game;

        var thisCtx = new TaskContext();

        EventLoop.Run(async () =>
        {
            await Script(thisCtx, await this.AttachEntity(game.world));
            thisCtx.Cancel();
        });
    }

    public async Task Walk(TaskContext ctx, Entity entity)
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

        while (ctx.Running)
        {
            entity.Set(end);

            await WhenAny(ctx,
                entity.Removed<Move>(),
                entity.Added<Collision>());

            if (!ctx.Running) break;

            entity.Set(start);

            await WhenAny(ctx,
                entity.Removed<Move>(),
                entity.Added<Collision>());
        }
    }

    public async Task Script(TaskContext ctx, Entity entity)
    {
        var walk = Walk(ctx, entity);
        var dead = entity.Added<Delete>().Create(ctx);

        await WhenAny(walk, dead);
    }
}
