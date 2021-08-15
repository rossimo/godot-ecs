using Leopotam.EcsLite;

public struct Event<E>
    where E : struct
{
    public object Component;
    public Target Target;

    public void Add(EcsWorld world, int self, int other)
    {
        world.Add(Resolve(self, other), Component);
    }

    public int Resolve(int self, int other)
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

public enum Target
{
    Self,
    Other
}

[System.AttributeUsage(System.AttributeTargets.Struct)]
public class IsEvent : System.Attribute
{

}