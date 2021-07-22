using System;
using System.Collections.Generic;
using Loyc.Collections;
using System.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public State(State state)
        {
            Components = state.Components.Clone();
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public State With(int componentId, int entityId, Component component)
        {
            var prev = this;
            var state = this;

            Components.TryGetValue(componentId, out var components);
            if (components == null)
            {
                state = new State(state);
                var newComponents = new Dictionary<int, Component>();
                newComponents.Add(entityId, component);
                state.Components.Add(componentId, newComponents);
            }
            else
            {
                components.TryGetValue(entityId, out var existing);
                if (existing != component)
                {
                    state = new State(state);
                    var newComponents = state.Components[componentId] = components.Clone();
                    newComponents[entityId] = component;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public State With<C>(int entityId, C component)
         where C : Component
        {
            return With(ComponentUtils<C>.Index, entityId, component);
        }

        public State With(int entityId, params Component[] components)
        {

            var state = this;
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                var componentId = component.GetType().Name.GetHashCode();

                state = state.With(componentId, entityId, component);
            }

            return state;
        }

        public State Batch<C>(Dictionary<int, C> update)
            where C : Component
        {
            var componentId = ComponentUtils<C>.Index;

            var state = new State(this);

            Dictionary<int, Component> newComponents;

            if (!state.Components.ContainsKey(componentId))
            {
                newComponents = state.Components[componentId] = new Dictionary<int, Component>();
            }
            else
            {
                newComponents = state.Components[componentId] = state.Components[componentId].Clone();
            }

            foreach (var entry in update)
            {
                newComponents[entry.Key] = entry.Value;
            }

            return state;
        }

        public State Without(int entityId)
        {
            var prev = this;
            var state = this;
            foreach (var componentId in state.Components.Keys)
            {
                if (state.Components[componentId].ContainsKey(entityId))
                {
                    if (state == prev)
                    {
                        state = new State(prev);
                    }

                    var newComponents = state.Components[componentId] = state.Components[componentId].Clone();
                    newComponents.Remove(entityId);
                }
            }

            if (Logging)
            {
                Logger.Log(prev, state, LoggingIgnore);
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
            var newComponents = state.Components[componentId] = state.Components[componentId].Clone();
            newComponents.Remove(entityId);

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
            return list.ConcatNow(new[] { value }).ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<int, T> Clone<T>(this Dictionary<int, T> self)
        {
            return new Dictionary<int, T>(self);
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
            var newIds = newComponents?.Keys.ToHashSet();
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