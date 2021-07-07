using System;
using System.Linq;
using System.Collections.Generic;

namespace Ecs
{
    public record Component();

    public record Entity
    {
        public Dictionary<string, Component> Components = new Dictionary<string, Component>();

        public Entity() { }

        public Entity(IEnumerable<Component> components)
            => (Components) = (components.ToDictionary(component => component.GetType().Name));

        public Entity(params Component[] components)
            => (Components) = (components.ToDictionary(component => component.GetType().Name));

        public Component Get(string componentId)
        {
            return Components.Get(componentId);
        }

        public Component Get(Type type)
        {
            return Get(type.Name);
        }

        public bool Has(string type)
        {
            return Components.ContainsKey(type);
        }

        public bool Has(Type type)
        {
            return Has(type.Name);
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

        public Entity With<C>(C component) where C : Component
        {
            var existing = Get<C>();

            if (existing != component)
            {
                var componentId = component.GetType().Name;
                var components = new Dictionary<string, Component>(Components);

                components[componentId] = component;

                return this with { Components = components };
            }
            else
            {
                return this;
            }
        }

        public Entity Without(string componentId)
        {
            if (Components.ContainsKey(componentId))
            {
                var components = new Dictionary<string, Component>(Components);
                components.Remove(componentId);
                return this with { Components = components };
            }

            return this;
        }
    }

    public class State
    {
        public Dictionary<string, Dictionary<string, Component>> Components = new Dictionary<string, Dictionary<string, Component>>();

        public static IEnumerable<string> LOGGING_IGNORE = new[] { typeof(Ticks).Name };

        public State()
        {
            Components.Add(typeof(Position).Name, new Dictionary<string, Component>());
            Components.Add(typeof(Player).Name, new Dictionary<string, Component>());
            Components.Add(typeof(Speed).Name, new Dictionary<string, Component>());
            Components.Add(typeof(MouseLeft).Name, new Dictionary<string, Component>());
            Components.Add(typeof(MouseRight).Name, new Dictionary<string, Component>());
            Components.Add(typeof(Move).Name, new Dictionary<string, Component>());
            Components.Add(typeof(ExpirationEvent).Name, new Dictionary<string, Component>());
            Components.Add(typeof(Event).Name, new Dictionary<string, Component>());
            Components.Add(typeof(Add).Name, new Dictionary<string, Component>());
            Components.Add(typeof(Remove).Name, new Dictionary<string, Component>());
            Components.Add(typeof(AddEntity).Name, new Dictionary<string, Component>());
            Components.Add(typeof(Ticks).Name, new Dictionary<string, Component>());
            Components.Add(typeof(Destination).Name, new Dictionary<string, Component>());
            Components.Add(typeof(Velocity).Name, new Dictionary<string, Component>());
            Components.Add(typeof(Collision).Name, new Dictionary<string, Component>());
            Components.Add(typeof(CollisionEvent).Name, new Dictionary<string, Component>());
            Components.Add(typeof(Area).Name, new Dictionary<string, Component>());
            Components.Add(typeof(AreaEnterEvent).Name, new Dictionary<string, Component>());
            Components.Add(typeof(Sprite).Name, new Dictionary<string, Component>());
            Components.Add(typeof(Rotation).Name, new Dictionary<string, Component>());
            Components.Add(typeof(Scale).Name, new Dictionary<string, Component>());
            Components.Add(typeof(Color).Name, new Dictionary<string, Component>());
            Components.Add(typeof(Flash).Name, new Dictionary<string, Component>());
            Components.Add(typeof(ClickEvent).Name, new Dictionary<string, Component>());
            Components.Add(typeof(Inventory).Name, new Dictionary<string, Component>());
            Components.Add(typeof(EventQueue).Name, new Dictionary<string, Component>());
        }

        public State(State state)
        {
            Components = new Dictionary<string, Dictionary<string, Component>>(state.Components);
        }

        public State(Dictionary<string, Dictionary<string, Component>> components)
        {
            Components = new Dictionary<string, Dictionary<string, Component>>(components);
        }

        public Entity this[string entityId]
        {
            get => Get(entityId);
            set => Set(entityId, value);
        }

        public Entity Get(string entityId)
        {
            var entity = new Entity();
            foreach (var component in Components.Values)
            {
                if (component.ContainsKey(entityId))
                {
                    entity = entity.With<Component>(component.Get(entityId));
                }
            }
            return entity;
        }

        public Entity Set(string entityId, Entity entity)
        {
            foreach (var componentId in Components.Keys)
            {
                if (entity.Has(componentId))
                {
                    Components[componentId][entityId] = entity.Get(componentId);
                }
                else
                {
                    Components[componentId].Remove(entityId);
                }
            }
            return entity;
        }

        public IEnumerable<(string, C1)> Get<C1>()
            where C1 : Component
        {
            return GetComponent(typeof(C1).Name).Select(entry => (entry.Key, entry.Value as C1));
        }

        public Dictionary<string, Component> GetComponent(string componentId)
        {
            return Components[componentId];
        }

        public Boolean ContainsKey(string entityId)
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

        public State With(string entityId, Entity entity)
        {
            var prev = this;
            var state = this;
            foreach (var component in entity.Components.Values)
            {
                state = state.With(entityId, component);
            }
            return state;
        }

        public State With(string entityId, Component component)
        {
            var componentId = component.GetType().Name;

            if (this.Components.ContainsKey(componentId) &&
                this.Components[componentId].ContainsKey(entityId) &&
                this.Components[componentId][entityId] == component)
            {
                return this;
            }

            var prev = this;
            var state = new State(this);

            state.Components[componentId] = new Dictionary<string, Component>(state.Components[componentId]);
            state.Components[componentId][entityId] = component;

            Logger.Log(prev, state, State.LOGGING_IGNORE);
            return state;
        }

        public State With(string entityId, Func<Entity, Entity> transform)
        {
            return this.With(entityId, transform(this.Get(entityId)));
        }

        public State Without(string entityId)
        {
            var prev = this;
            var state = this;
            foreach (var componentId in state.Components.Keys)
            {
                state = state.Without(componentId, entityId);
            }
            return state;
        }

        public State Without<C>(string entityId) where C : Component
        {
            return this.Without(typeof(C).Name, entityId);
        }

        public State Without(string componentId, string entityId)
        {
            if (!this.Components[componentId].ContainsKey(entityId))
            {
                return this;
            }

            var prev = this;
            var state = new State(this);
            state.Components[componentId] = new Dictionary<string, Component>(state.Components[componentId]);
            state.Components[componentId].Remove(entityId);
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
        public static Result<Component> Compare(string type, State before, State after)
        {
            if (before == after || before.Components[type] == after.Components[type])
            {
                return new Result<Component>(
                    Added: new (string ID, Component Component)[] { },
                    Removed: new (string ID, Component Component)[] { },
                    Changed: new (string ID, Component Component)[] { });
            }

            var oldComponents = before.GetComponent(type);
            var newComponents = after.GetComponent(type);

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
            return Compare(type.Name, before, after);
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