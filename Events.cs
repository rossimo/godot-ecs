using DefaultEcs;
using System;
using System.Linq;

public interface Target
{
    public Entity Find(Entity self, Entity other);

    public static Target Self = new Self();

    public static Target Other = new Other();

    public static Func<Entity, Target> Specific = (Entity Entity) =>
    {
        return new TargetSpecific()
        {
            Target = Entity
        };
    };
}

public class Self : Target
{
    public Entity Find(Entity self, Entity other)
    {
        return self;
    }
}

public class Other : Target
{
    public Entity Find(Entity self, Entity other)
    {
        return other;
    }
}

public class TargetSpecific : Target
{
    public Entity Target;

    public Entity Find(Entity self, Entity other)
    {
        return Target;
    }
}

public record Task
{
    public Target Target = Target.Self;

    public virtual void Execute(DefaultEcs.World world, DefaultEcs.Entity entity)
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

    private static System.Reflection.MethodInfo SetGeneric = typeof(Entity).GetMethods()
        .Single(method => method.Name == "Set" && method.GetParameters().Length == 1);

    public Add() { }

    public Add(object component)
        => (Component) = (component);

    override public void Execute(DefaultEcs.World world, DefaultEcs.Entity entity)
    {
        var type = Component.GetType();
        var set = SetGeneric.MakeGenericMethod(new[] { type });
        set.Invoke(entity, new[] { Component });
    }
}

public record Remove : Task
{
    public Type Type;

    private static System.Reflection.MethodInfo RemoveGeneric = typeof(Entity).GetMethod("Remove");

    override public void Execute(DefaultEcs.World world, DefaultEcs.Entity entity)
    {
        var remove = RemoveGeneric.MakeGenericMethod(new[] { Type });
        remove.Invoke(entity, new object[] { });
    }
}

public record AddEntity : Task
{
    private static System.Reflection.MethodInfo SetGeneric = typeof(Entity).GetMethods()
        .Single(method => method.Name == "Set" && method.GetParameters().Length == 1);

    public object[] Components;

    public AddEntity() { }

    public AddEntity(object[] components)
        => (Components) = (components);

    override public void Execute(DefaultEcs.World world, DefaultEcs.Entity entity)
    {
        foreach (var component in Components)
        {
            var type = component.GetType();
            var set = SetGeneric.MakeGenericMethod(new[] { type });
            set.Invoke(entity, new[] { component });
        }
    }
}

public record RemoveEntity : Task
{
    override public void Execute(DefaultEcs.World world, DefaultEcs.Entity entity)
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

    private DefaultEcs.World World;

    public Events(DefaultEcs.World world)
    {
        World = world;
        world.Set(new EventQueue());
    }

    public void System()
    {
        var eventQueue = World.TryGet<EventQueue>();
        if (eventQueue?.Events?.Count() == 0) return;

        foreach (var queued in eventQueue.Events)
        {
            var (self, other, @event) = queued;

            foreach (var task in @event.Tasks)
            {
                task.Execute(World, task.Target.Find(self, other));
            }
        }

        eventQueue.Events = new (DefaultEcs.Entity Source, DefaultEcs.Entity Target, Event Event)[] { };
        World.Set(eventQueue);
    }
}