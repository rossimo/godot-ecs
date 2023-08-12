using System.Runtime.CompilerServices;

public class Many<T>
{
    public T[] Items;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<T> GetEnumerator()
    {
        return (Items as IEnumerable<T>).GetEnumerator();
    }
}

public static class Many
{

}

[System.AttributeUsage(System.AttributeTargets.Class)]
public class IsMany : System.Attribute
{

}