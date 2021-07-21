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
        var com = state.Component1(1);

        return state.With(1, com with { X = com.X + 1 });
    }

    public static State System2(State previous, State state)
    {
        var com = state.Component2(2);

        return state.With(2, com with { Y = com.Y + 1 });
    }

    public static State System3(State previous, State state)
    {
        var com = state.Component3(3);

        return state.With(3, com with { Z = com.Z + 1 });
    }
}

public static class Extended
{
    public static int COMPONENT1 = typeof(Component1).Name.GetHashCode();
    public static int COMPONENT2 = typeof(Component2).Name.GetHashCode();
    public static int COMPONENT3 = typeof(Component3).Name.GetHashCode();

    public static Component1 Component1(this State state, int entityId)
    {
        return state.Get<Component1>(COMPONENT1, entityId);
    }

    public static State WithoutComponent1(this State state, int entityId)
    {
        return state.Without(COMPONENT1, entityId);
    }

    public static Dictionary<int, Component> Component1(this State state)
    {
        return state.GetAll<Component1>(COMPONENT1);
    }

    public static Component2 Component2(this State state, int entityId)
    {
        return state.Get<Component2>(COMPONENT2, entityId);
    }

    public static State WithoutComponent2(this State state, int entityId)
    {
        return state.Without(COMPONENT2, entityId);
    }

    public static Dictionary<int, Component> Component2(this State state)
    {
        return state.GetAll<Component2>(COMPONENT2);
    }

    public static Component3 Component3(this State state, int entityId)
    {
        return state.Get<Component3>(COMPONENT3, entityId);
    }

    public static State WithoutComponent3(this State state, int entityId)
    {
        return state.Without(COMPONENT3, entityId);
    }

    public static Dictionary<int, Component> Component3(this State state)
    {
        return state.GetAll<Component3>(COMPONENT3);
    }
}