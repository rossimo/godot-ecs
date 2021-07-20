using Godot;
using DefaultEcs;

public class EntityKinematicBody2D : KinematicBody2D, EntityNode
{
    private Entity _entity;

    public Entity Entity
    {
        get { return _entity; }
        set { _entity = value; }
    }
}

public interface EntityNode
{
    public Entity Entity { get; set; }
}