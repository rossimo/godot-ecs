using System;
using System.Linq;
using System.Collections.Generic;

namespace Ecs
{
    public record Component();

    public class State
    {
        private Dictionary<int, Dictionary<int, Component>> Components = new Dictionary<int, Dictionary<int, Component>>();

        public static IEnumerable<int> LOGGING_IGNORE = new[] { typeof(Ticks).Name.GetHashCode() };

        public State()
        {
        }

        public State(State state)
        {
            Components = new Dictionary<int, Dictionary<int, Component>>(state.Components);
        }

        public IEnumerable<int> Types()
        {
            return Components.Keys;
        }

        public IEnumerable<(int, C1)> GetAll<C1>(int componentId)
            where C1 : Component
        {
            return GetComponent(componentId).Select(entry => (entry.Key, entry.Value as C1));
        }

        public IEnumerable<(int, C1)> Get<C1>()
            where C1 : Component
        {
            return GetAll<C1>(typeof(C1).Name.GetHashCode());
        }

        public C1 Get<C1>(int componentId, int entityId)
            where C1 : Component
        {
            return Components.ContainsKey(componentId) &&
                Components[componentId].ContainsKey(entityId)
                ? Components[componentId][entityId] as C1
                : null;
        }

        public C1 Get<C1>(int entityId)
            where C1 : Component
        {
            var componentId = typeof(C1).Name.GetHashCode();
            return Get<C1>(componentId, entityId);
        }

        public (C1, C2) Get<C1, C2>(int componentId1, int componentId2, int entityId)
            where C1 : Component
            where C2 : Component
        {
            return (Get<C1>(componentId1, entityId), Get<C2>(componentId2, entityId));
        }

        public (C1, C2) Get<C1, C2>(int entityId)
            where C1 : Component
            where C2 : Component
        {
            return (Get<C1>(typeof(C1).Name.GetHashCode(), entityId), Get<C2>(typeof(C2).Name.GetHashCode(), entityId));
        }

        public Dictionary<int, Component> GetComponent(int componentId)
        {
            if (!Components.ContainsKey(componentId))
            {
                return new Dictionary<int, Component>();
            }
            return Components[componentId];
        }

        public Boolean ContainsKey(int entityId)
        {
            foreach (var component in Components.Values)
            {
                if (component.ContainsKey(entityId))
                {
                    return true;
                }
            }

            return false;
        }

        public State With(int entityId, params Component[] components)
        {
            var prev = this;
            var state = this;
            foreach (var component in components)
            {
                var componentId = component.GetType().Name.GetHashCode();

                if (state.Components.ContainsKey(componentId) &&
                    state.Components[componentId].ContainsKey(entityId) &&
                    state.Components[componentId][entityId] == component)
                {
                    continue;
                }

                state = new State(state);

                state.Components[componentId] = state.Components.ContainsKey(componentId)
                    ? new Dictionary<int, Component>(state.Components[componentId])
                    : new Dictionary<int, Component>(1);
                state.Components[componentId][entityId] = component;
            }
            Logger.Log(prev, state, State.LOGGING_IGNORE);

            return state;
        }

        public State Batch<C>(Dictionary<int, C> update)
            where C : Component
        {
            var componentId = typeof(C).Name.GetHashCode();

            var state = new State(this);

            if (!state.Components.ContainsKey(componentId))
            {
                state.Components[componentId] = new Dictionary<int, Component>(1);
            }
            else
            {
                state.Components[componentId] = new Dictionary<int, Component>(state.Components[componentId]);
            }

            foreach (var entry in update)
            {
                state.Components[componentId][entry.Key] = entry.Value;
            }

            return state;
        }

        public State Without(int entityId)
        {
            var prev = this;
            var state = this;
            foreach (var componentId in state.Components.Keys)
            {
                state = state.Without(componentId.GetHashCode(), entityId);
            }
            return state;
        }

        public State Without<C>(int entityId) where C : Component
        {
            return this.Without(typeof(C).Name.GetHashCode(), entityId);
        }

        public State Without(int componentId, int entityId)
        {
            if (!Components.ContainsKey(componentId) ||
                !Components[componentId].ContainsKey(entityId))
            {
                return this;
            }

            var prev = this;
            var state = new State(this);
            state.Components[componentId] = new Dictionary<int, Component>(state.Components[componentId]);
            state.Components[componentId].Remove(entityId);

            if (state.Components[componentId].Count() == 0)
            {
                state.Components.Remove(componentId);
            }
            Logger.Log(prev, state, State.LOGGING_IGNORE);
            return state;
        }
    }

