using System;
using Godot;

public class ClickableSprite : Godot.Sprite
{
    [Signal]
    public delegate void pressed();

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.IsPressed() && GetRect().HasPoint(ToLocal(mouseButton.Position)))
            {
                GetTree().SetInputAsHandled();
                EmitSignal(nameof(pressed));
            }
        }
    }
}