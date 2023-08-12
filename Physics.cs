using Flecs;
using Godot;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct PhysicsNode : IComponent
{
    public CharacterBody2D Node;
}
