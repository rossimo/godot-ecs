using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections.Generic;

namespace SimpleEcs
{
    public static class Logger
    {
        public static BlockingCollection<(SimpleEcs.State Previous, SimpleEcs.State Next, IEnumerable<int> ignore)> Queue =
                new BlockingCollection<(SimpleEcs.State Previous, SimpleEcs.State Next, IEnumerable<int> ignore)>();

        public static Thread LogThread = new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    var (Previous, State, ignore) = Queue.Take();
                    if (Previous == State) continue;

                    var types = Previous.Types()
                        .Concat(State.Types())
                        .Distinct()
                        .Where(type => ignore?.Contains(type) == false);

                    var diffs = new List<SimpleEcs.Result<SimpleEcs.Component>>();
                    foreach (var type in types)
                    {
                        diffs.Add(Diff.Compare(type, Previous, State));
                    }

                    IEnumerable<(int ID, string Message)> all = new List<(int, string)>();
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

        public static void Log(State Previous, State State, IEnumerable<int> ignore = null)
        {
            if (!LogThread.IsAlive) {
                LogThread.Start();
            }

            Queue.Add((Previous, State, ignore));
        }
    }
}