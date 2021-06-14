using Ecs;
using System.Linq;
using System.Collections.Generic;

public record Inventory : Component
{
    public Component[] Items = new Component[] { };

    public Inventory() { }

    public Inventory(IEnumerable<Component> items)
        => (Items) = (items.ToArray());
}

public record AddItem(Component Item, string Target = null) : Task(Target);