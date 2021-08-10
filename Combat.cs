using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

[EditorComponent]
public struct Expiration
{
    public ulong Tick;
}

public class CombatSystem : IEcsRunSystem
{
    [EcsWorld] readonly EcsWorld world = default;
    [EcsShared] readonly Shared shared = default;
    [EcsPool] readonly EcsPool<Tick> ticks = default;
    [EcsPool] readonly EcsPool<Expiration> expirations = default;
    [EcsPool] readonly EcsPool<Delete> deletes = default;

    public void Run(EcsSystems systems)
    {
        ulong tick = ticks.Get(shared.Physics).Value;

        foreach (var entity in world.Filter<Expiration>().End())
        {
            ref var expiration = ref expirations.Get(entity);

            if (expiration.Tick <= tick)
            {
                deletes.Ensure(entity);
            }
        }
    }
}