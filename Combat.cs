using Ecs;

public record ExpirationEvent : Event
{
    public ulong Tick;

    public ExpirationEvent(params Task[] tasks) => (Tasks) = (tasks);

    public override string ToString()
    {
        return $"{this.GetType().Name} {{ Tick = {Tick}, {Utils.Log(nameof(Tasks), Tasks)} }}";
    }
}

public static class Combat
{
    public static State System(State previous, State state)
    {
        var tick = state["physics"].Get<Ticks>().Tick;

        foreach (var (id, ev) in state.Get<ExpirationEvent>())
        {
            if (ev.Tick <= tick)
            {
                var queued = (id, null as string, ev);

                state = state.Without<ExpirationEvent>(id);

                state = state.With(Events.ENTITY, entity => entity.With(new EventQueue()
                {
                    Events = entity.Get<EventQueue>().Events.With(queued)
                }));
            }
        }

        return state;
    }
}