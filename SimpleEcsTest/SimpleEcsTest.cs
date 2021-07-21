using System;
using System.Collections.Generic;
using SimpleEcs;

public record Component1 : Component
{
    public int X;
}

public record Component2 : Component
{
    public int Y;
}

public record Component3 : Component
{
    public int Z;
}

public class SimpleEcsTest
{
    public static void Main(string[] args)
    {
        var state = new State()
        {
            LoggingIgnore = new[]
            {
                nameof(Component1).GetHashCode(),
                nameof(Component2).GetHashCode(),
                nameof(Component3).GetHashCode()
            }
        }.With(1, new Component1())
            .With(2, new Component2())
            .With(3, new Component3());

        Console.WriteLine("Start");

        State previous = state;
        for (var i = 0; i < 100000; i++)
        {
            state = Systems.System1(previous, state);
            state = Systems.System2(previous, state);
            state = Systems.System3(previous, state);
            previous = state;
        }

        Console.WriteLine("Stop");
    }
}

public static class Systems
{
    public static State System1(State previous, State state)
    {
        var com = state.Get<Component1>(1);

        return state.With(1, com with { X = com.X + 1 });
    }

    public static State System2(State previous, State state)
    {
        var com = state.Get<Component2>(2);

        return state.With(2, com with { Y = com.Y + 1 });
    }

    public static State System3(State previous, State state)
    {
        var com = state.Get<Component3>(3);

        return state.With(3, com with { Z = com.Z + 1 });
    }
}
