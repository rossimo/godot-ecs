
using System;
using System.Linq;
using System.Collections.Generic;

public static class Utils
{
    private static string SPACER = "    ";
    private static int DEPTH = 0;

    public static string ID(this DefaultEcs.Entity entity)
    {
        return $"{entity.GetHashCode()}";
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
}
