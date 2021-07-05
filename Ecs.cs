using System;
using System.Linq;
using System.Collections.Generic;

namespace Ecs
{
    public record Component();

    public record Entity
    {
        private Dictionary<string, Component> Components = new Dictionary<string, Component>();

        public Entity() { }

        public Entity(IEnumerable<Component> components)
            => (Components) = (components.ToDictionary(component => component.GetType().Name));

        public Entity(params Component[] components)
            => (Components) = (components.ToDictionary(component => component.GetType().Name));

        public Component Get(string component)
        {
            return Components.Get(component);
        }

        public Component Get(Type type)
        {
            return Get(type.Name);
        }

        public bool Has(Type type)
        {
            return Components.ContainsKey(type.Name);
        }

        public bool Has<C>() where C : Component
        {
            return Has(typeof(C));
        }

        public C Get<C>() where C : Component
        {
            return Get(typeof(C).Name) as C;
        }

        public (C1, C2) Get<C1, C2>()
            where C1 : Component
            where C2 : Component
        {
            return (Get<C1>(), Get<C2>());
        }

        public (C1, C2, C3) Get<C1, C2, C3>()
            where C1 : Component
            where C2 : Component
            where C3 : Component
        {
            return (Get<C1>(), Get<C2>(), Get<C3>());
        }

        public (C1, C2, C3, C4) Get<C1, C2, C3, C4>()
            where C1 : Component
            where C2 : Component
            where C3 : Component
            where C4 : Component
        {
            return (Get<C1>(), Get<C2>(), Get<C3>(), Get<C4>());
        }

        public IEnumerable<Type> Types()
        {
            return Components.Values.Select(component => component.GetType());
        }

        public Entity With<C>(C component) where C : Component
        {
            var existing = Get<C>();

            if (existing != component)
            {
                var key = component.GetType().Name;
                var components = new Dictionary<string, Component>(Components);

                components[key] = component;

                return this with { Components = components };
            }
            else
            {
                return this;
            }
        }

        public Entity Without(string component)
        {
            if (Components.ContainsKey(component))
            {
                var components = new Dictionary<string, Component>(Components);
                components.Remove(component);
                return this with { Components = components };
            }

            return this;
        }

        public Entity Without<C>() where C : Component
        {
            return Without(typeof(C).Name);
        }
    }

    public class State : Dictionary<string, Entity>
    {
        public static IEnumerable<Type> LOGGING_IGNORE = new[] { typeof(Ticks) };

        public State() : base()
        {
        }

        public State(State state) : base(state)
        {
        }

        public State(Dictionary<string, Entity> state) : base(state)
        {
        }

        public State With(string id, Entity entity)
        {
            var prev = this;
            var next = new State(Utils.With(prev, id, entity));
            Logger.Log(prev, next, State.LOGGING_IGNORE);
            return next;
        }

        public State With(string id, params Component[] components)
        {
            var state = this;

            var entity = this.ContainsKey(id)
                ? this[id]
                : new Entity();

            foreach (var component in components)
            {
                entity = entity.With(component);
            }

            return state.With(id, entity);
        }

        public State With(string id, Func<Entity, Entity> transform)
        {
            var entity = this.ContainsKey(id)
                ? this[id]
                : new Entity();

            return this.With(id, transform(entity));
        }

        public IEnumerable<Type> Types()
        {
            var set = new HashSet<Type>();
            foreach (var entity in Values)
            {
                foreach (var type in entity.Types())
                {
                    set.Add(type);
                }
            }
            return set;
        }

        public State Without(string id)
        {
            var prev = this;
            var next = new State(prev);
            if (next.ContainsKey(id))
            {
                next.Remove(id);
            }
            Logger.Log(prev, next, State.LOGGING_IGNORE);
            return next;
        }

        public State Without<C>(string id) where C : Component
        {
            return With(id, this[id].Without<C>());
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

        public static V Get<K, V>(this Dictionary<K, V> dict, K key)
        {
            V val;
            dict.TryGetValue(key, out val);
            return val;
        }

        public static V[] With<V>(this V[] list, V value)
        {
            return list.Concat(new[] { value }).ToArray();
        }

        public static Dictionary<K, V> With<K, V>(this Dictionary<K, V> dict, K key, V value)
        {
            if (dict.ContainsKey(key) && dict[key].Equals(value))
            {
                return dict;
            }

            var clone = new Dictionary<K, V>(dict);
            clone[key] = value;
            return clone;
        }

        public static IEnumerable<(string ID, IEnumerable<Component> Components)> Get(this Dictionary<string, Entity> entities, params Type[] types)
        {
            return entities.Where(entry =>
            {
                foreach (var type in types)
                {
                    if (!entry.Value.Has(type))
                    {
                        return false;
                    }
                }
                return true;
            }).Select(entry =>
            {
                var (id, entity) = entry;
                var components = types.Select(type => entity.Get(type.Name));
                return (id, components);
            });
        }

        public static IEnumerable<(string, C1)> Get<C1>(this Dictionary<string, Entity> entities)
            where C1 : Component
        {
            var types = new[] { typeof(C1) };
            return entities.Get(types).Select(entry =>
            {
                return (entry.ID, entry.Components.ElementAt(0) as C1);
            });
        }

        public static IEnumerable<(string, C1, C2)> Get<C1, C2>(this Dictionary<string, Entity> entities)
            where C1 : Component
            where C2 : Component
        {
            return entities.Get(typeof(C1), typeof(C2)).Select(entry =>
            {
                return (entry.ID, entry.Components.ElementAt(0) as C1, entry.Components.ElementAt(1) as C2);
            });
        }

        public static IEnumerable<(string, C1, C2, C3)> Get<C1, C2, C3>(this Dictionary<string, Entity> entities)
            where C1 : Component
            where C2 : Component
            where C3 : Component
        {
            return entities.Get(typeof(C1), typeof(C2), typeof(C3)).Select(entry =>
            {
                return (entry.ID, entry.Components.ElementAt(0) as C1, entry.Components.ElementAt(1) as C2, entry.Components.ElementAt(2) as C3);
            });
        }

        public static Dictionary<string, Component> Get(this Dictionary<string, Entity> entities, Type type)
        {
            return entities.Where(entry => entry.Value.Has(type))
                .ToDictionary(entry => entry.Key, entry => entry.Value.Get(type));
        }
    }

    public record Result<C>(
        IEnumerable<(string ID, C Component)> Added,
        IEnumerable<(string ID, C Component)> Removed,
        IEnumerable<(string ID, C Component)> Changed)
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
        public static Result<Component> Compare(Type type, State before, State after)
        {
            if (before == after)
            {
                return new Result<Component>(
                    Added: new (string ID, Component Component)[] { },
                    Removed: new (string ID, Component Component)[] { },
                    Changed: new (string ID, Component Component)[] { });
            }

            var oldComponents = before.Get(type);
            var newComponents = after.Get(type);

            var oldIds = oldComponents.Keys.ToHashSet();
            var newIds = newComponents.Keys;
            var changeIds = newIds.Where(id =>
            {
                return oldIds.Contains(id) == true && oldComponents[id] != newComponents[id];
            });

            return new Result<Component>(
                Added: newIds.Except(oldIds).Select(id => (id, newComponents[id])),
                Removed: oldIds.Except(newIds).Select(id => (id, oldComponents[id])),
                Changed: changeIds.Select(id => (id, newComponents[id])));
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