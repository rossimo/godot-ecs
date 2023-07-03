using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

[Tool]
public partial class EcsPlugin : EditorPlugin
{
    private Control dock;
    private Node current;

    private static int MARGIN = 3;

    public override void _EnterTree()
    {
        dock = new Control()
        {
            Name = "Components",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        AddControlToDock(DockSlot.RightUl, dock);

        RenderComponents();

        var selector = GetEditorInterface().GetSelection();
        selector.Connect("selection_changed", new Callable(this, nameof(SelectedNode)));

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

    public enum Category
    {
        Component,
        MultiComponent,
        Target,
        Event,
        Value
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
                HorizontalAlignment = Godot.HorizontalAlignment.Center
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
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            OffsetLeft = MARGIN,
            OffsetTop = MARGIN,
            OffsetBottom = -MARGIN,
            OffsetRight = -MARGIN
        };
        panel.AddChild(layout);

        Action<Godot.Control, object, string, Category> addObject = null;
        addObject = (Godot.Control parent, object obj, string prefix, Category objCategory) =>
        {
            var type = obj.GetType();
            var isMany = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Many<>);
            var isEvent = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Event<>);

            if (isMany)
            {
                var manyLayout = new VBoxContainer()
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    SizeFlagsVertical = 0
                };
                parent.AddChild(manyLayout);

                var arrayField = type.GetField("Items");
                var array = arrayField.GetValue(obj) as Array;
                for (var i = 0; i < array.Length; i++)
                {
                    addObject(manyLayout, array.GetValue(i), $"{prefix}/{i}", Category.MultiComponent);
                }
                return;
            }

            if (isEvent)
            {
                var eventLayout = new VBoxContainer()
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    SizeFlagsVertical = 0
                };
                parent.AddChild(eventLayout);

