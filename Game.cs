using Ecs;
using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public class Game : Godot.YSort
{
    public State State;
    private State Previous;
    private int Tick;
    private List<Event> EventQueue = new List<Event>();

    public override void _Ready()
    {
        State = new State() {
            { "hero", new Entity(
                new Player { },
                new Speed { Value = 4f },
                new Inventory { },
                new Position{ X = 50, Y = 50 },
                new Scale { X = 3, Y = 3 },
                new EnterEvent(),
                new Sprite { Image = "res://resources/tiles/tile072.png" },
                new Collision())},
            { "potion", Potion },
            { "fire", new Entity(
                new Position { X = 400, Y = 200 },
                new EnterEvent(
                    new Add(new Flash { Color = new Color { Red = 1f, Green = 0f, Blue = 0f } }, Target.Other),
                    new Add(new Flash { Color = new Color { Red = 2f, Green = 2f, Blue = 0f } })
                ),
                new Scale { X = 2, Y = 2 },
                new Sprite { Image = "res://resources/tiles/tile495.png" })},
            { "button", new Entity(
                new Position { X = 300, Y = 300 },
                new EnterEvent(
                    new Add(new Flash { Color = new Color { Red = 0.1f, Green = 0.1f, Blue = 0.1f } }),
                    new AddEntity(Potion, "potion")
                ),
                new Scale { X = 2, Y = 2 },
                new Sprite { Image = "res://resources/tiles/tile481.png" })}
        };

        State.Log(null, State);
    }

    public static Entity Potion = new Entity(
        new Position { X = 200, Y = 300 },
        new EnterEvent(new RemoveEntity()),
        new Flash { Color = new Color { Red = 2f, Green = 2f, Blue = 2f } },
        new Scale { X = 2, Y = 2 },
        new Sprite { Image = "res://resources/tiles/tile570.png" });

    public override void _Input(InputEvent @event)
    {
        State = Input.System(State, this, @event);
    }

    public void Event(string id, string otherId, Event ev)
    {
        otherId = otherId?.Split("-").FirstOrDefault();

        Func<Task, Task> withTick = (Task task) =>
        {
            return task is Add add && add.Component is TickComponent tickComponent
                ? add with { Component = tickComponent with { Tick = Tick } }
                : task;
        };

        EventQueue.Add(ev with
        {
            Tasks = ev.Tasks.Select(task =>
            {
                var target = id;

                if (task.Target == Target.Other)
                {
                    if (otherId?.Length > 0)
                    {
                        target = otherId;
                    }
                    else
                    {
                        return withTick(task);
                    }
                }
                else if (task.Target == Target.Self)
                {
                    target = id;
                }
                else if (task.Target?.Length > 0)
                {
                    target = task.Target;
                }

                task = task with { Target = target };

                return withTick(task);
            }).ToArray()
        });
    }

    public void _Event(string id, GodotWrapper ev)
    {
        Event(id, null, ev.Get<Event>());
    }

    public void _Event(Node other, string id, GodotWrapper ev)
    {
        Event(id, other.GetParent().Name, ev.Get<Event>());
    }

    public override void _PhysicsProcess(float delta)
    {
        foreach (var ev in EventQueue)
        {
            State = Events.System(Tick, State, ev);
        }
        EventQueue.Clear();

        Renderer.System(Previous, State, this);
        State = Physics.System(Previous, State, this, delta);

        Previous = State;
        Tick = Tick + 1;
        GC.Collect();
    }
}
