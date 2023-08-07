using Flecs;

[Editor]
public struct Expiration : IComponent
{
    public ulong Tick;
}

public class CombatSystem
{
    private Flecs.Entity tickEntity;

    public CombatSystem(World world)
    {
        tickEntity = world.EntityIterator<Tick>().Entity(0);
    }

    public void Run(Iterator iterator)
    {
        var tick = tickEntity.GetComponent<Tick>().Value;

        for (var i = 0; i < iterator.Count; i++)
        {
            var entity = iterator.Entity(i);
            var expiration = entity.GetComponent<Expiration>();

            if (expiration.Tick <= tick)
            {
                entity.Add<Delete>();
            }
        }
    }
}