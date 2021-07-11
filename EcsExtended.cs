using System.Collections.Generic;

namespace Ecs
{
    public static class Extended
    {
        public static int VELOCITY = typeof(Velocity).Name.GetHashCode();
        public static int DESTINATION = typeof(Destination).Name.GetHashCode();
        public static int POSITION = typeof(Position).Name.GetHashCode();
        public static int PHYSICS_NODE = typeof(PhysicsNode).Name.GetHashCode();
        public static int TICKS = typeof(Ticks).Name.GetHashCode();
        public static int SPEED = typeof(Speed).Name.GetHashCode();
        public static int SPRITE = typeof(Sprite).Name.GetHashCode();
        public static int SCALE = typeof(Scale).Name.GetHashCode();
        public static int COLLISION_EVENT = typeof(CollisionEvent).Name.GetHashCode();
        public static int EVENT_QUEUE = typeof(EventQueue).Name.GetHashCode();
        public static int EXPIRATION_EVENT = typeof(ExpirationEvent).Name.GetHashCode();
        public static int MOUSE_LEFT = typeof(MouseLeft).Name.GetHashCode();
        public static int MOUSE_RIGHT = typeof(MouseRight).Name.GetHashCode();
        public static int PLAYER = typeof(Player).Name.GetHashCode();
        public static int INVENTORY = typeof(Inventory).Name.GetHashCode();
        public static int LOW_RENDER_PRIORITY = typeof(LowRenderPriority).Name.GetHashCode();

        public static Velocity Velocity(this State state, int entityId)
        {
            return state.Get<Velocity>(VELOCITY, entityId);
        }

        public static IEnumerable<(int, Velocity)> Velocity(this State state)
        {
            return state.GetAll<Velocity>(VELOCITY);
        }

        public static Destination Destination(this State state, int entityId)
        {
            return state.Get<Destination>(DESTINATION, entityId);
        }

        public static IEnumerable<(int, Destination)> Destination(this State state)
        {
            return state.GetAll<Destination>(DESTINATION);
        }

        public static Position Position(this State state, int entityId)
        {
            return state.Get<Position>(POSITION, entityId);
        }

        public static IEnumerable<(int, Position)> Position(this State state)
        {
            return state.GetAll<Position>(POSITION);
        }

        public static PhysicsNode PhysicsNode(this State state, int entityId)
        {
            return state.Get<PhysicsNode>(PHYSICS_NODE, entityId);
        }

        public static IEnumerable<(int, PhysicsNode)> PhysicsNode(this State state)
        {
            return state.GetAll<PhysicsNode>(PHYSICS_NODE);
        }

        public static Ticks Ticks(this State state, int entityId)
        {
            return state.Get<Ticks>(TICKS, entityId);
        }

        public static IEnumerable<(int, Ticks)> Ticks(this State state)
        {
            return state.GetAll<Ticks>(TICKS);
        }

        public static Speed Speed(this State state, int entityId)
        {
            return state.Get<Speed>(SPEED, entityId);
        }

        public static IEnumerable<(int, Speed)> Speed(this State state)
        {
            return state.GetAll<Speed>(SPEED);
        }

        public static Sprite Sprite(this State state, int entityId)
        {
            return state.Get<Sprite>(SPRITE, entityId);
        }

        public static IEnumerable<(int, Sprite)> Sprite(this State state)
        {
            return state.GetAll<Sprite>(SPRITE);
        }

        public static Scale Scale(this State state, int entityId)
        {
            return state.Get<Scale>(SCALE, entityId);
        }

        public static IEnumerable<(int, Scale)> Scale(this State state)
        {
            return state.GetAll<Scale>(SCALE);
        }

        public static CollisionEvent CollisionEvent(this State state, int entityId)
        {
            return state.Get<CollisionEvent>(COLLISION_EVENT, entityId);
        }

        public static IEnumerable<(int, CollisionEvent)> CollisionEvent(this State state)
        {
            return state.GetAll<CollisionEvent>(COLLISION_EVENT);
        }

        public static EventQueue EventQueue(this State state, int entityId)
        {
            return state.Get<EventQueue>(EVENT_QUEUE, entityId);
        }

        public static IEnumerable<(int, EventQueue)> EventQueue(this State state)
        {
            return state.GetAll<EventQueue>(EVENT_QUEUE);
        }

        public static ExpirationEvent ExpirationEvent(this State state, int entityId)
        {
            return state.Get<ExpirationEvent>(EXPIRATION_EVENT, entityId);
        }

        public static IEnumerable<(int, ExpirationEvent)> ExpirationEvent(this State state)
        {
            return state.GetAll<ExpirationEvent>(EXPIRATION_EVENT);
        }

        public static MouseLeft MouseLeft(this State state, int entityId)
        {
            return state.Get<MouseLeft>(MOUSE_LEFT, entityId);
        }

        public static IEnumerable<(int, MouseLeft)> MouseLeft(this State state)
        {
            return state.GetAll<MouseLeft>(MOUSE_LEFT);
        }

        public static MouseRight MouseRight(this State state, int entityId)
        {
            return state.Get<MouseRight>(MOUSE_RIGHT, entityId);
        }

        public static IEnumerable<(int, MouseRight)> MouseRight(this State state)
        {
            return state.GetAll<MouseRight>(MOUSE_RIGHT);
        }

        public static Player Player(this State state, int entityId)
        {
            return state.Get<Player>(PLAYER, entityId);
        }

        public static IEnumerable<(int, Player)> Player(this State state)
        {
            return state.GetAll<Player>(PLAYER);
        }

        public static Inventory Inventory(this State state, int entityId)
        {
            return state.Get<Inventory>(INVENTORY, entityId);
        }

        public static IEnumerable<(int, Inventory)> Inventory(this State state)
        {
            return state.GetAll<Inventory>(INVENTORY);
        }

        public static LowRenderPriority LowRenderPriority(this State state, int entityId)
        {
            return state.Get<LowRenderPriority>(LOW_RENDER_PRIORITY, entityId);
        }

        public static IEnumerable<(int, LowRenderPriority)> LowRenderPriority(this State state)
        {
            return state.GetAll<LowRenderPriority>(LOW_RENDER_PRIORITY);
        }
    }
}