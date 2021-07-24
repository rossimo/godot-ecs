using System;
using Leopotam.EcsLite;
using System.Runtime.CompilerServices;

public struct Update<T>
{
    public EcsPackedEntity Entity;
}

public class UpdateQueue<C>
    where C : struct
{
    private EcsWorld World;
    private EcsFilter Filter;
    private EcsPool<C> ComponentsPool;
    private EcsPool<Update<C>> UpdatePool;

    public UpdateQueue(EcsWorld world)
    {
        World = world;
        ComponentsPool = World.GetPool<C>();
        UpdatePool = World.GetPool<Update<C>>();
        Filter = World.Filter<C>().Inc<Update<C>>().End();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator<C> GetEnumerator()
    {
        return new Enumerator<C>(Filter.GetEnumerator(), World, UpdatePool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref C Get(int entity)
    {
        return ref ComponentsPool.Get(entity);
    }

    public struct Enumerator<X> : IDisposable
        where X : struct
    {
        EcsFilter.Enumerator _enumerator;
        int _current;

        readonly EcsPool<Update<X>> _updatePool;
        readonly EcsWorld _world;

        public Enumerator(EcsFilter.Enumerator enumerator, EcsWorld world, EcsPool<Update<X>> updatePool)
        {
            _enumerator = enumerator;
            _updatePool = updatePool;
            _world = world;
            _current = -1;
        }

        public int Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (_enumerator.MoveNext())
            {
                var current = _enumerator.Current;
                var update = _updatePool.Get(current);
                _updatePool.Del(current);
                if (update.Entity.Unpack(_world, out var entity))
                {
                    _current = entity;
                    return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            _enumerator.Dispose();
        }
    }
}

public static class UpdateUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref C AddEmit<C>(this EcsPool<C> pool, EcsWorld world, int entity)
        where C : struct
    {
        ref var component = ref pool.Add(entity);
        UpdateEmit(pool, world, entity);
        return ref component;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdateEmit<C>(this EcsPool<C> pool, EcsWorld world, int entity)
        where C : struct
    {
        ref var update = ref world.GetPool<Update<C>>().Add(entity);
        update.Entity = world.PackEntity(entity);
    }
}
