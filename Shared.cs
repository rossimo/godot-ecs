

using System.Runtime.InteropServices;
using Flecs;

[StructLayout(LayoutKind.Sequential)]
public struct Shared : IComponent
{
    public Game Game;
    public int Events;
    public int Physics;
    public int Input;
    public int FrameTime;
}