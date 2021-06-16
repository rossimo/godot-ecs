using System;
using Godot;

public class ClickableSprite : Godot.Sprite
{
    public Rect2 Rect = new Rect2();

    [Signal]
    public delegate void pressed();

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.IsPressed() && Rect.HasPoint(ToLocal(mouseButton.Position)))
            {
                EmitSignal(nameof(pressed));
            }
        }
    }
}