using System.Collections.Generic;

public static class Utils
{
    public static T[] ToArray<T>(this Godot.Collections.Array godotArray)
    {
        var list = new List<T>();

        foreach (T element in godotArray)
        {
            list.Add(element);
        }

        return list.ToArray();
    }
}