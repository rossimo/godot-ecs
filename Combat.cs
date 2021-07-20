using DefaultEcs;

public record ExpirationEvent : Event
{
    public ulong Tick;

    public ExpirationEvent(params Task[] tasks) => (Tasks) = (tasks);

    public override string ToString()
    {
        return $"{this.GetType().Name} {{ Tick = {Tick}, {Utils.Log(nameof(Tasks), Tasks)} }}";
    }
}

public class Combat
{
    private DefaultEcs.EntitySet ExpirationEvents;
    private DefaultEcs.World World;

    public Combat(DefaultEcs.World world)
    {
        World = world;

        ExpirationEvents = World.GetEntities().With<ExpirationEvent>().AsSet();
    }

    public void System()
    {
        var tick = World.Get<Ticks>().Tick;
        foreach (var entity in ExpirationEvents.GetEntities())
        {
            var expiration = entity.Get<ExpirationEvent>();
            if (expiration.Tick <= tick)
            {
                var queued = (entity, default(DefaultEcs.Entity), expiration);

                World.Set(new EventQueue()
                {
                    Events = World.Get<EventQueue>().Events.With(queued)
                });
            }
        }
    }
}