using System;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

[Editor]
public struct Health
{
    public int Value;
}

[Queued]
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
    [EcsPool] readonly EcsPool<Queue<HealthUpdate>> healthUpdateQueue = default;

    public void Run(EcsSystems systems)
    {
        foreach (var entity in world.Filter<Health>().Inc<Queue<HealthUpdate>>().End())
        {
            ref var updates = ref healthUpdateQueue.Get(entity);
            ref var health = ref healths.Get(entity);

            foreach (var update in updates)
            {
                health.Value += update.Delta;
            }

            if (health.Value <= 0)
            {
                deletes.Ensure(entity);
            }
        }

        foreach (var entity in world.Filter<Queue<HealthUpdate>>().End())
        {
            healthUpdateQueue.Del(entity);
        }
    }
}