                var eventTitleLayout = new HBoxContainer()
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                };
                eventLayout.AddChild(eventTitleLayout);

                var iconLayout = new CenterContainer()
                {
                    CustomMinimumSize = new Vector2(0, 26),
                    SizeFlagsVertical = 0
                };
                eventTitleLayout.AddChild(iconLayout);

                iconLayout.AddChild(new Godot.TextureRect()
                {
                    Texture = GD.Load<Texture2D>("res://satellite.png")
                });

                var eventType = type.GenericTypeArguments[0];
                eventTitleLayout.AddChild(new Label()
                {
                    Text = eventType.Name,
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                });

                var eventRemove = new Button()
                {
                    Text = "X",
                    SizeFlagsVertical = 0
                };
                eventTitleLayout.AddChild(eventRemove);
                eventRemove.Pressed += () => ComponentRemoved(prefix);

                var outerChildLayout = new HBoxContainer()
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                };
                eventLayout.AddChild(outerChildLayout);

                var childLayout = new VBoxContainer()
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    SizeFlagsVertical = 0
                };
                outerChildLayout.AddChild(childLayout);

                var targetField = type.GetField("Target");
                var target = targetField.GetValue(obj);

                var targetLayout = new HBoxContainer()
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                };
                childLayout.AddChild(targetLayout);

                var targetIconLayout = new VBoxContainer() { };
                targetLayout.AddChild(targetIconLayout);

                targetIconLayout.AddChild(new Godot.TextureRect()
                {
                    Texture = GD.Load<Texture2D>("res://node.png"),
                    CustomMinimumSize = new Vector2(0, 26),
                    StretchMode = TextureRect.StretchModeEnum.KeepCentered,
                });

                if (target == null)
                {
                    var pickerLayout = new HBoxContainer()
                    {
                        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                    };
                    targetLayout.AddChild(pickerLayout);

                    pickerLayout.AddChild(new Godot.TextureRect()
                    {
                        Texture = GD.Load<Texture2D>("res://target.png"),
                        StretchMode = TextureRect.StretchModeEnum.KeepCentered
                    });

                    var picker = new OptionButton()
                    {
                        Text = "Set Target",
                        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                    };
                    pickerLayout.AddChild(picker);
                    picker.ItemSelected += (long index) => ReplaceComponent((int)index, prefix + "/target/", new GodotWrapper(MetaType.Target));

                    for (var i = 0; i < Utils.TARGETS.Count; i++)
                    {
                        var targetType = Utils.TARGETS[i];
                        picker.GetPopup().AddItem(targetType.Name, i);
                    }
                }
                else
                {
                    addObject(targetLayout, target, prefix + "/target/" + Utils.ComponentName(target), Category.Target);
                }

                var componentField = type.GetField("Component");
                var component = componentField.GetValue(obj);
                var childComponentLayout = new HBoxContainer()
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                };
                childLayout.AddChild(childComponentLayout);

                var childComponentIconLayout = new VBoxContainer() { };
                childComponentLayout.AddChild(childComponentIconLayout);

                childComponentIconLayout.AddChild(new Godot.TextureRect()
                {
                    Texture = GD.Load<Texture2D>("res://node.png"),
                    CustomMinimumSize = new Vector2(0, 26),
                    StretchMode = TextureRect.StretchModeEnum.KeepCentered,
                });

                if (component == null)
                {
                    var pickerLayout = new HBoxContainer()
                    {
                        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                    };
                    childComponentLayout.AddChild(pickerLayout);

                    pickerLayout.AddChild(new Godot.TextureRect()
                    {
                        Texture = GD.Load<Texture2D>("res://tag.png"),
                        StretchMode = TextureRect.StretchModeEnum.KeepCentered
                    });

                    var picker = new OptionButton()
                    {
                        Text = "Set Component",
                        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                    };
                    pickerLayout.AddChild(picker);
                    picker.ItemSelected += (long index) => ReplaceComponent((int)index, prefix + "/component/", new GodotWrapper(MetaType.Component));

                    for (var i = 0; i < Utils.COMPONENTS.Count; i++)
                    {
                        var componentType = Utils.COMPONENTS[i];
                        picker.GetPopup().AddItem(componentType.Name, i);
                    }
                }
                else
                {
                    addObject(childComponentLayout, component, prefix + "/component/" + Utils.ComponentName(component), Category.Component);
                }

                return;
            }

            var componentPrimitiveOuterLayout = new VBoxContainer()
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = 0
            };
            parent.AddChild(componentPrimitiveOuterLayout);

            var componentPrimitiveLayout = new HBoxContainer()
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            componentPrimitiveOuterLayout.AddChild(componentPrimitiveLayout);

            var titleLayout = new HBoxContainer()
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = 0,
            };
            componentPrimitiveLayout.AddChild(titleLayout);

            Texture2D iconTex = null;
            switch (objCategory)
            {
                case Category.Component:
                    {
                        iconTex = GD.Load<Texture2D>("res://tag.png");
                        break;
                    }
                case Category.MultiComponent:
                    {
                        iconTex = GD.Load<Texture2D>("res://tags.png");
                        break;
                    }
                case Category.Target:
                    {
                        iconTex = GD.Load<Texture2D>("res://target.png");
                        break;
                    }
            }

            if (iconTex != null)
            {
                titleLayout.AddChild(new Godot.TextureRect()
                {
                    Texture = iconTex,
                    StretchMode = TextureRect.StretchModeEnum.KeepCentered
                });
            }

            titleLayout.AddChild(new Label()
            {
                Text = type.Name
            });

            if (type.GetFields().Length > 0)
            {
                var fieldsLayout = new VBoxContainer()
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    SizeFlagsVertical = 0
                };
                componentPrimitiveLayout.AddChild(fieldsLayout);

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
                            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                            Text = $"{childObj}"
                        };
                        fieldLayout.AddChild(editor);
                        editor.TextChanged += (string value) => FieldChanged(value, name, new GodotWrapper(fieldInfo.FieldType));
                    }
                    else
                    {
                        var indentLayout = new HBoxContainer()
                        {
                            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                            SizeFlagsVertical = 0
                        };
                        componentPrimitiveOuterLayout.AddChild(indentLayout);

                        indentLayout.AddChild(new Control()
                        {
                            CustomMinimumSize = new Vector2(16, 0)
                        });

                        var childLayout = new VBoxContainer()
                        {
                            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                            SizeFlagsVertical = 0
                        };
                        indentLayout.AddChild(childLayout);
                        addObject(childLayout, childObj, name, Category.Value);
                    }
                }
            }

            if (objCategory != Category.Value)
            {
                var remove = new Button()
                {
                    Text = "X",
                    SizeFlagsVertical = 0
                };
                componentPrimitiveLayout.AddChild(remove);
                remove.Pressed += () => ComponentRemoved(prefix);
            }
        };

        foreach (var obj in current.ToComponents("components/").OrderBy(el => el.GetType().Name))
        {
            var type = obj.GetType();
            var name = type.Name.ToLower();
            var annotations = new List<string>();

            var isMany = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Many<>);
            if (isMany)
            {
                var manyType = type = type.GetGenericArguments().First();
                name = manyType.Name.ToLower();
                annotations.Add("[]");
            }

            var isEvent = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Event<>);
            if (isEvent)
            {
                var eventType = type = type.GetGenericArguments().First();
                name = eventType.Name.ToLower();
                annotations.Add("()");
            }

            name = $"{name}{(String.Join("", annotations))}";

            addObject(layout, obj, $"components/{name}", isMany ? Category.MultiComponent : Category.Component);
        }

        var picker = new OptionButton() { Text = "Add Component" };
        layout.AddChild(picker);
        picker.ItemSelected += (long index) => AddComponent((int)index, "components/");

        for (var i = 0; i < Utils.COMPONENTS.Count; i++)
        {
            var componentType = Utils.COMPONENTS[i];
            picker.GetPopup().AddItem(componentType.Name, i);

            Texture2D iconTex = null;
            if (componentType.HasEventHint())
            {
                iconTex = GD.Load<Texture2D>("res://satellite.png");
            }
            else if (componentType.HasManyHint())
            {
                iconTex = GD.Load<Texture2D>("res://tags.png");
            }
            else
            {
                iconTex = GD.Load<Texture2D>("res://tag.png");
            }

            if (iconTex != null)
            {
                picker.GetPopup().SetItemIcon(i, iconTex);
            }
        }
    }

    public void FieldChanged(string value, string path, GodotWrapper typeWrapper)
    {
        if (current == null) return;

        var type = typeWrapper.Get<Type>();

        try
        {
            current.SetMeta(path, Convert.ChangeType(value, type).ToVariant());
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

        var unserialized = current.ToComponents("components" + Utils.DELIMETER);

        foreach (var meta in current.GetMetaList())
        {
            current.RemoveMeta(meta);
        }

        foreach (var component in unserialized)
        {
            var meta = component.ToMeta();
            foreach (var entry in meta)
            {
                current.SetMeta("components" + Utils.DELIMETER + entry.Key.ToLower(), entry.Value.ToVariant());
            }
        }

        GetUndoRedo().CreateAction($"Removing {prefix}");
        GetUndoRedo().CommitAction();

        RenderComponents();
    }

    public void ReplaceComponent(int index, string prefix, GodotWrapper metaType)
    {
        if (current != null)
        {
            Type type = null;

            switch (metaType.Get<MetaType>())
            {
                case MetaType.Component:
                    {
                        type = Utils.COMPONENTS[index];
                    }
                    break;
                case MetaType.Target:
                    {
                        type = Utils.TARGETS[index];
                    }
                    break;
            }
            if (type == null) return;

            var component = Utils.Instantiate(type);

            var metas = component.ToMeta();
            var metaName = metas.First().Key.Split(Utils.DELIMETER).First();
            var manyIndex = 0;

            if (type.HasManyHint())
            {
                var existingMeta = current.GetMetaList();
                while (existingMeta
                    .Where(meta => meta.StartsWith($"{prefix}{Utils.DELIMETER}{manyIndex}".ToLower()))
                    .Count() > 0)
                {
                    manyIndex++;
                }
            }

            var normalizedPrefix = prefix.Split(Utils.DELIMETER).Where(part => part.Length > 0).ToArray();

            foreach (var meta in current.GetMetaList().Where(meta =>
            {
                var normalizedMeta = meta.Split(Utils.DELIMETER).Where(part => part.Length > 0).ToArray();
                if (normalizedMeta.Length < normalizedPrefix.Length)
                {
                    return false;
                }

                for (int i = 0; i < normalizedPrefix.Length; i++)
                {
                    if (normalizedPrefix[i] != normalizedMeta[i])
                    {
                        return false;
                    }
                }

                return true;
            }))
            {
                current.RemoveMeta(meta);
            }

            foreach (var meta in metas)
            {
                var key = prefix + meta.Key.ToLower();
                if (type.HasManyHint())
                {
                    key = key.Replace(prefix + metaName + Utils.DELIMETER + "0" + Utils.DELIMETER, prefix + metaName + $"{Utils.DELIMETER}{manyIndex}{Utils.DELIMETER}");
                }
                current.SetMeta(key, meta.Value.ToVariant());
            }

            GetUndoRedo().CreateAction($"Add {type.Name}");
            GetUndoRedo().CommitAction();
        }

        RenderComponents();
    }

    public void AddComponent(int index, string prefix)
    {
        if (current != null)
        {
            var type = Utils.COMPONENTS[index];
            var component = Utils.Instantiate(type);

            var metas = component.ToMeta();
            var metaName = metas.First().Key.Split(Utils.DELIMETER).First();
            var manyIndex = 0;

            if (type.HasManyHint())
            {
                var existingMeta = current.GetMetaList();
                while (existingMeta
                    .Where(meta => meta.StartsWith($"components{Utils.DELIMETER}{metaName}{Utils.DELIMETER}{manyIndex}".ToLower()))
                    .Count() > 0)
                {
                    manyIndex++;
                }
            }

            foreach (var meta in metas)
            {
                var key = prefix + meta.Key.ToLower();
                if (type.HasManyHint())
                {
                    key = key.Replace(prefix + metaName + Utils.DELIMETER + "0" + Utils.DELIMETER, prefix + metaName + $"{Utils.DELIMETER}{manyIndex}{Utils.DELIMETER}");
                }

                current.SetMeta(key, meta.Value.ToVariant());
            }

            GetUndoRedo().CreateAction($"Add {type.Name}");
            GetUndoRedo().CommitAction();
        }

        RenderComponents();
    }
}
