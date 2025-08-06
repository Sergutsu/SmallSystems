using GalacticVentures.EntitySystem.Core;

namespace GalacticVentures.EntitySystem.Events
{
    /// <summary>
    /// Event triggered when an entity's faction changes
    /// </summary>
    public class FactionChangedEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public EntityFaction OldFaction { get; private set; }
        public EntityFaction NewFaction { get; private set; }

        public FactionChangedEvent(GameEntity source, string entityId, EntityFaction oldFaction, EntityFaction newFaction) 
            : base(source, EventPriority.Normal)
        {
            EntityId = entityId;
            OldFaction = oldFaction;
            NewFaction = newFaction;
        }
    }
}