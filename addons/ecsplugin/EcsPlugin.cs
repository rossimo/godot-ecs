using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

[Tool]
public class EcsPlugin : EditorPlugin
{
    private Control dock;
    private List<Type> components = new List<Type>();
    private List<Type> available = GetComponents().ToList();
    private Node current;

    public override void _EnterTree()
    {
        dock = new Panel() { Name = "Components" };
        AddControlToDock(DockSlot.RightUl, dock);

        RenderComponents();

        var selector = GetEditorInterface().GetSelection();
        selector.Connect("selection_changed", this, nameof(SelectedNode));
    }

    public override void _ExitTree()
    {
        RemoveControlFromDocks(dock);
        dock.QueueFree();
    }

    public void SelectedNode()
    {
        var selector = GetEditorInterface().GetSelection();

        current = selector.GetSelectedNodes().ToArray<Node>().FirstOrDefault();
		current.GetMetaList();
    }

    public void RenderComponents()
    {
        foreach (Node child in dock.GetChildren())
        {
            dock.RemoveChild(child);
            child.QueueFree();
        }

        var layout = new VBoxContainer();
        layout.AnchorRight = 1;
        dock.AddChild(layout);

        foreach (var component in components)
        {
            var componentLayout = new HBoxContainer();
            componentLayout.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
            layout.AddChild(componentLayout);

            var componentLabel = new Label() { Text = component.Name };
            componentLabel.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
            componentLabel.SizeFlagsVertical = 0;
            componentLayout.AddChild(componentLabel);

            var fieldsLayout = new VBoxContainer();
            fieldsLayout.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
            fieldsLayout.SizeFlagsVertical = 0;
            componentLayout.AddChild(fieldsLayout);

            foreach (var field in component.GetFields())
            {
                var fieldLabel = new Label() { Text = field.Name };
                fieldsLayout.AddChild(fieldLabel);
            }
        }

        var picker = new OptionButton() { Text = "Add Component" };
        layout.AddChild(picker);
        picker.Connect("item_selected", this, nameof(AddComponent));

        for (var i = 0; i < available.Count; i++)
        {
            var component = available[i];
            if (components.Contains(component)) continue;

            picker.GetPopup().AddItem(component.Name, i);
        }
    }

    public void AddComponent(int index)
    {
        var component = available[index];
        components.Add(component);
        components = components.OrderBy(component => component.Name).ToList();

        available = GetComponents().Where(component => !components.Contains(component)).ToList();

        RenderComponents();
    }

    public static Type[] GetComponents()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.GetCustomAttributes(typeof(EditorComponent), false)?.Length > 0)
            .OrderBy(component => component.Name)
            .ToArray();
    }
}
