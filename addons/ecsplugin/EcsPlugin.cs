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

		dock = new Panel() { Name = "Components" };
		AddControlToDock(DockSlot.RightUl, dock);

		CreateComponentList();
	}

	public override void _ExitTree()
	{
		RemoveInspectorPlugin(inspector);

		RemoveControlFromDocks(dock);
		dock.QueueFree();
	}

	public void CreateComponentList()
	{
		foreach (Node child in dock.GetChildren())
		{
			dock.RemoveChild(child);
			child.QueueFree();
		}


		var layout = new VBoxContainer();
		layout.AnchorRight = 1;
		dock.AddChild(layout);

		foreach (var component in GetComponents())
		{
			var componentLayout = new HBoxContainer();
			componentLayout.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
			layout.AddChild(componentLayout);

			var componentLabel = new Label() { Text = component.Name };
			componentLabel.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
			componentLayout.AddChild(componentLabel);

			var fieldsLayout = new VBoxContainer();
			fieldsLayout.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
			componentLayout.AddChild(fieldsLayout);

			foreach (var field in component.GetFields())
			{
				var fieldLabel = new Label() { Text = field.Name };
				fieldsLayout.AddChild(fieldLabel);
			}
		}
	}

	public static IEnumerable<Type> GetComponents()
	{
		return AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(assembly => assembly.GetTypes())
			.Where(type => type.GetCustomAttributes(typeof(EditorComponent), false)?.Length > 0)
			.OrderBy(component => component.Name);
	}
}
