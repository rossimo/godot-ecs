using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

[Tool]
public class EcsPlugin : EditorPlugin
{
    private Control dock;
    private Node current;

    private static int MARGIN = 3;

    public override void _EnterTree()
    {
        dock = new Control()
        {
            Name = "Components",
            SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill,
        };
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
        var nodes = selector.GetSelectedNodes().ToArray<Node>();

        current = nodes.Length == 1
            ? nodes[0]
            : null;

        RenderComponents();
    }

    public void RenderComponents()
    {
        foreach (Node child in dock.GetChildren())
        {
            dock.RemoveChild(child);
            child.QueueFree();
        }

        if (current == null)
        {
            var center = new CenterContainer()
            {
                AnchorRight = 1,
                AnchorBottom = 1,
            };
            dock.AddChild(center);

            center.AddChild(new Label()
            {
                Text = "Select a single node to edit its components.",
                Align = Label.AlignEnum.Center
            });

            return;
        }

        var panel = new Panel()
        {
            AnchorRight = 1,
            AnchorBottom = 1,
        };
        dock.AddChild(panel);

        var layout = new VBoxContainer()
        {
            AnchorRight = 1,
            SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
            MarginLeft = MARGIN,
            MarginTop = MARGIN,
            MarginBottom = -MARGIN,
            MarginRight = -MARGIN
        };
        panel.AddChild(layout);

        Action<Godot.Control, object, string, bool> addObject = null;
        addObject = (Godot.Control parent, object obj, string prefix, bool top) =>
        {
            var type = obj.GetType();
            var queued = type.GetCustomAttributes(typeof(Queued), false)?.Length > 0;

            var componentLayout = new VBoxContainer()
            {
                SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill
            };
            parent.AddChild(componentLayout);

            var componentPrimitiveLayout = new HBoxContainer()
            {
                SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill
            };
            componentLayout.AddChild(componentPrimitiveLayout);

            componentPrimitiveLayout.AddChild(new Label()
            {
                Text = $"{(queued ? "» " : "")}{type.Name}",
                SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = 0
            });

            var fieldsLayout = new VBoxContainer()
            {
                SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = 0
            };
            componentPrimitiveLayout.AddChild(fieldsLayout);

            if (top)
            {
                var remove = new Button()
                {
                    Text = "X",
                    SizeFlagsVertical = 0
                };
                componentPrimitiveLayout.AddChild(remove);
                remove.Connect("pressed", this, nameof(ComponentRemoved), new Godot.Collections.Array { prefix });
            }

            foreach (var fieldInfo in type.GetFields())
            {
                var fieldLayout = new HBoxContainer();
                fieldsLayout.AddChild(fieldLayout);

                var childObj = fieldInfo.GetValue(obj);
                var name = $"{prefix}/{fieldInfo.Name.ToLower()}";

                if (fieldInfo.FieldType.IsEditable())
                {
                    fieldLayout.AddChild(new Label()
                    {
                        Text = fieldInfo.Name
                    });

                    var editor = new LineEdit()
                    {
                        SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
                        Text = $"{childObj}"
                    };
                    fieldLayout.AddChild(editor);

                    editor.Connect("text_changed", this, nameof(FieldChanged),
                        new Godot.Collections.Array { name, new GodotWrapper(fieldInfo.FieldType) });
                }
                else
                {
                    var indentLayout = new HBoxContainer()
                    {
                        SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
                        SizeFlagsVertical = 0
                    };
                    componentLayout.AddChild(indentLayout);

                    indentLayout.AddChild(new Control()
                    {
                        RectMinSize = new Vector2(15, 0)
                    });

                    var childLayout = new VBoxContainer()
                    {
                        SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
                        SizeFlagsVertical = 0
                    };
                    indentLayout.AddChild(childLayout);
                    addObject(childLayout, childObj, name, false);
                }
            }
        };

        foreach (var component in current.ToComponents().OrderBy(el => el.GetType().Name))
        {
            addObject(layout, component, $"components/{component.GetType().Name.ToLower()}", true);
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

    public void FieldChanged(string value, string path, GodotWrapper typeWrapper)
    {
        if (current == null) return;

        var type = typeWrapper.Get<Type>();

        try
        {
            current.SetMeta(path, Convert.ChangeType(value, type));
        }
        catch (Exception)
        {
            current.SetMeta(path, "");
        }

        GetUndoRedo().CreateAction($"Edited {path}");
        GetUndoRedo().CommitAction();
    }

    public void ComponentRemoved(string prefix)
    {
        if (current == null) return;

        foreach (var meta in current.GetMetaList().Where(meta => meta.StartsWith(prefix)))
        {
            current.RemoveMeta(meta);
        }

        GetUndoRedo().CreateAction($"Edited {prefix}");
        GetUndoRedo().CommitAction();

        RenderComponents();
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
