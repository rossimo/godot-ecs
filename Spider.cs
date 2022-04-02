using System;
using System.Threading.Tasks;

using static EventLoop;
using static System.Threading.Tasks.Task;

public class Spider : Godot.Sprite
{
    private bool Running = true;

    public override void _Ready()
    {
        var game = this.GetParent() as Game;

        EventLoop.Run(async () =>
        {
            await Script(await this.AttachEntity(game.world));
        });
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        Running = false;
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

        while (Running)
        {
            entity.Set(end);

            await WhenAny(
                entity.Removed<Move>(),
                entity.Added<Collision>());

            if (!Running) break;

            entity.Set(start);

            await WhenAny(
                entity.Removed<Move>(),
                entity.Added<Collision>());
        }
    }

    public async Task Script(Entity entity)
    {
        var walk = Walk(entity);
        var (cleanup, dead) = entity.Added<Delete>().Task();

        await WhenAny(walk, dead);
        cleanup();
    }
}
