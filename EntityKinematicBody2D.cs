using Godot;
using System;
using Leopotam.EcsLite;

public interface EntityNode
{
    public EcsPackedEntity Entity { get; }
}

public partial class EntityKinematicBody2D : CharacterBody2D, EntityNode
{
    private EcsPackedEntity _entity;

    public EcsPackedEntity Entity
    {
        get { return _entity; }
        set { _entity = value; }
    }
}