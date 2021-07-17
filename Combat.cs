using DefaultEcs;
using System.Linq;
using System.Collections.Generic;

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
    private DefaultEcs.EntitySet Ticks;
    private DefaultEcs.EntitySet EventQueue;

    public Combat(DefaultEcs.World world)
    {
        ExpirationEvents = world.GetEntities().With<ExpirationEvent>().AsSet();
        Ticks = world.GetEntities().With<Ticks>().AsSet();
        EventQueue = world.GetEntities().With<EventQueue>().AsSet();
    }

    public void System()
    {
        var tick = Ticks.GetEntities()[0].Get<Ticks>().Tick;
        var eventQueue = EventQueue.GetEntities()[0];

        foreach (var entity in ExpirationEvents.GetEntities())
        {
            var expiration = entity.Get<ExpirationEvent>();
            if (expiration.Tick <= tick)
            {
                var queued = (entity, -3, expiration);

                entity.Remove<ExpirationEvent>();
                eventQueue.Set(new EventQueue()
                {
                    Events = eventQueue.Get<EventQueue>().With(queued)
                });
            }
        }
    }
}