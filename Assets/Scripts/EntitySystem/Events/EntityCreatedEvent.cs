using GalacticVentures.EntitySystem.Core;

namespace GalacticVentures.EntitySystem.Events
{
    /// <summary>
    /// Event triggered when an entity is created and registered
    /// </summary>
    public class EntityCreatedEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public EntityFaction Faction { get; private set; }

        public EntityCreatedEvent(GameEntity source, string entityId, EntityFaction faction) 
            : base(source, EventPriority.High)
        {
            EntityId = entityId;
            Faction = faction;
        }
    }
}