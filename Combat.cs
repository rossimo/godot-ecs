using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

public struct Expiration
{
    public ulong Tick;
}

public class Combat : IEcsRunSystem
{
    [EcsWorld] readonly EcsWorld world = default;
    [EcsPool] readonly EcsPool<Ticks> ticks = default;
    [EcsPool] readonly EcsPool<Expiration> expirations = default;
    [EcsPool] readonly EcsPool<Delete> deletes = default;

    public void Run(EcsSystems systems)
    {
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