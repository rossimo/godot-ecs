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
                            state = state.With(target, entity.With(new Inventory
                            {
                                Items = inventory.Items.Concat(new[] { addItem.Item }).ToArray()
                            }));
                        }
                    }
                    break;
            }
        }

        return state;
    }
}