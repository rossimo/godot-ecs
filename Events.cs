using Flecs.NET.Core;

public struct Event<E>
{
    public object Component;
    public Target Target;
}

public interface Target
{
    public Entity Resolve(Entity self, Entity other);
}

[IsTarget]
public struct TargetSelf : Target
{
    public Entity Resolve(Entity self, Entity other)
    {
        return self;
    }
}

[IsTarget]
public struct TargetOther : Target
{
    public Entity Resolve(Entity self, Entity other)
    {
        return other;
    }
}

[AttributeUsage(AttributeTargets.Struct)]
public class IsEvent : Attribute
{

}

[AttributeUsage(AttributeTargets.Struct)]
public class IsTarget : Attribute
{

}