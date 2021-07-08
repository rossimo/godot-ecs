using Ecs;
using System.Linq;

public record Inventory : Component
{
    public Component[] Items = new Component[] { };
}

public record AddItem : Task
{
    public Component Item;
}

public static class Items
{
    public static State System(int tick, State state, int target, Task[] tasks)
    {
        foreach (var task in tasks)
        {
            switch (task)
            {
                case AddItem addItem:
                    {
                        var inventory = state.Get<Inventory>(target);
                        if (inventory != null)
                        {
                            state = state.With(target, new Inventory
                            {
                                Items = inventory.Items.Concat(new[] { addItem.Item }).ToArray()
                            });
                        }
                    }
                    break;
            }
        }

        return state;
    }
}