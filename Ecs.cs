using System;
using System.Linq;
using Newtonsoft.Json;
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

        public Type[] Types()
        {
            return Components.Values.Select(component => component.GetType()).ToArray();
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
            Log(prev, next);
            return next;
        }

        public State With(string id, params Component[] components)
        {
            var state = this;
            foreach (var component in components)
            {
                state = state.With(id, state[id].With(component));
            }
            return state;
        }

        public Type[] Types()
        {
            var set = new HashSet<Type>();
            foreach (var entity in Values)
            {
                foreach (var type in entity.Types())
                {
                    set.Add(type);
                }
            }
            return set.ToArray();
        }

        public State Without(string id)
        {
            var prev = this;
            var next = new State(prev);
            if (next.ContainsKey(id))
            {
                next.Remove(id);
            }
            Log(prev, next);
            return next;
        }

        public State Without<C>(string id) where C : Component
        {
            return With(id, this[id].Without<C>());
        }

        public static void Log(State Previous, State State)
        {
            if (Previous == State) return;
            
            var types = new List<Type>()
                .Concat(Previous?.Types() ?? new Type[] { })
                .Concat(State?.Types() ?? new Type[] { })
                .Distinct();

            var diffs = new List<Result<Component>>();
            foreach (var type in types)
            {
                diffs.Add(Diff.Compare(type, Previous, State));
            }


            IEnumerable<(string ID, string Message)> all = new List<(string, string)>();
            foreach (var (Added, Removed, Changed) in diffs)
            {
                all = all
                    .Concat(Removed.Select(entry => (entry.ID, $"- {entry}")))
                    .Concat(Added.Select(entry => (entry.ID, $"+ {entry}")))
                    .Concat(Changed.Select(entry => (entry.ID, $"~ {entry}")));
            }

            foreach (var entry in all.OrderBy(entry => entry.ID))
            {
               Logger.Info(entry.Message);
            }
        }
    }

    public static class Utils
    {
        public static V Get<K, V>(this Dictionary<K, V> dict, K key)
        {
            V val;
            dict.TryGetValue(key, out val);
            return val;
        }

        public static Dictionary<K, V> With<K, V>(this Dictionary<K, V> dict, K key, V value)
        {
            var clone = new Dictionary<K, V>(dict);
            clone[key] = value;
            return clone;
        }

        public static IEnumerable<(string, IEnumerable<Component>)> Get(this Dictionary<string, Entity> entities, params Type[] types)
        {
            return entities.Where(entry =>
            {
                var distinct = types.Select(type => type.Name).Distinct();
                var existing = entry.Value.Types().Select(type => type.Name);
                return existing.Intersect(distinct).Count() == types.Count();
            }).Select(entry =>
            {
                var (id, entity) = entry;
                var components = types.Select(type => entity.Get(type.Name));
                return (id, components);
            });
        }

        public static IEnumerable<(string ID, Component Component)> Get(this Dictionary<string, Entity> entities, Type type)
        {
            var types = new[] { type };
            return entities.Get(types).Select(entry =>
            {
                return (entry.Item1, entry.Item2.FirstOrDefault());
            });
        }

        public static IEnumerable<(string, C1)> Get<C1>(this Dictionary<string, Entity> entities)
            where C1 : Component
        {
            var types = new[] { typeof(C1) };
            return entities.Get(types).Select(entry =>
            {
                return (entry.Item1, entry.Item2.FirstOrDefault() as C1);
            });
        }

        public static IEnumerable<(string, C1, C2)> Get<C1, C2>(this Dictionary<string, Entity> entities)
            where C1 : Component
            where C2 : Component
        {
            var types = new[] { typeof(C1), typeof(C2) };
            return entities.Get(types).Select(entry =>
            {
                var sorted = entry.Item2.ToList();
                return (entry.Item1, sorted[0] as C1, sorted[1] as C2);
            });
        }

        public static IEnumerable<(string, C1, C2, C3)> Get<C1, C2, C3>(this Dictionary<string, Entity> entities)
            where C1 : Component
            where C2 : Component
            where C3 : Component
        {
            var types = new[] { typeof(C1), typeof(C2), typeof(C3) };
            return entities.Get(types).Select(entry =>
            {
                var sorted = entry.Item2.ToList();
                return (entry.Item1, sorted[0] as C1, sorted[1] as C2, sorted[2] as C3);
            });
        }

        public static IEnumerable<C> SortByType<C>(this IEnumerable<C> values, IEnumerable<Type> types)
        {
            return types.Select(type => values.SingleOrDefault(value => value.GetType().Equals(type)));
        }

        public static void Dump(object obj)
        {
            Logger.Info(JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize
            }));
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
                    Added: new List<(string ID, Component Component)>(),
                    Removed: new List<(string ID, Component Component)>(),
                    Changed: new List<(string ID, Component Component)>());
            }

            var oldComponents = before?.Get(type) ?? new List<(string ID, Component Component)>();
            var newComponents = after?.Get(type) ?? new List<(string ID, Component Component)>();

            var oldIds = oldComponents.Select(entry => entry.ID);
            var newIds = newComponents.Select(entry => entry.ID);

            var removedIds = oldIds.Except(newIds);
            var addedIds = newIds.Except(oldIds);
            var commonIds = newIds.Union(oldIds);

            var removed = oldComponents.Where(entry => removedIds.Contains(entry.ID));
            var added = newComponents.Where(entry => addedIds.Contains(entry.ID));
            var changed = newComponents.Where(newComponent =>
            {
                var id = newComponent.ID;
                var changes = oldComponents.Where(entry =>
                {
                    return entry.ID == id && newComponent.Component != entry.Component;
                });

                return changes.Count() > 0;
            });

            return new Result<Component>(Added: added, Removed: removed, Changed: changed);
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