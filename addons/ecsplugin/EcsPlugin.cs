using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

[Tool]
public class EcsPlugin : EditorPlugin
{
    private EditorInspectorPlugin inspector;
    private Control dock;

    public override void _EnterTree()
    {
        inspector = GD.Load<CSharpScript>("res://addons/ecsplugin/EcsInspector.cs").New() as EditorInspectorPlugin;
        AddInspectorPlugin(inspector);

        dock = new Control() { Name = "Components" };

		var components = GetComponents();

		var componentList = new Godot.Label()
		{
			Text = String.Join("\n", components.Select(component => component.Name))
		};
		dock.AddChild(componentList);

        AddControlToDock(DockSlot.RightUl, dock);
    }

    public override void _ExitTree()
    {
        RemoveInspectorPlugin(inspector);

        RemoveControlFromDocks(dock);
        dock.QueueFree();
    }

    public static IEnumerable<Type> GetComponents()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.GetCustomAttributes(typeof(Component), false)?.Length > 0);
    }
}
