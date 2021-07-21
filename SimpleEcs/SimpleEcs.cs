using System;
using System.Linq;
using System.Collections.Generic;

namespace SimpleEcs
{
    public record Component();

    public static class ComponentUtils<T>
    {
        public static int Index = typeof(T).Name.GetHashCode();
    }

    public class State
    {
        private static readonly Dictionary<int, Component> BLANK = new Dictionary<int, Component>();

        public bool Logging;
        public IEnumerable<int> LoggingIgnore;
        private Dictionary<int, Dictionary<int, Component>> Components;

        public State()
        {
            Components = new Dictionary<int, Dictionary<int, Component>>();
            LoggingIgnore = new int[] { };
            Logging = false;
        }

        public State(State state)
        {
            Components = new Dictionary<int, Dictionary<int, Component>>(state.Components);
            LoggingIgnore = state.LoggingIgnore;
            Logging = state.Logging;
        }

        public IEnumerable<int> Types()
        {
            return Components.Keys;
        }

        public Dictionary<int, Component> Get<C1>()
            where C1 : Component
        {
            return GetComponent(ComponentUtils<C1>.Index);
        }

        public C1 Get<C1>(int entityId)
            where C1 : Component
        {
            Components.TryGetValue(ComponentUtils<C1>.Index, out var components);
            if (components == null) return null;

            components.TryGetValue(entityId, out var component);
            return component as C1;
        }

        public Dictionary<int, Component> GetComponent(int componentId)
        {
            Components.TryGetValue(componentId, out var components);
            return components == null
                ? BLANK
                : components;
        }

        public State With(int componentId, int entityId, Component component)
        {
            var prev = this;
            var state = this;

            Components.TryGetValue(componentId, out var components);

            if (components == null)
            {
                state = new State(state);
                state.Components[componentId] = new Dictionary<int, Component> { { entityId, component } };
            }
            else
            {
                Components[componentId].TryGetValue(componentId, out var existing);
                if (existing == null || existing != component)
                {
                    state = new State(state);
                    state.Components[componentId] = new Dictionary<int, Component>(Components[componentId]);
                    state.Components[componentId][entityId] = component;
                }
                else
                {
                    return this;
                }
            }

            if (Logging)
            {
                Logger.Log(prev, state, LoggingIgnore);
            }

            return state;
        }

        public State With<C>(int entityId, C component)
         where C : Component
        {
            return With(ComponentUtils<C>.Index, entityId, component);
        }

        public State With(int entityId, params Component[] components)
        {

            var state = this;
            foreach (var component in components)
            {
                var componentId = component.GetType().Name.GetHashCode();

                state = state.With(componentId, entityId, component);
            }

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
                state = state.Without(componentId, entityId);
            }
            return state;
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

            if (Logging)
            {
                Logger.Log(prev, state, LoggingIgnore);
            }

            return state;
        }

        public State Without<C>(int entityId)
            where C : Component
        {
            return Without(ComponentUtils<C>.Index, entityId);
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
        where C : Component;

    public class Diff<C>
        where C : Component
    {
        private static readonly IEnumerable<(int ID, C Component)> BLANK = new (int ID, C Component)[] { };

        public static Result<C> Compare(int componentId, State before, State after)
        {
            if (before == after)
            {
                return new Result<C>(
                    Added: BLANK,
                    Removed: BLANK,
                    Changed: BLANK);
            }

            var oldComponents = before.GetComponent(componentId);
            var newComponents = after.GetComponent(componentId);

            var oldIds = oldComponents?.Keys;
            var newIds = newComponents?.Keys;
            var changeIds = (oldIds != null && newIds != null)
                ? newIds.Intersect(oldIds).Where(id => oldComponents[id] != newComponents[id])
                : null;

            return new Result<C>(
                Added: (oldIds != null && newIds != null)
                    ? newIds.Except(oldIds).Select(id => (id, newComponents[id] as C))
                    : newIds?.Select(id => (id, newComponents[id] as C)) ?? BLANK,
                Removed: (oldIds != null && newIds != null)
                    ? oldIds.Except(newIds).Select(id => (id, oldComponents[id] as C))
                    : oldIds?.Select(id => (id, oldComponents[id] as C)) ?? BLANK,
                Changed: changeIds?.Select(id => (id, newComponents[id] as C)) ?? BLANK);
        }

        public static Result<C> Compare(State before, State after)
        {
            var componentId = ComponentUtils<C>.Index;
            return Compare(componentId, before, after);
        }
    }
}