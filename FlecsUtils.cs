using Flecs;

public static class FlecsUtils
{
    public static void RegisterSystem<TComponent1>(this World world, CallbackIterator callback, string? name = null)
    {
        world.RegisterSystem<TComponent1>(callback, world.EcsOnUpdate, name);
    }

    public static void RegisterSystem(this World world, CallbackIterator callback, string filterExpression, string? name = null)
    {
        world.RegisterSystem(callback, world.EcsOnUpdate, filterExpression, name);
    }
}
