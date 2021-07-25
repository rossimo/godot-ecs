using Leopotam.EcsLite;

public struct Expiration
{
    public ulong Tick;
}

public class Combat : IEcsRunSystem, IEcsInitSystem
{
    private EcsPool<Ticks> ticks;
    private EcsPool<Expiration> expirations;
    private EcsPool<Delete> deletes;

    public void Init(EcsSystems systems)
    {
        var world = systems.GetWorld();

        ticks = world.GetPool<Ticks>();
        expirations = world.GetPool<Expiration>();
        deletes = world.GetPool<Delete>();
    }

    public void Run(EcsSystems systems)
    {
        var world = systems.GetWorld();

        ulong tick = 0;

        foreach (var entity in world.Filter<Ticks>().End())
        {
            tick = ticks.Get(entity).Tick;
        }

        foreach (var entity in world.Filter<Expiration>().End())
        {
            ref var expiration = ref expirations.Get(entity);

            if (expiration.Tick <= tick)
            {
                deletes.Add(entity);
            }
        }
    }
}