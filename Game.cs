using Godot;
using System.Linq;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.Threading;
using System.Threading.Tasks;

public class Game : Godot.YSort
{
    public EcsWorld world = new EcsWorld();
    public EcsSystems systems;
    public Shared shared;
    public InputSystem input;
    public FrameTimeSystem frameTime;

    public interface RunListener
    {
        void Run();
        void Cancel();
    }

    private EcsPool<PhysicsNode> physicsComponents;
    private EcsPool<AreaNode> areaComponents;
    private EcsPool<PositionTween> positions;
    private EcsPool<ModulateTween> modulates;
    private EcsPool<RenderNode> renders;
    public List<RunListener> RunListeners = new List<RunListener>();
    public GodotSynchronizationContext Context = new GodotSynchronizationContext();

    public override void _Ready()
    {
        physicsComponents = world.GetPool<PhysicsNode>();
        areaComponents = world.GetPool<AreaNode>();
        positions = world.GetPool<PositionTween>();
        modulates = world.GetPool<ModulateTween>();
        renders = world.GetPool<RenderNode>();

        shared = new Shared() { Game = this };

        frameTime = new FrameTimeSystem();
        input = new InputSystem();

        systems = new EcsSystems(world, shared);

        systems
            .Add(frameTime)
            .Add(input)
            .Add(new CombatSystem())
            .Add(new HealthSystem())
            .Add(new PhysicsSystem())
            .Add(new RendererSystem())
            .Add(new DeleteComponentSystem<Notify<AreaNode>>())
            .Add(new DeleteComponentSystem<Notify<Sprite>>())
            .Add(new DeleteComponentSystem<Notify<Flash>>())
            .Add(new DeleteComponentSystem<Notify<Area>>())
            .Add(new DeleteComponentSystem<Collision>())
            .Add(new DeleteEntitySystem())
            .Inject()
            .Init();

        foreach (var node in GetChildren().OfType<Godot.Node>())
        {
            DiscoverEntity(node);
        }

        systems.Init();
    }

    public override void _ExitTree()
    {
        foreach (var listener in RunListeners)
        {
            listener.Cancel();
        }
    }

    public int DiscoverEntity(Node node)
    {
        var components = node.ToComponents("components/");
        if (components.Length == 0) return -1;

        var entity = world.NewEntity();

        foreach (var component in components)
        {
            world.AddNotify(entity, component);
        }

        Node2D renderNode = null;
        KinematicBody2D physicsNode = null;
        Area2D areaNode = null;

        if (node is KinematicBody2D foundPhysics)
        {
            physicsNode = foundPhysics;
        }
        if (node is Area2D foundArea)
        {
            areaNode = foundArea;
        }
        else if (node is Node2D found)
        {
            renderNode = found;
        }

        if (areaNode == null)
        {
            foreach (var parent in new[] { physicsNode, renderNode })
            {
                var potential = parent?.GetChildren().ToArray<Godot.Node>().OfType<Area2D>().FirstOrDefault();
                if (potential != null)
                {
                    areaNode = potential;
                    break;
                }
            }
        }

        if (physicsNode == null)
        {
            foreach (var parent in new[] { renderNode })
            {
                var potential = parent?.GetChildren().ToArray<Godot.Node>().OfType<KinematicBody2D>().FirstOrDefault();
                if (potential != null)
                {
                    physicsNode = potential;
                    break;
                }
            }
        }

        if (physicsNode != null)
        {
            var position = physicsNode.GlobalPosition;
            if (physicsNode.GetParent() != this)
            {
                physicsNode.GetParent().RemoveChild(physicsNode);
                AddChild(physicsNode);
            }

            physicsNode.GlobalPosition = position;
            physicsNode.Scale *= renderNode.Scale;
            physicsNode.Rotation += renderNode.Rotation;

            physicsNode.SetEntity(world, entity);

            ref var physicsComponent = ref physicsComponents.Add(entity);
            physicsComponent.Node = physicsNode;
        }

        if (areaNode != null)
        {
            if (physicsNode != null && areaNode.GetParent() != physicsNode)
            {
                var position = areaNode.GlobalPosition;

                areaNode.GetParent().RemoveChild(areaNode);
                physicsNode.AddChild(areaNode);

                areaNode.GlobalPosition = position;
                areaNode.Scale *= renderNode.Scale;
                areaNode.Rotation += renderNode.Rotation;
            }

            areaNode.SetEntity(world, entity);

            ref var areaComponent = ref areaComponents.Add(entity);
            areaComponents.Notify(entity);
            areaComponent.Node = areaNode;
        }

        if (renderNode != null)
        {
            renderNode.SetEntity(world, entity);

            ref var render = ref renders.Add(entity);
            render.Node = renderNode;

            ref var position = ref positions.Add(entity);
            position.Tween = new Tween() { Name = "position" };
            renderNode.AddChild(position.Tween);

            ref var modulate = ref modulates.Add(entity);
            modulate.Tween = new Tween() { Name = "modulate" };
            renderNode.AddChild(modulate.Tween);
        }

        return entity;
    }

