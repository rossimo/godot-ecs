public partial class Damage
{
    public static async Task Script(Entity self, CancellationToken token)
    {
        while (true)
        {
            self.SetAndNotify(new Flash()
            {
                Color = new Color()
                {
                    Blue = 0,
                    Green = 0,
                    Red = 1
                }
            });

            self.Set(new Timer()
            {
                RemainingTicks = PhysicsSystem.MillisToTicks(3000)
            });

            await self.WhenAny(token)
                .Removed<Timer>()
                .Task();
        }
    }
}
