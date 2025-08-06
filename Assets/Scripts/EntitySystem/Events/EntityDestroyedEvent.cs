using GalacticVentures.EntitySystem.Core;

namespace GalacticVentures.EntitySystem.Events
{
    /// <summary>
    /// Event triggered when an entity is destroyed and unregistered
    /// </summary>
    public class EntityDestroyedEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public EntityFaction Faction { get; private set; }

        public EntityDestroyedEvent(GameEntity source, string entityId, EntityFaction faction) 
            : base(source, EventPriority.High)
        {
            EntityId = entityId;
            Faction = faction;
        }
    }
}