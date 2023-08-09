using System.Runtime.CompilerServices;

public struct Many<T>
    where T : struct
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

[System.AttributeUsage(System.AttributeTargets.Struct)]
public class IsMany : System.Attribute
{

}