using GalacticVentures.EntitySystem.Core;
using UnityEngine;

namespace GalacticVentures.EntitySystem.Events
{
    /// <summary>
    /// Event triggered when a component is added to an entity
    /// </summary>
    public class ComponentAddedEvent : GameEvent
    {
        public ScriptableObject Component { get; private set; }
        public System.Type ComponentType { get; private set; }

        public ComponentAddedEvent(GameEntity source, ScriptableObject component) 
            : base(source, EventPriority.Normal)
        {
            Component = component;
            ComponentType = component?.GetType();
        }
    }
}