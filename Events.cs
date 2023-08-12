public class Event<E>
{
    public object Component;
    public object Target;
}

public interface Target
{
    public int Resolve(int self, int other);
}

[IsTarget]
public class TargetSelf : Target
{
    public int Resolve(int self, int other)
    {
        return self;
    }
}

[IsTarget]
public class TargetOther : Target
{
    public int Resolve(int self, int other)
    {
        return other;
    }
}

[System.AttributeUsage(System.AttributeTargets.Class)]
public class IsEvent : System.Attribute
{

}

[System.AttributeUsage(System.AttributeTargets.Class)]
public class IsTarget : System.Attribute
{

}