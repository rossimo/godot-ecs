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

		Action<Godot.Control, object, string> addObject = null;
		addObject = (Godot.Control parent, object obj, string prefix) =>
		{
			var type = obj.GetType();
			var isMany = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Many<>);
			var isEvent = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Event<>);

			if (isMany)
			{
				var manyLayout = new HBoxContainer()
				{
					SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill
				};
				parent.AddChild(manyLayout);

				var iconLayout = new CenterContainer()
				{
					RectMinSize = new Vector2(0, 26),
					SizeFlagsVertical = 0
				};
				manyLayout.AddChild(iconLayout);

				iconLayout.AddChild(new Godot.TextureRect()
				{
					Texture = GD.Load<Texture>("res://bars.png")
				});

				var childLayout = new VBoxContainer()
				{
					SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
					SizeFlagsVertical = 0
				};
				manyLayout.AddChild(childLayout);

				var arrayField = type.GetField("Items");
				var array = arrayField.GetValue(obj) as Array;
				for (var i = 0; i < array.Length; i++)
				{
					addObject(childLayout, array.GetValue(i), $"{prefix}/{i}");
				}
				return;
			}

			if (isEvent)
			{
				var eventLayout = new HBoxContainer()
				{
					SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill
				};
				parent.AddChild(eventLayout);

				var iconLayout = new CenterContainer()
				{
					RectMinSize = new Vector2(0, 26),
					SizeFlagsVertical = 0
				};
				eventLayout.AddChild(iconLayout);

				iconLayout.AddChild(new Godot.TextureRect()
				{
					Texture = GD.Load<Texture>("res://event.png")
				});

				var childLayout = new VBoxContainer()
				{
					SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
					SizeFlagsVertical = 0
				};
				eventLayout.AddChild(childLayout);

				var componentField = type.GetField("Component");
				var component = componentField.GetValue(obj);

				var picker = new OptionButton() { Text = "Set Component" };
				childLayout.AddChild(picker);
				picker.Connect("item_selected", this, nameof(ReplaceComponent), new Godot.Collections.Array { prefix + "/component/" });

				for (var i = 0; i < Utils.COMPONENTS.Count; i++)
				{
					var componentType = Utils.COMPONENTS[i];
					picker.GetPopup().AddItem(componentType.Name, i);
				}

				if (component != null)
				{
					addObject(childLayout, component, prefix + "/component/");
				}
				return;
			}

			var componentPrimitiveLayout = new HBoxContainer()
			{
				SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill
			};
			parent.AddChild(componentPrimitiveLayout);

			var titleLayout = new HBoxContainer()
			{
				SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
				SizeFlagsVertical = 0,
			};
			componentPrimitiveLayout.AddChild(titleLayout);

			titleLayout.AddChild(new Godot.TextureRect()
			{
				Texture = GD.Load<Texture>("res://cog.png"),
				StretchMode = TextureRect.StretchModeEnum.KeepCentered
			});

			titleLayout.AddChild(new Label()
			{
				Text = type.Name
			});

			if (type.GetFields().Length > 0)
			{
				var fieldsLayout = new VBoxContainer()
				{
					SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
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
						parent.AddChild(indentLayout);

						indentLayout.AddChild(new Control()
						{
							RectMinSize = new Vector2(16, 0)
						});

						var childLayout = new VBoxContainer()
						{
							SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
							SizeFlagsVertical = 0
						};
						indentLayout.AddChild(childLayout);
						addObject(childLayout, childObj, name);
					}
				}
			}

			var remove = new Button()
			{
				Text = "X",
				SizeFlagsVertical = 0
			};
			componentPrimitiveLayout.AddChild(remove);
			remove.Connect("pressed", this, nameof(ComponentRemoved), new Godot.Collections.Array { prefix + "/" });
		};


		foreach (var obj in current.ToComponents().OrderBy(el => el.GetType().Name))
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

			addObject(layout, obj, $"components/{name}");
		}

		var picker = new OptionButton() { Text = "Add Component" };
		layout.AddChild(picker);
		picker.Connect("item_selected", this, nameof(AddComponent), new Godot.Collections.Array { "components/" });

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

		var unserialized = current.ToComponents();

		foreach (var meta in current.GetMetaList())
		{
			current.RemoveMeta(meta);
		}

		foreach (var component in unserialized)
		{
			var meta = component.ToMeta();
			foreach (var entry in meta)
			{
				current.SetMeta("components/" + entry.Key.ToLower(), entry.Value);
			}
		}

		GetUndoRedo().CreateAction($"Removing {prefix}");
		GetUndoRedo().CommitAction();

		RenderComponents();
	}

	public void ReplaceComponent(int index, string prefix)
	{
		if (current != null)
		{
			var type = Utils.COMPONENTS[index];
			var component = Utils.Instantiate(type);

			var metas = component.ToMeta();
			var metaName = metas.First().Key.Split("/").First();
			var manyIndex = 0;

			if (type.HasManyHint())
			{
				var existingMeta = current.GetMetaList();
				while (existingMeta
					.Where(meta => meta.StartsWith($"components/{metaName}/{manyIndex}".ToLower()))
					.Count() > 0)
				{
					manyIndex++;
				}
			}

			foreach (var meta in current.GetMetaList().Where(meta => meta.StartsWith(prefix)))
			{
				current.RemoveMeta(meta);
			}

			foreach (var meta in metas)
			{
				var key = prefix + meta.Key.ToLower();
				if (type.HasManyHint())
				{
					key = key.Replace(prefix + metaName + "/0/", prefix + metaName + $"/{manyIndex}/");
				}
				current.SetMeta(key, meta.Value);
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
			var metaName = metas.First().Key.Split("/").First();
			var manyIndex = 0;

			if (type.HasManyHint())
			{
				var existingMeta = current.GetMetaList();
				while (existingMeta
					.Where(meta => meta.StartsWith($"components/{metaName}/{manyIndex}".ToLower()))
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
					key = key.Replace(prefix + metaName + "/0/", prefix + metaName + $"/{manyIndex}/");
				}
				current.SetMeta(key, meta.Value);
			}

			GetUndoRedo().CreateAction($"Add {type.Name}");
			GetUndoRedo().CommitAction();
		}

		RenderComponents();
	}
}
