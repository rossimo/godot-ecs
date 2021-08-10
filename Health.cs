using System;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

[Editor]
public struct Health
{
    public int Value;
}

[IsMany]
[Editor]
public struct HealthUpdate
{
    public int Delta;
}

public class HealthSystem : IEcsRunSystem
{
    [EcsWorld] readonly EcsWorld world = default;
    [EcsPool] readonly EcsPool<Health> healths = default;
    [EcsPool] readonly EcsPool<Delete> deletes = default;
    [EcsPool] readonly EcsPool<Many<HealthUpdate>> healthUpdates = default;

    public void Run(EcsSystems systems)
    {
        foreach (var entity in world.Filter<Health>().Inc<Many<HealthUpdate>>().End())
        {
            ref var health = ref healths.Get(entity);

            foreach (var update in healthUpdates.Get(entity))
            {
                health.Value += update.Delta;
            }

            if (health.Value <= 0)
            {
                deletes.Ensure(entity);
            }
        }

        foreach (var entity in world.Filter<Many<HealthUpdate>>().End())
        {
            healthUpdates.Del(entity);
        }
    }
}