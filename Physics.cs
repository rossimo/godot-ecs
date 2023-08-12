using Flecs;
using Godot;
using System.Runtime.InteropServices;
using bottlenoselabs.C2CS.Runtime;

[StructLayout(LayoutKind.Sequential)]
public struct PhysicsNode : IComponent
{
    public CString Node;
}
