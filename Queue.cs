using Leopotam.EcsLite;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public struct Queue<T>
    where T : struct
{
    public T[] Items;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<T> GetEnumerator()
    {
        return (Items as IEnumerable<T>).GetEnumerator();
    }
}

public static class QueueUtils
{
    public static ref T Queue<T>(this EcsPool<Queue<T>> pool, int entity)
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
public class Queued : System.Attribute
{

}