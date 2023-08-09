public struct Event<E>
    where E : struct
{
    public object Component;
    public object Target;
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