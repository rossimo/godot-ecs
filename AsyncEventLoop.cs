using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

public interface Cancellable
{
    void Cancel();
}

public class TaskContext : Cancellable
{
    public bool Running = true;
    public List<Cancellable> Listeners = new List<Cancellable>();

    public void Cancel()
    {
        Running = false;
        foreach (var listener in Listeners)
        {
            listener.Cancel();
        }
    }
}

public interface Taskable
{
    Task Create(TaskContext ctx);
}

public interface Taskable<T> : Taskable
{
    Task<T> CreateAs(TaskContext ctx);
}

public class EventLoopAwaiter<T> : INotifyCompletion, Cancellable
{
    public Action Continuation;
    public Action Cleanup;
    public bool Completed;
    public T Result;

    public void OnCompleted(Action continuation)
    {
        this.Continuation = continuation;

        if (IsCompleted)
        {
            if (Cleanup != null)
            {
                Cleanup();
            }

            continuation();
        }
    }

    public void Next()
    {
        var context = SynchronizationContext.Current;
        if (context == null)
        {
            if (Cleanup != null)
            {
                Cleanup();
            }

            Continuation();
        }
        else
        {
            context.Post(_ =>
            {
                if (Cleanup != null)
                {
                    Cleanup();
                }

                Continuation();
            }, null);
        }
    }

    public bool IsCompleted
    {
        get
        {
            return Completed;
        }
    }

    public T GetResult()
    {
        return Result;
    }

    public void Cancel()
    {
        if (Cleanup != null)
        {
            Cleanup();
        }
    }
}

public class EventLoopScheduler : TaskScheduler
{
    private List<Task> tasksCollection = new List<Task>();

    public void Execute()
    {
        foreach (var task in tasksCollection.ToArray())
        {
            TryExecuteTask(task);
        }

        tasksCollection.RemoveAll(el => el.IsCanceled || el.IsCompleted || el.IsFaulted);
    }

    protected override IEnumerable<Task> GetScheduledTasks()
    {
        return tasksCollection.ToArray();
    }

    protected override void QueueTask(Task task)
    {
        if (task != null)
        {
            tasksCollection.Add(task);
        }
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        return false;
    }
}

public sealed class EventLoopSynchronizationContext : SynchronizationContext
{
    private static readonly ConcurrentQueue<Message> Queue;

    static EventLoopSynchronizationContext()
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
        return new EventLoopSynchronizationContext();
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

public static class EventLoop
{
    public static readonly EventLoopScheduler Scheduler;
    private static TaskFactory Factory;

    static EventLoop()
    {
        SynchronizationContext.SetSynchronizationContext(new EventLoopSynchronizationContext());
        Scheduler = new EventLoopScheduler();
        Factory = new TaskFactory(Scheduler);
    }

    public static Task Run(Func<Task> func)
    {
        return Factory
            .StartNew(() => func(), CancellationToken.None, TaskCreationOptions.DenyChildAttach, Scheduler)
            .Unwrap();
    }

    public static Task<T> Run<T>(Func<Task<T>> func)
    {
        return Factory
            .StartNew(() => func(), CancellationToken.None, TaskCreationOptions.DenyChildAttach, Scheduler)
            .Unwrap();
    }

    public static Task Run(Action action)
    {
        return Factory
            .StartNew(action, CancellationToken.None, TaskCreationOptions.DenyChildAttach, Scheduler);
    }

    public static Task<T> Run<T>(Func<T> func)
    {
        return Factory
            .StartNew(func, CancellationToken.None, TaskCreationOptions.DenyChildAttach, Scheduler);
    }

    public static async Task<Task> WhenAny(TaskContext ctx, params Taskable[] taskables)
    {
        var child = new TaskContext();
        ctx.Listeners.Add(child);

        try
        {
            var result = await Task.WhenAny(taskables.Select(taskable => taskable.Create(child)));

            if (result.Exception != null)
            {
                Console.WriteLine(result.Exception);
            }

            return result;
        }
        finally
        {
            child.Cancel();
            ctx.Listeners.Remove(child);
        }
    }
}