using Godot;
using RelEcs;

public static class RelEcsUtils
{
    public static void SetEntity(this GodotObject obj, Entity entity)
    {
        var packed = entity.Identity;
        obj.SetMeta($"entity{Utils.DELIMETER}id", packed.Id);
        obj.SetMeta($"entity{Utils.DELIMETER}gen", packed.Generation);

        if (!obj.HasUserSignal("entity"))
        {
            obj.AddUserSignal("entity");
        }

        obj.EmitSignal("entity", Array.Empty<Variant>());
    }

    public static void UpdateComponent<T>(this World world, Entity entity, T component) where T : class
    {
        if (world.HasComponent<T>(entity))
        {
            world.RemoveComponent<T>(entity);
        }

        world.AddComponent(entity, component);
    }
}