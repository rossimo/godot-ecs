using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

[Tool]
public class EcsPlugin : EditorPlugin
{
    private Control dock;
    private Node current;

    private List<Type> COMPONENTS = GetComponents().ToList();

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

        var layout = new VBoxContainer();
        layout.AnchorRight = 1;
        dock.AddChild(layout);

        foreach (var metadata in current?.GetMetaList() ?? new string[] { })
        {
            var path = metadata.Split('/');
            if (path.Length < 2) continue;

            var prefix = path[0];
            if (path[0] != "components") continue;

            var name = path[1];
            var component = COMPONENTS
                .FirstOrDefault(component => component.Name.ToLower() == name.ToLower());
            if (component == null) continue;

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

            foreach (var fieldInfo in component.GetFields())
            {
                var fieldLabel = new Label() { Text = fieldInfo.Name };
                fieldsLayout.AddChild(fieldLabel);
            }
        }

        var picker = new OptionButton() { Text = "Add Component" };
        layout.AddChild(picker);
        picker.Connect("item_selected", this, nameof(AddComponent));

        for (var i = 0; i < COMPONENTS.Count; i++)
        {
            var componentType = COMPONENTS[i];
            picker.GetPopup().AddItem(componentType.Name, i);
        }
    }

    public void AddComponent(int index)
    {
        if (current != null)
        {
            var componentType = COMPONENTS[index];
            var component = Activator.CreateInstance(componentType);

            var metadata = new Dictionary<string, object>() {
                { "components", new Dictionary<string, object>() {
                    { componentType.Name.ToLower(), component.ToFieldMap() }
                }}
            }.ToFlat("/");

            foreach (var entry in metadata)
            {
                current.SetMeta(entry.Key, entry.Value);
            }

            GetUndoRedo().CreateAction($"Add {componentType.Name}");
            GetUndoRedo().CommitAction();
        }

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