    public override void _Input(InputEvent @event)
    {
        input.Run(systems, @event);
    }

    public override void _PhysicsProcess(float deltaValue)
    {
        frameTime.Run(systems, deltaValue);
        systems.Run();

        foreach (var listener in RunListeners)
        {
            listener.Run();
        }

        GodotSynchronizationContext.Update();
    }

    public void AreaEvent(Node targetNode, GodotWrapper sourceWrapper)
    {
        var packedSource = sourceWrapper.Get<EcsPackedEntity>();
        int entity = -1;
        packedSource.Unpack(world, out entity);

        int other = targetNode.GetEntity(world);

        var pool = world.GetPool<Many<Event<Area>>>();
        if (pool.SafeHas(entity))
        {
            ref var events = ref pool.Get(entity);
            foreach (var ev in events)
            {
                ev.Add(world, entity, other);
            }
        }

        if (pool.SafeHas(other))
        {
            ref var events = ref pool.Get(other);
            foreach (var ev in events)
            {
                ev.Add(world, other, entity);
            }
        }
    }

    public sealed class GodotSynchronizationContext : SynchronizationContext
    {
        private static readonly ConcurrentQueue<Message> Queue;

        static GodotSynchronizationContext()
        {
            Queue = new ConcurrentQueue<Message>();
        }

        private static void Enqueue(SendOrPostCallback d, object state)
        {
            Queue.Enqueue(new Message(d, state));
        }

        public static void Update()
        {
            if (!Queue.Any())
                return;

            Message message;

            if (!Queue.TryDequeue(out message))
                return;

            message.Callback(message.State);
        }

        public override SynchronizationContext CreateCopy()
        {
            return new GodotSynchronizationContext();
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            Enqueue(d, state);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            Enqueue(d, state);
        }

        private sealed class Message
        {
            public Message(SendOrPostCallback callback, object state)
            {
                Callback = callback;
                State = state;
            }

            public SendOrPostCallback Callback { get; set; }
            public object State { get; set; }
        }
    }

    public static class GodotTasks
    {
        public static readonly TaskScheduler Scheduler;

        static GodotTasks()
        {
            var context = new GodotSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(context);
            Scheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }

        public static Task Run(Func<Task> func)
        {
            if (func == null)
                throw new ArgumentNullException("func");

            var task = Task.Factory
                .StartNew(func, CancellationToken.None, TaskCreationOptions.DenyChildAttach, Scheduler)
                .Unwrap();

            return task;
        }

        public static Task<T> Run<T>(Func<Task<T>> func)
        {
            if (func == null)
                throw new ArgumentNullException("func");

            var task = Task.Factory
                .StartNew(func, CancellationToken.None, TaskCreationOptions.DenyChildAttach, Scheduler)
                .Unwrap();

            return task;
        }

        public static Task Run(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var task = Task.Factory
                .StartNew(action, CancellationToken.None, TaskCreationOptions.DenyChildAttach, Scheduler);

            return task;
        }

        public static Task<T> Run<T>(Func<T> func)
        {
            if (func == null)
                throw new ArgumentNullException("func");

            var task = Task.Factory
                .StartNew(func, CancellationToken.None, TaskCreationOptions.DenyChildAttach, Scheduler);

            return task;
        }
    }
}
