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
    }

    public delegate State System(State state);

    public record World
    {
        public State State = new State();
        public IEnumerable<Func<State, State>> Systems = new List<Func<State, State>>();

        public Entity this[string id]
        {
            get => this.State[id];
            set => this.State[id] = value;
        }

        public World With(string id, Component component)
        {
            return this with { State = State.With(id, this[id].With(component)) };
        }

        public World Run()
        {
            var state = State;

            foreach (var system in Systems)
            {
                state = system(state);
            }

            return this with { State = state };
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
}
