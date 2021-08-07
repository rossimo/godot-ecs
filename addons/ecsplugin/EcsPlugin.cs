using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

[Tool]
public class EcsPlugin : EditorPlugin
{
	private Control dock;
	private Node current;

	public override void _EnterTree()
	{
		dock = new Panel() { Name = "Components" };
		AddControlToDock(DockSlot.RightUl, dock);

		RenderComponents();

		var selector = GetEditorInterface().GetSelection();
		selector.Connect("selection_changed", this, nameof(SelectedNode));

		current = selector.GetSelectedNodes().ToArray<Node>()?.FirstOrDefault();
	}

	public override void _ExitTree()
	{
		RemoveControlFromDocks(dock);
		dock.QueueFree();
	}

	public void SelectedNode()
	{
		var selector = GetEditorInterface().GetSelection();
		current = selector.GetSelectedNodes().ToArray<Node>()?.FirstOrDefault();

		RenderComponents();
	}

	public void RenderComponents()
	{
		foreach (Node child in dock.GetChildren())
		{
			dock.RemoveChild(child);
			child.QueueFree();
		}

		var layout = new VBoxContainer()
		{
			AnchorRight = 1
		};
		dock.AddChild(layout);

		if (current == null) return;

		foreach (var component in current.ToComponents().OrderBy(el => el.GetType().Name))
		{
			var type = component.GetType();
			var componentLayout = new HBoxContainer()
			{
				SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill
			};
			layout.AddChild(componentLayout);

			componentLayout.AddChild(new Label()
			{
				Text = type.Name,
				SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
				SizeFlagsVertical = 0
			});

			var fieldsLayout = new VBoxContainer()
			{
				SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
				SizeFlagsVertical = 0
			};
			componentLayout.AddChild(fieldsLayout);

			foreach (var fieldInfo in type.GetFields())
			{
				var fieldLayout = new HBoxContainer();
				fieldsLayout.AddChild(fieldLayout);

				fieldLayout.AddChild(new Label()
				{
					Text = fieldInfo.Name
				});

				fieldLayout.AddChild(new LineEdit()
				{
					SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
					Text = $"{fieldInfo.GetValue(component)}"
				});
			}
		}

		var picker = new OptionButton() { Text = "Add Component" };
		layout.AddChild(picker);
		picker.Connect("item_selected", this, nameof(AddComponent));

		for (var i = 0; i < Utils.COMPONENTS.Count; i++)
		{
			var componentType = Utils.COMPONENTS[i];
			picker.GetPopup().AddItem(componentType.Name, i);
		}
	}

	public void AddComponent(int index)
	{
		if (current != null)
		{
			var componentType = Utils.COMPONENTS[index];
			var component = Activator.CreateInstance(componentType);

			var fieldMap = component.ToFieldMap();

			var metadata = new Dictionary<string, object>() {
				{ "components", new Dictionary<string, object>() {
					{ componentType.Name, fieldMap.Count > 0 ?  fieldMap : true }
				} }
			}.ToFlat("/");

			foreach (var entry in metadata)
			{
				current.SetMeta(entry.Key.ToLower(), entry.Value);
			}

			GetUndoRedo().CreateAction($"Add {componentType.Name}");
			GetUndoRedo().CommitAction();
		}

		RenderComponents();
	}
}
