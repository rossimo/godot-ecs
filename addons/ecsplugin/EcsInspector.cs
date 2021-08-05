using Godot;
using System;

public class EcsInspector : EditorInspectorPlugin
{
    public override bool CanHandle(Godot.Object @object)
    {
        return true;
    }

    public override bool ParseProperty(Godot.Object @object, int type, string path, int hint, string hintText, int usage)
    {
        switch (type)
        {
            default:
                return false;
        }
    }
}
