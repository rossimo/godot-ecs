using Ecs;
using System.Collections.Generic;

public record Inventory : Component
{
    public IEnumerable<Component> Items = new List<Component>();

    public Inventory() { }

    public Inventory(IEnumerable<Component> items)
        => (Items) = (items);
}

public record AddItem(Component Item, string Target = null, bool TargetOther = false) : Command(Target, TargetOther);