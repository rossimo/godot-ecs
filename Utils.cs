
using System;
using System.Linq;
using System.Collections.Generic;

public static class Utils
{
    private static string SPACER = "    ";
    private static int DEPTH = 0;

    public static string ID(this DefaultEcs.Entity entity)
    {
        var data = entity.ToString().Split(" ").Last();
        var world = data.Split(":").First();
        var id = data.Split(".").First().Split(":").Last();
        var version = data.Split(".").Last();
        return $"{world}-{id}-{version}";
    }

    public static string Log<V>(string name, IEnumerable<V> list)
    {
        var indent = String.Join("", Enumerable.Range(0, DEPTH + 1).Select(i => SPACER));

        DEPTH++;
        var root = $"\n{indent}{String.Join(",\n" + indent, (V[])list).Trim()}{indent}\n";
        DEPTH--;

        var trimmedRoot = list.Count() > 0
            ? root
            : root.Trim();

        var suffix = list.Count() > 0
            ? String.Join("", Enumerable.Range(0, DEPTH).Select(i => SPACER))
            : "";

        return $"{name} = [{trimmedRoot}{suffix}]";
    }

    public static V[] With<V>(this V[] list, V value)
    {
        return list.Concat(new[] { value }).ToArray();
    }

    public static C TryGet<C>(this DefaultEcs.World world)
    {
        return world.Has<C>()
            ? world.Get<C>()
            : default(C);
    }

    public static C TryGet<C>(this DefaultEcs.Entity entity)
    {
        return entity.Has<C>()
            ? entity.Get<C>()
            : default(C);
    }
}
