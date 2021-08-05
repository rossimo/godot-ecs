using Godot;
using System;

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
		AddControlToDock(DockSlot.RightUl, dock);
	}

	public override void _ExitTree()
	{
		RemoveInspectorPlugin(inspector);

		RemoveControlFromDocks(dock);
		dock.QueueFree();
	}
}
