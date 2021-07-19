using DefaultEcs;
using System;
using System.Linq;

public static class Target
{
    public static int Other = -1;
    public static int Self = -2;
}

public record Task
{
    public DefaultEcs.Entity Target;
    public bool TargetSelf;
    public bool TargetOther;

    public virtual void Execute(DefaultEcs.Entity entity)
    {
    }
}

public record Event
{
    public Task[] Tasks = new Task[] { };

    public Event(params Task[] tasks)
        => (Tasks) = (tasks);

    public override string ToString()
    {
        return $"{this.GetType().Name} {{ {Utils.Log(nameof(Tasks), Tasks)} }}";
    }
}

public record Add : Task
{
    public object Component;

    public Add() { }

    public Add(object component)
        => (Component) = (component);

    override public void Execute(DefaultEcs.Entity entity)
    {
        entity.Set(Component);
    }
}

public record Remove<T> : Task
{
    override public void Execute(DefaultEcs.Entity entity)
    {
        entity.Remove<T>();
    }
}

public record AddEntity : Task
{
    public object[] Components;

    public AddEntity() { }

    public AddEntity(object[] components)
        => (Components) = (components);

    override public void Execute(DefaultEcs.Entity entity)
    {
        foreach (var component in Components)
        {
            entity.Set(component);
        }
    }
}

public record RemoveEntity : Task
{
    override public void Execute(DefaultEcs.Entity entity)
    {
        entity.Dispose();
    }
}

public record EventQueue
{
    public (DefaultEcs.Entity Source, DefaultEcs.Entity Target, Event Event)[] Events =
        new (DefaultEcs.Entity Source, DefaultEcs.Entity Target, Event Event)[] { };

    public EventQueue(params (DefaultEcs.Entity Source, DefaultEcs.Entity Target, Event Event)[] queue)
        => (Events) = (queue);

    public override string ToString()
    {
        return $"{this.GetType().Name} {{ {Utils.Log(nameof(Events), Events)} }}";
    }
}

public class Events
{
    public static int ENTITY = 2;

    private DefaultEcs.World world;

    public Events(DefaultEcs.World world)
    {
        this.world = world;
        world.Set(new EventQueue());
    }

    public void System()
    {
        var eventQueue = world.Get<EventQueue>();
        if (eventQueue?.Events?.Count() == 0) return;

        foreach (var queued in eventQueue.Events)
        {
            var (entity, otherEntity, @event) = queued;

            foreach (var task in @event.Tasks)
            {
                DefaultEcs.Entity target = task.Target;

                if (task.TargetOther)
                {
                    target = otherEntity;
                }

                if (task.TargetSelf)
                {
                    target = entity;
                }

                task.Execute(target);
            }
        }

        eventQueue.Events = new (DefaultEcs.Entity Source, DefaultEcs.Entity Target, Event Event)[] { };
    }
}