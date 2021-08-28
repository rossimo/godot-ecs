using Leopotam.EcsLite;

public struct Event<E>
    where E : struct
{
    public object Component;
    public object Target;

    public void Add(EcsWorld world, int self, int other)
    {
        if (Target is Target target)
        {
            var entityId = target.Resolve(self, other);
            if (entityId != -1)
            {
                world.Add(entityId, Component);
            }
        }
    }
}

public interface Target
{
    public int Resolve(int self, int other);
}

[IsTarget]
public struct TargetSelf : Target
{
    public int Resolve(int self, int other)
    {
        return self;
    }
}

[IsTarget]
public struct TargetOther : Target
{
    public int Resolve(int self, int other)
    {
        return other;
    }
}

[System.AttributeUsage(System.AttributeTargets.Struct)]
public class IsEvent : System.Attribute
{

}

[System.AttributeUsage(System.AttributeTargets.Struct)]
public class IsTarget : System.Attribute
{

}