aaaaaaaaaausing System.Runtime.CompilerServices;

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
    public static ref T Concat<T>(this EcsPool<Many<T>> pool, int entity)
        where T : struct
    {
        ref var queue = ref pool.Ensure(entity);

        if (queue.Items == null)
        {
            queue.Items = new[] { default(T) };
        }
        else
        {
            var list = queue.Items.ToList();
            list.Add(default(T));
            queue.Items = list.ToArray();
        }

        return ref queue.Items[queue.Items.Length - 1];
    }
}

[System.AttributeUsage(System.AttributeTargets.Struct)]
public class IsMany : System.Attribute
{

}