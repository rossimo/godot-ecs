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
}