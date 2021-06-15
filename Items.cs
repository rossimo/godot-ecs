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

public static class Items
{
    public static State System(int tick, State state, string target, Task[] tasks)
    {
        foreach (var task in tasks)
        {
            switch (task)
            {
                case AddItem addItem:
                    {
                        var entity = state[target];
                        var inventory = entity.Get<Inventory>();
                        if (inventory != null)
                        {
                            state = state.With(target, entity.With(new Inventory(inventory.Items.Concat(new[] { addItem.Item }))));
                        }
                    }
                    break;
            }
        }

        return state;
    }
}