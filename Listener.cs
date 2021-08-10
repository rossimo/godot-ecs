using Godot;
using System;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Linq;

public struct Listener<E>
    where E : struct
{
    public object[] Components;
    public Target Target;

    public void Run(EcsWorld world, int self, int other)
    {
        foreach (var component in Components)
        {
            world.Add(ResolveTarget(self, other), component);
        }
    }

    public int ResolveTarget(int self, int other)
    {
        var target = -1;

        if (Target == Target.Self)
        {
            target = self;
        }
        else if (Target == Target.Other)
        {
            target = other;
        }

        return target;
    }
}

[System.AttributeUsage(System.AttributeTargets.Struct)]
public class Listened : System.Attribute
{

}