using UnityEngine;

namespace GalacticVentures.EntitySystem.Core
{
    /// <summary>
    /// Interface for all entity components that can be attached to GameEntity objects
    /// </summary>
    public interface IEntityComponent
    {
        /// <summary>
        /// Unique identifier for this component instance
        /// </summary>
        string ComponentId { get; }
        
        /// <summary>
        /// Called when the component is added to an entity
        /// </summary>
        /// <param name="owner">The entity this component is attached to</param>
        void Initialize(GameEntity owner);
        
        /// <summary>
        /// Called when the component is removed from an entity or the entity is destroyed
        /// </summary>
        void Cleanup();
        
        /// <summary>
        /// Validates the current state of the component
        /// </summary>
        /// <returns>True if the component is in a valid state</returns>
        bool IsValid();
    }
}