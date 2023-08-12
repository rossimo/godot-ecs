using Flecs;
using System.Runtime.InteropServices;

[Editor]
[StructLayout(LayoutKind.Sequential)]
public struct Health : IComponent
{
    public int Value;
}
