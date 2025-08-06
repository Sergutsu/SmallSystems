using System;

namespace GalacticVentures.EntitySystem.Core
{
    /// <summary>
    /// Defines the priority levels for event processing
    /// </summary>
    [Serializable]
    public enum EventPriority
    {
        /// <summary>
        /// Low priority events processed last
        /// </summary>
        Low = 0,
        
        /// <summary>
        /// Normal priority events processed in standard order
        /// </summary>
        Normal = 1,
        
        /// <summary>
        /// High priority events processed before normal events
        /// </summary>
        High = 2,
        
        /// <summary>
        /// Critical events processed immediately
        /// </summary>
        Critical = 3
    }
}