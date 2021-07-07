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
        var tick = state.Get<Ticks>("physics").Tick;

        foreach (var (id, ev) in state.Get<ExpirationEvent>())
        {
            if (ev.Tick <= tick)
            {
                var queued = (id, null as string, ev);

                state = state.Without<ExpirationEvent>(id);

                state = state.With(Events.ENTITY, new EventQueue()
                {
                    Events = state.Get<EventQueue>(Events.ENTITY).Events.With(queued)
                });
            }
        }

        return state;
    }
}