    public static class Utils
    {
        private static string SPACER = "    ";
        private static int DEPTH = 0;

        public static string Log<V>(string name, IEnumerable<V> list)
        {
            var indent = String.Join("", Enumerable.Range(0, DEPTH + 1).Select(i => SPACER));

            DEPTH++;
            var root = $"\n{indent}{String.Join(",\n" + indent, (V[])list).Trim()}{indent}\n";
            DEPTH--;

            var trimmedRoot = list.Count() > 0
                ? root
                : root.Trim();

            var suffix = list.Count() > 0
                ? String.Join("", Enumerable.Range(0, DEPTH).Select(i => SPACER))
                : "";

            return $"{name} = [{trimmedRoot}{suffix}]";
        }

        public static V[] With<V>(this V[] list, V value)
        {
            return list.Concat(new[] { value }).ToArray();
        }
    }

    public record Result<C>(
        IEnumerable<(int ID, C Component)> Added,
        IEnumerable<(int ID, C Component)> Removed,
        IEnumerable<(int ID, C Component)> Changed)
        where C : Component
    {
        public Result<D> To<D>() where D : Component
        {
            return new Result<D>(
                Added: Added.Select(entry => (entry.ID, entry.Component as D)),
                Removed: Removed.Select(entry => (entry.ID, entry.Component as D)),
                Changed: Changed.Select(entry => (entry.ID, entry.Component as D)));
        }
    }

    public class Diff
    {
        public static Result<Component> Compare(int componentId, State before, State after)
        {
            if (before == after || before.GetComponent(componentId) == after.GetComponent(componentId))
            {
                return new Result<Component>(
                    Added: new (int ID, Component Component)[] { },
                    Removed: new (int ID, Component Component)[] { },
                    Changed: new (int ID, Component Component)[] { });
            }

            var oldComponents = before.GetComponent(componentId);
            var newComponents = after.GetComponent(componentId);

            var oldIds = oldComponents.Keys.ToHashSet();
            var newIds = newComponents.Keys.ToHashSet();
            var changeIds = newIds.Intersect(oldIds).Where(id => oldComponents[id] != newComponents[id]);

            return new Result<Component>(
                Added: newIds.Except(oldIds).Select(id => (id, newComponents[id])),
                Removed: oldIds.Except(newIds).Select(id => (id, oldComponents[id])),
                Changed: changeIds.Select(id => (id, newComponents[id])));
        }

        public static Result<Component> Compare(Type type, State before, State after)
        {
            return Compare(type.Name.GetHashCode(), before, after);
        }

        public static Result<C1> Compare<C1>(State Current, State next)
            where C1 : Component
        {
            var changes1 = Compare(typeof(C1), Current, next).To<C1>();
            return changes1;
        }

        public static (Result<C1>, Result<C2>) Compare<C1, C2>(State Current, State next)
            where C1 : Component
            where C2 : Component
        {
            var changes1 = Compare(typeof(C1), Current, next).To<C1>();
            var changes2 = Compare(typeof(C2), Current, next).To<C2>();
            return (changes1, changes2);
        }

        public static (Result<C1>, Result<C2>, Result<C3>) Compare<C1, C2, C3>(State Current, State next)
            where C1 : Component
            where C2 : Component
            where C3 : Component
        {
            var changes1 = Compare(typeof(C1), Current, next).To<C1>();
            var changes2 = Compare(typeof(C2), Current, next).To<C2>();
            var changes3 = Compare(typeof(C3), Current, next).To<C3>();
            return (changes1, changes2, changes3);
        }

        public static (Result<C1>, Result<C2>, Result<C3>, Result<C4>) Compare<C1, C2, C3, C4>(State Current, State next)
            where C1 : Component
            where C2 : Component
            where C3 : Component
            where C4 : Component
        {
            var changes1 = Compare(typeof(C1), Current, next).To<C1>();
            var changes2 = Compare(typeof(C2), Current, next).To<C2>();
            var changes3 = Compare(typeof(C3), Current, next).To<C3>();
            var changes4 = Compare(typeof(C4), Current, next).To<C4>();
            return (changes1, changes2, changes3, changes4);
        }

        public static (Result<C1>, Result<C2>, Result<C3>, Result<C4>, Result<C5>) Compare<C1, C2, C3, C4, C5>(State Current, State next)
            where C1 : Component
            where C2 : Component
            where C3 : Component
            where C4 : Component
            where C5 : Component
        {
            var changes1 = Compare(typeof(C1), Current, next).To<C1>();
            var changes2 = Compare(typeof(C2), Current, next).To<C2>();
            var changes3 = Compare(typeof(C3), Current, next).To<C3>();
            var changes4 = Compare(typeof(C4), Current, next).To<C4>();
            var changes5 = Compare(typeof(C5), Current, next).To<C5>();
            return (changes1, changes2, changes3, changes4, changes5);
        }

