using System;

namespace GalacticVentures.EntitySystem.Core
{
    /// <summary>
    /// Base interface for all game events
    /// </summary>
    public interface IGameEvent
    {
        /// <summary>
        /// Unique identifier for this event instance
        /// </summary>
        string EventId { get; }
        
        /// <summary>
        /// When this event was created
        /// </summary>
        DateTime Timestamp { get; }
        
        /// <summary>
        /// The entity that triggered this event (can be null for system events)
        /// </summary>
        GameEntity Source { get; }
        
        /// <summary>
        /// Priority level for event processing
        /// </summary>
        EventPriority Priority { get; }
    }
}