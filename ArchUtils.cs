using Godot;
using Arch.Core;

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
}