        public static (Result<C1>, Result<C2>, Result<C3>, Result<C4>, Result<C5>, Result<C6>) Compare<C1, C2, C3, C4, C5, C6>(State Current, State next)
            where C1 : Component
            where C2 : Component
            where C3 : Component
            where C4 : Component
            where C5 : Component
            where C6 : Component
        {
            var changes1 = Compare(typeof(C1), Current, next).To<C1>();
            var changes2 = Compare(typeof(C2), Current, next).To<C2>();
            var changes3 = Compare(typeof(C3), Current, next).To<C3>();
            var changes4 = Compare(typeof(C4), Current, next).To<C4>();
            var changes5 = Compare(typeof(C5), Current, next).To<C5>();
            var changes6 = Compare(typeof(C6), Current, next).To<C6>();
            return (changes1, changes2, changes3, changes4, changes5, changes6);
        }

        public static (Result<C1>, Result<C2>, Result<C3>, Result<C4>, Result<C5>, Result<C6>, Result<C7>) Compare<C1, C2, C3, C4, C5, C6, C7>(State Current, State next)
            where C1 : Component
            where C2 : Component
            where C3 : Component
            where C4 : Component
            where C5 : Component
            where C6 : Component
            where C7 : Component
        {
            var changes1 = Compare(typeof(C1), Current, next).To<C1>();
            var changes2 = Compare(typeof(C2), Current, next).To<C2>();
            var changes3 = Compare(typeof(C3), Current, next).To<C3>();
            var changes4 = Compare(typeof(C4), Current, next).To<C4>();
            var changes5 = Compare(typeof(C5), Current, next).To<C5>();
            var changes6 = Compare(typeof(C6), Current, next).To<C6>();
            var changes7 = Compare(typeof(C7), Current, next).To<C7>();
            return (changes1, changes2, changes3, changes4, changes5, changes6, changes7);
        }

        public static (Result<C1>, Result<C2>, Result<C3>, Result<C4>, Result<C5>, Result<C6>, Result<C7>, Result<C8>) Compare<C1, C2, C3, C4, C5, C6, C7, C8>(State Current, State next)
            where C1 : Component
            where C2 : Component
            where C3 : Component
            where C4 : Component
            where C5 : Component
            where C6 : Component
            where C7 : Component
            where C8 : Component
        {
            var changes1 = Compare(typeof(C1), Current, next).To<C1>();
            var changes2 = Compare(typeof(C2), Current, next).To<C2>();
            var changes3 = Compare(typeof(C3), Current, next).To<C3>();
            var changes4 = Compare(typeof(C4), Current, next).To<C4>();
            var changes5 = Compare(typeof(C5), Current, next).To<C5>();
            var changes6 = Compare(typeof(C6), Current, next).To<C6>();
            var changes7 = Compare(typeof(C7), Current, next).To<C7>();
            var changes8 = Compare(typeof(C8), Current, next).To<C8>();
            return (changes1, changes2, changes3, changes4, changes5, changes6, changes7, changes8);
        }

        public static (Result<C1>, Result<C2>, Result<C3>, Result<C4>, Result<C5>, Result<C6>, Result<C7>, Result<C8>, Result<C9>) Compare<C1, C2, C3, C4, C5, C6, C7, C8, C9>(State Current, State next)
            where C1 : Component
            where C2 : Component
            where C3 : Component
            where C4 : Component
            where C5 : Component
            where C6 : Component
            where C7 : Component
            where C8 : Component
            where C9 : Component
        {
            var changes1 = Compare(typeof(C1), Current, next).To<C1>();
            var changes2 = Compare(typeof(C2), Current, next).To<C2>();
            var changes3 = Compare(typeof(C3), Current, next).To<C3>();
            var changes4 = Compare(typeof(C4), Current, next).To<C4>();
            var changes5 = Compare(typeof(C5), Current, next).To<C5>();
            var changes6 = Compare(typeof(C6), Current, next).To<C6>();
            var changes7 = Compare(typeof(C7), Current, next).To<C7>();
            var changes8 = Compare(typeof(C8), Current, next).To<C8>();
            var changes9 = Compare(typeof(C9), Current, next).To<C9>();
            return (changes1, changes2, changes3, changes4, changes5, changes6, changes7, changes8, changes9);
        }
    }
}