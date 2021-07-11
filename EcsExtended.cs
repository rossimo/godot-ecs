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
        public static int MOVE = typeof(Move).Name.GetHashCode();
        public static int FLASH = typeof(Flash).Name.GetHashCode();

        public static Velocity Velocity(this State state, int entityId)
        {
            return state.Get<Velocity>(VELOCITY, entityId);
        }

        public static State WithoutVelocity(this State state, int entityId)
        {
            return state.Without(VELOCITY, entityId);
        }

        public static Dictionary<int, Component> Velocity(this State state)
        {
            return state.GetAll<Velocity>(VELOCITY);
        }

        public static Move Move(this State state, int entityId)
        {
            return state.Get<Move>(MOVE, entityId);
        }

        public static State WithoutMove(this State state, int entityId)
        {
            return state.Without(MOVE, entityId);
        }

        public static Dictionary<int, Component> Move(this State state)
        {
            return state.GetAll<Move>(MOVE);
        }

        public static Flash Flash(this State state, int entityId)
        {
            return state.Get<Flash>(FLASH, entityId);
        }

        public static State WithoutFlash(this State state, int entityId)
        {
            return state.Without(FLASH, entityId);
        }

        public static Dictionary<int, Component> Flash(this State state)
        {
            return state.GetAll<Flash>(FLASH);
        }

        public static Destination Destination(this State state, int entityId)
        {
            return state.Get<Destination>(DESTINATION, entityId);
        }

        public static State WithoutDestination(this State state, int entityId)
        {
            return state.Without(DESTINATION, entityId);
        }

        public static Dictionary<int, Component> Destination(this State state)
        {
            return state.GetAll<Destination>(DESTINATION);
        }

        public static Position Position(this State state, int entityId)
        {
            return state.Get<Position>(POSITION, entityId);
        }

        public static State WithoutPosition(this State state, int entityId)
        {
            return state.Without(POSITION, entityId);
        }

        public static Dictionary<int, Component> Position(this State state)
        {
            return state.GetAll<Position>(POSITION);
        }

        public static PhysicsNode PhysicsNode(this State state, int entityId)
        {
            return state.Get<PhysicsNode>(PHYSICS_NODE, entityId);
        }

        public static State WithoutPhysicsNode(this State state, int entityId)
        {
            return state.Without(PHYSICS_NODE, entityId);
        }

        public static Dictionary<int, Component> PhysicsNode(this State state)
        {
            return state.GetAll<PhysicsNode>(PHYSICS_NODE);
        }

        public static Ticks Ticks(this State state, int entityId)
        {
            return state.Get<Ticks>(TICKS, entityId);
        }

        public static State WithoutTicks(this State state, int entityId)
        {
            return state.Without(TICKS, entityId);
        }

        public static Dictionary<int, Component> Ticks(this State state)
        {
            return state.GetAll<Ticks>(TICKS);
        }

        public static Speed Speed(this State state, int entityId)
        {
            return state.Get<Speed>(SPEED, entityId);
        }

        public static State WithoutSpeed(this State state, int entityId)
        {
            return state.Without(SPEED, entityId);
        }

        public static Dictionary<int, Component> Speed(this State state)
        {
            return state.GetAll<Speed>(SPEED);
        }

        public static Sprite Sprite(this State state, int entityId)
        {
            return state.Get<Sprite>(SPRITE, entityId);
        }

        public static State WithoutSprite(this State state, int entityId)
        {
            return state.Without(SPRITE, entityId);
        }

        public static Dictionary<int, Component> Sprite(this State state)
        {
            return state.GetAll<Sprite>(SPRITE);
        }

        public static Scale Scale(this State state, int entityId)
        {
            return state.Get<Scale>(SCALE, entityId);
        }

        public static State WithoutScale(this State state, int entityId)
        {
            return state.Without(SCALE, entityId);
        }

        public static Dictionary<int, Component> Scale(this State state)
        {
            return state.GetAll<Scale>(SCALE);
        }

        public static CollisionEvent CollisionEvent(this State state, int entityId)
        {
            return state.Get<CollisionEvent>(COLLISION_EVENT, entityId);
        }

        public static State WithoutCollisionEvent(this State state, int entityId)
        {
            return state.Without(COLLISION_EVENT, entityId);
        }

        public static Dictionary<int, Component> CollisionEvent(this State state)
        {
            return state.GetAll<CollisionEvent>(COLLISION_EVENT);
        }

        public static EventQueue EventQueue(this State state, int entityId)
        {
            return state.Get<EventQueue>(EVENT_QUEUE, entityId);
        }

        public static State WithoutEventQueue(this State state, int entityId)
        {
            return state.Without(EVENT_QUEUE, entityId);
        }

        public static Dictionary<int, Component> EventQueue(this State state)
        {
            return state.GetAll<EventQueue>(EVENT_QUEUE);
        }

        public static ExpirationEvent ExpirationEvent(this State state, int entityId)
        {
            return state.Get<ExpirationEvent>(EXPIRATION_EVENT, entityId);
        }

        public static State WithoutExpirationEvent(this State state, int entityId)
        {
            return state.Without(EXPIRATION_EVENT, entityId);
        }

        public static Dictionary<int, Component> ExpirationEvent(this State state)
        {
            return state.GetAll<ExpirationEvent>(EXPIRATION_EVENT);
        }

        public static MouseLeft MouseLeft(this State state, int entityId)
        {
            return state.Get<MouseLeft>(MOUSE_LEFT, entityId);
        }

        public static State WithoutMouseLeft(this State state, int entityId)
        {
            return state.Without(MOUSE_LEFT, entityId);
        }

        public static Dictionary<int, Component> MouseLeft(this State state)
        {
            return state.GetAll<MouseLeft>(MOUSE_LEFT);
        }

        public static MouseRight MouseRight(this State state, int entityId)
        {
            return state.Get<MouseRight>(MOUSE_RIGHT, entityId);
        }

        public static State WithoutMouseRight(this State state, int entityId)
        {
            return state.Without(MOUSE_RIGHT, entityId);
        }

        public static Dictionary<int, Component> MouseRight(this State state)
        {
            return state.GetAll<MouseRight>(MOUSE_RIGHT);
        }

        public static Player Player(this State state, int entityId)
        {
            return state.Get<Player>(PLAYER, entityId);
        }

        public static State WithoutPlayer(this State state, int entityId)
        {
            return state.Without(PLAYER, entityId);
        }

        public static Dictionary<int, Component> Player(this State state)
        {
            return state.GetAll<Player>(PLAYER);
        }

        public static Inventory Inventory(this State state, int entityId)
        {
            return state.Get<Inventory>(INVENTORY, entityId);
        }

        public static State WithoutInventory(this State state, int entityId)
        {
            return state.Without(INVENTORY, entityId);
        }

        public static Dictionary<int, Component> Inventory(this State state)
        {
            return state.GetAll<Inventory>(INVENTORY);
        }

        public static LowRenderPriority LowRenderPriority(this State state, int entityId)
        {
            return state.Get<LowRenderPriority>(LOW_RENDER_PRIORITY, entityId);
        }

        public static State WithoutLowRenderPriority(this State state, int entityId)
        {
            return state.Without(LOW_RENDER_PRIORITY, entityId);
        }

        public static Dictionary<int, Component> LowRenderPriority(this State state)
        {
            return state.GetAll<LowRenderPriority>(LOW_RENDER_PRIORITY);
        }
    }
}