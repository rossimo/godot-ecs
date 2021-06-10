using Ecs;
using System;
using System.Linq;

public record Item() : Component;

public record Potion() : Item;

public record Inventory(Item[] Items = null) : Component
{
    public Item[] Items { get; init; } = Items ?? new Item[0];

    public Inventory Add(params Item[] items)
    {
        return this with { Items = Items.Concat(items).ToArray() };
    }

    public Inventory Remove(params Item[] items)
    {
        return this with { Items = Items.Where(item => !items.Contains(item)).ToArray() };
    }
}

public record InventoryAddAction(Item Item) : Component;

public record InventoryRemoveAction(Item Item) : Component;

public static class Items
{
    public static State System(State state)
    {
        foreach (var (id, inventory, action) in state.Get<Inventory, InventoryRemoveAction>())
        {
            state = state.With(id, state[id].Without<InventoryRemoveAction>());
            state = state.With(id, state[id].With(inventory.Remove(action.Item)));
        }

        foreach (var (id, inventory, action) in state.Get<Inventory, InventoryAddAction>())
        {
            state = state.With(id, state[id].Without<InventoryAddAction>());
            state = state.With(id, state[id].With(inventory.Add(action.Item)));
        }

        return state;
    }
}