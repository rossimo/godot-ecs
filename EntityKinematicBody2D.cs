using Godot;
using System;
using Leopotam.EcsLite;

public interface EntityNode
{
    public EcsPackedEntity Entity { get; }
}

public class EntityKinematicBody2D : KinematicBody2D, EntityNode
{
    private EcsPackedEntity _entity;

    public EcsPackedEntity Entity
    {
        get { return _entity; }
        set { _entity = value; }
    }
}