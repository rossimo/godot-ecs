using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections.Generic;
using Ecs;

public static class Logger
{
    public static BlockingCollection<(State Previous, State Next, IEnumerable<string> ignore)> Queue =
            new BlockingCollection<(Ecs.State Previous, Ecs.State Next, IEnumerable<string> ignore)>();

    public static Thread LogThread = new Thread(new ThreadStart(() =>
        {
            while (true)
            {
                var (Previous, State, ignore) = Queue.Take();
                if (Previous == State) return;

                var types = new List<string>()
                    .Concat(Previous.Components.Keys)
                    .Concat(State.Components.Keys)
                    .Distinct()
                    .Where(type => ignore?.Contains(type) == false);

                var diffs = new List<Ecs.Result<Component>>();
                foreach (var type in types)
                {
                    diffs.Add(Diff.Compare(type, Previous, State));
                }

                IEnumerable<(string ID, string Message)> all = new List<(string, string)>();
                foreach (var (Added, Removed, Changed) in diffs)
                {
                    all = all
                        .Concat(Removed.Select(entry => (entry.ID, $"- {(entry.ID, entry.Component.GetType().Name)}")))
                        .Concat(Added.Select(entry => (entry.ID, $"+ {entry}")))
                        .Concat(Changed.Select(entry => (entry.ID, $"~ {entry}")));
                }

                foreach (var message in all.OrderBy(entry => entry.ID))
                {
                    Console.WriteLine(message.Message);
                }
            }
        }));

    static Logger()
    {
        //LogThread.Start();
    }

    public static void Log(State Previous, State State, IEnumerable<string> ignore = null)
    {
        //Queue.Add((Previous, State, ignore));
    }
}