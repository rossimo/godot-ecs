using System;
using SimpleEcs;

record Component1 : Component
{
    public int X;
}

record Component2 : Component
{
    public int Y;
}

record Component3 : Component
{
    public int Z;
}

class SimpleEcsTest
{
    static void Main(string[] args)
    {
        var state = new SimpleEcsTest.State();

        Console.WriteLine(args.Length);
    }
}

public static class System1
{
    public static State System(State previous, State state)
    {

    }
}