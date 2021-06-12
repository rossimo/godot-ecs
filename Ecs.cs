using System;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Ecs
{
    public record Component();

    public record Entity
    {
        public IEnumerable<Component> Components = new List<Component>();

        public Entity() { }

        public Entity(IEnumerable<Component> components)
            => (Components) = (components);

        public Entity(params Component[] components)
            => (Components) = (components);

        public C Get<C>() where C : Component
        {
            return Components.OfType<C>().FirstOrDefault();
        }

        public Entity With<C>(C component) where C : Component
        {
            var existing = Get<C>();
            var components = existing == null
                ? new List<Component>(Components)
                : Components.Where(other => !component.GetType().Equals(other.GetType())).ToList();

            components.Add(component);

            return this with { Components = components };
        }

        public Entity Without<C>() where C : Component
        {
            return this with { Components = Components.Where(other => !typeof(C).Equals(other.GetType())).ToList() };
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
            return new State(Utils.With(this, id, entity));
        }

        public State With(string id, Component component)
        {
            return With(id, this[id].With(component));
        }

        public State Without(string id)
        {
            var state = new State(this);
            if (state.ContainsKey(id))
            {
                state.Remove(id);
            }
            return state;
        }
    }

    public static class Utils
    {
        public static Dictionary<K, V> With<K, V>(this Dictionary<K, V> dict, K key, V value)
        {
            if (dict.ContainsKey(key))
            {
                var clone = new Dictionary<K, V>(dict);
                clone[key] = value;
                return clone;
            }
            else
            {
                return new Dictionary<K, V>(dict) { { key, value } };
            }
        }

        public static IEnumerable<(string, IEnumerable<Component>)> Get(this Dictionary<string, Entity> entities, params Type[] types)
        {
            return entities.Where(entry =>
            {
                var distinct = types.Distinct();
                var existing = entry.Value.Components.Select(component => component.GetType()).Distinct();
                return existing.Intersect(distinct).Count() == types.Count();
            }).Select(entry =>
            {
                var (id, entity) = entry;
                return (id, entity.Components.SortByType(types));
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
            Console.WriteLine(JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize
            }));
        }
    }

    public record Result<C>(
        IEnumerable<(string, C)> Added,
        IEnumerable<(string, C)> Removed,
        IEnumerable<(string, C)> Changed);

    public class Diff
    {
        public static Result<C> Compare<C>(State before, State after) where C : Component
        {
            if (before == after) return new Result<C>(
                Added: new List<(string, C)>(),
                Removed: new List<(string, C)>(),
                Changed: new List<(string, C)>());

            var oldComponents = before?.Get<C>() ?? new List<(string, C)>();
            var newComponents = after?.Get<C>() ?? new List<(string, C)>();

            var oldIds = oldComponents.Select(entry => entry.Item1);
            var newIds = newComponents.Select(entry => entry.Item1);

            var removedIds = oldIds.Except(newIds);
            var addedIds = newIds.Except(oldIds);
            var commonIds = newIds.Union(oldIds);

            var removed = oldComponents.Where(entry => removedIds.Contains(entry.Item1));
            var added = newComponents.Where(entry => addedIds.Contains(entry.Item1));
            var changed = newComponents.Where(newComponent =>
            {
                var id = newComponent.Item1;
                var changes = oldComponents.Where(entry =>
                {
                    return entry.Item1 == id && newComponent.Item2 != entry.Item2;
                });

                return changes.Count() > 0;
            });

            foreach (var remove in removed)
            {
                Console.WriteLine($"- {remove}");
            }

            foreach (var add in added)
            {
                Console.WriteLine($"+ {add}");
            }

            foreach (var change in changed)
            {
                Console.WriteLine($"~ {change}");
            }

            return new Result<C>(Added: added, Removed: removed, Changed: changed);
        }

        public static (Result<C1>, Result<C2>) Compare<C1, C2>(State Current, State next)
            where C1 : Component
            where C2 : Component
        {
            var changes1 = Compare<C1>(Current, next);
            var changes2 = Compare<C2>(Current, next);
            return (changes1, changes2);
        }

        public static (Result<C1>, Result<C2>, Result<C3>) Compare<C1, C2, C3>(State Current, State next)
            where C1 : Component
            where C2 : Component
            where C3 : Component
        {
            var changes1 = Compare<C1>(Current, next);
            var changes2 = Compare<C2>(Current, next);
            var changes3 = Compare<C3>(Current, next);
            return (changes1, changes2, changes3);
        }

        public static (Result<C1>, Result<C2>, Result<C3>, Result<C4>) Compare<C1, C2, C3, C4>(State Current, State next)
            where C1 : Component
            where C2 : Component
            where C3 : Component
            where C4 : Component
        {
            var changes1 = Compare<C1>(Current, next);
            var changes2 = Compare<C2>(Current, next);
            var changes3 = Compare<C3>(Current, next);
            var changes4 = Compare<C4>(Current, next);
            return (changes1, changes2, changes3, changes4);
        }

        public static (Result<C1>, Result<C2>, Result<C3>, Result<C4>, Result<C5>) Compare<C1, C2, C3, C4, C5>(State Current, State next)
            where C1 : Component
            where C2 : Component
            where C3 : Component
            where C4 : Component
            where C5 : Component
        {
            var changes1 = Compare<C1>(Current, next);
            var changes2 = Compare<C2>(Current, next);
            var changes3 = Compare<C3>(Current, next);
            var changes4 = Compare<C4>(Current, next);
            var changes5 = Compare<C5>(Current, next);
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
            var changes1 = Compare<C1>(Current, next);
            var changes2 = Compare<C2>(Current, next);
            var changes3 = Compare<C3>(Current, next);
            var changes4 = Compare<C4>(Current, next);
            var changes5 = Compare<C5>(Current, next);
            var changes6 = Compare<C6>(Current, next);
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
            var changes1 = Compare<C1>(Current, next);
            var changes2 = Compare<C2>(Current, next);
            var changes3 = Compare<C3>(Current, next);
            var changes4 = Compare<C4>(Current, next);
            var changes5 = Compare<C5>(Current, next);
            var changes6 = Compare<C6>(Current, next);
            var changes7 = Compare<C7>(Current, next);
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
            var changes1 = Compare<C1>(Current, next);
            var changes2 = Compare<C2>(Current, next);
            var changes3 = Compare<C3>(Current, next);
            var changes4 = Compare<C4>(Current, next);
            var changes5 = Compare<C5>(Current, next);
            var changes6 = Compare<C6>(Current, next);
            var changes7 = Compare<C7>(Current, next);
            var changes8 = Compare<C8>(Current, next);
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
            var changes1 = Compare<C1>(Current, next);
            var changes2 = Compare<C2>(Current, next);
            var changes3 = Compare<C3>(Current, next);
            var changes4 = Compare<C4>(Current, next);
            var changes5 = Compare<C5>(Current, next);
            var changes6 = Compare<C6>(Current, next);
            var changes7 = Compare<C7>(Current, next);
            var changes8 = Compare<C8>(Current, next);
            var changes9 = Compare<C9>(Current, next);
            return (changes1, changes2, changes3, changes4, changes5, changes6, changes7, changes8, changes9);
        }
    }
}
