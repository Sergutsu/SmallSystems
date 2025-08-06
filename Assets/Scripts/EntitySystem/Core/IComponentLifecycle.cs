using UnityEngine;

namespace GalacticVentures.EntitySystem.Core
{
    /// <summary>
    /// Interface for components that need custom lifecycle management
    /// </summary>
    public interface IComponentLifecycle
    {
        /// <summary>
        /// Called when the component is first added to an entity
        /// </summary>
        /// <param name="entity">The entity this component belongs to</param>
        /// <param name="component">The component instance</param>
        void OnComponentAdded(GameEntity entity, ScriptableObject component);

        /// <summary>
        /// Called when the component is about to be removed from an entity
        /// </summary>
        /// <param name="entity">The entity this component belongs to</param>
        /// <param name="component">The component instance</param>
        void OnComponentRemoved(GameEntity entity, ScriptableObject component);

        /// <summary>
        /// Called when the entity is being destroyed
        /// </summary>
        /// <param name="entity">The entity being destroyed</param>
        /// <param name="component">The component instance</param>
        void OnEntityDestroyed(GameEntity entity, ScriptableObject component);

        /// <summary>
        /// Check if this lifecycle handler can manage the given component type
        /// </summary>
        /// <param name="componentType">The component type to check</param>
        /// <returns>True if this handler can manage the component type</returns>
        bool CanHandle(System.Type componentType);
    }
}