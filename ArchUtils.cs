using Godot;
using Arch;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;

public static class RelEcsUtils
{
    public static void SetEntity(this GodotObject obj, Entity entity)
    {

        obj.SetMeta($"entity{Utils.DELIMETER}id", entity.Id);
        obj.SetMeta($"entity{Utils.DELIMETER}world", entity.WorldId);

        if (!obj.HasUserSignal("entity"))
        {
            obj.AddUserSignal("entity");
        }

        obj.EmitSignal("entity", Array.Empty<Variant>());
    }

    public static void Update<T>(this Entity entity, T component)
    {
        if (entity.Has<T>())
        {
            entity.Set(component);
        }
        else
        {
            entity.Add(component);
        }
    }

    public static void Cleanup(this World world, Entity entity)
    {
        if (entity.GetComponentTypes().Length == 0)
        {
            world.Destroy(entity);
        }
    }
}