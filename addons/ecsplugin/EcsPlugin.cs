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

		Action<Godot.Control, object, object, string> addObject = null;
		addObject = (Godot.Control parent, object obj, object parentObj, string prefix) =>
		{
			var type = obj.GetType();
			var parentType = parentObj?.GetType();
			var isMany = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Many<>);
			var IsListened = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Listener<>);
			var parentIsMany = parentType?.IsGenericType == true && parentType?.GetGenericTypeDefinition() == typeof(Many<>);

			if (isMany)
			{
				var arrayField = obj.GetType().GetField("Items");
				var array = arrayField.GetValue(obj) as Array;
				for (var i = 0; i < array.Length; i++)
				{
					var childLayout = new HBoxContainer()
					{
						SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
						SizeFlagsVertical = 0
					};
					parent.AddChild(childLayout);

					addObject(childLayout, array.GetValue(i), obj, $"{prefix}/{i}");
				}
				return;
			}

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

			var titleLayout = new HBoxContainer()
			{
				SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
				SizeFlagsVertical = 0,
			};
			componentPrimitiveLayout.AddChild(titleLayout);

			titleLayout.AddChild(new Label()
			{
				Text = $"{(parentIsMany ? "•  " : "")}{type.Name}"
			});

			if (IsListened)
			{
				titleLayout.AddChild(new Godot.TextureRect()
				{
					Texture = GD.Load<Texture>("res://event_small.png"),
					StretchMode = TextureRect.StretchModeEnum.KeepCentered
				});
			}

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
						addObject(childLayout, childObj, obj, name);
					}
				}
			}

			var remove = new Button()
			{
				Text = "X",
				SizeFlagsVertical = 0
			};
			componentPrimitiveLayout.AddChild(remove);
			remove.Connect("pressed", this, nameof(ComponentRemoved), new Godot.Collections.Array { prefix });
		};


		foreach (var obj in current.ToComponents().OrderBy(el => el.GetType().Name))
		{
			var type = obj.GetType();
			var name = type.Name.ToLower();
			var isMany = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Many<>);

			if (isMany)
			{
				name = type.GetGenericArguments().First().Name.ToLower() + "[]";
			}

			addObject(layout, obj, null, $"components/{name}");
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

		GetUndoRedo().CreateAction($"Removing {prefix}");
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
			object values = fieldMap.Count > 0 ? fieldMap : true;
			object entry = null;
			var metaName = componentType.Name;

			if (componentType.HasManyHint())
			{
				metaName = $"{metaName}[]";
				var dict = new Dictionary<string, object>();

				var key = 0;
				var existingMeta = current.GetMetaList();
				while (existingMeta
					.Where(meta => meta.StartsWith($"components/{metaName}/{key}".ToLower()))
					.Count() > 0)
				{
					key++;
				}

				dict[$"{key}"] = values;

				entry = dict;
			}
			else
			{
				entry = values;
			}

			var metadata = new Dictionary<string, object>() {
				{ "components", new Dictionary<string, object>() {
					{ metaName, entry }
				} }
			}.ToFlat("/");

			foreach (var meta in metadata)
			{
				current.SetMeta(meta.Key.ToLower(), meta.Value);
			}

			GetUndoRedo().CreateAction($"Add {componentType.Name}");
			GetUndoRedo().CommitAction();
		}

		RenderComponents();
	}
}
