using Godot;
using System;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Linq;

public struct Listener<E>
    where E : struct
{
    public E Component;
    public Target Target;

    public void Run(EcsWorld world, int self, int other)
    {
        world.Add(ResolveTarget(self, other), Component);
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