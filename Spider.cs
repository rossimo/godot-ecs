public partial class Spider : Godot.Sprite2D
{
    public override void _Ready()
    {
        var game = this.GetParent() as Game;
        var world = game.world;

        this.RunEntityTask(world, Script);
    }

    public static async Task Script(Entity self, CancellationToken token)
    {
        var origin = self.Get<PhysicsNode>().Node.Position;
        var originX = origin.X;
        var originY = origin.Y;

        var random = new Random();

        while (true)
        {
            self.Set(new Timer()
            {
                RemainingTicks = PhysicsSystem.MillisToTicks(random.Within(3000))
            });

            await self.WhenAny(token)
                .Removed<Timer>()
                .Task();

            var theta = random.Within(2d * Math.PI);
            var radius = random.Within(100, 200);

            self.Set(new Move()
            {
                X = Convert.ToSingle(originX + radius * Math.Cos(theta)),
                Y = Convert.ToSingle(originY + radius * Math.Sin(theta)),
            });

            await self.WhenAny(token)
                .Removed<Move>()
                .Task();
        }
    }
}
