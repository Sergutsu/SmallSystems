using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GalacticVentures.EntitySystem.Events;

namespace GalacticVentures.EntitySystem.Core
{
    /// <summary>
    /// Base class for all game entities in the system
    /// </summary>
    public abstract class GameEntity : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField] protected string _entityId;
        [SerializeField] protected EntityFaction _faction = EntityFaction.None;
        [SerializeField] private List<ScriptableObject> _serializedComponents = new();
        
        private Dictionary<string, ScriptableObject> _components = new();
        private bool _isRegistered = false;

        public string EntityId => _entityId;
        public EntityFaction Faction => _faction;
        public bool IsRegistered => _isRegistered;

        /// <summary>
        /// Get a component of the specified type
        /// </summary>
        public T GetComponent<T>() where T : ScriptableObject
        {
            return GetComponent(typeof(T)) as T;
        }

        /// <summary>
        /// Get a component of the specified type
        /// </summary>
        public ScriptableObject GetComponent(Type componentType)
        {
            if (componentType == null) return null;
            
            _components.TryGetValue(componentType.Name, out var component);
            return component;
        }

        /// <summary>
        /// Check if the entity has a component of the specified type
        /// </summary>
        public bool HasComponent<T>() where T : ScriptableObject
        {
            return HasComponent(typeof(T));
        }

        /// <summary>
        /// Check if the entity has a component of the specified type
        /// </summary>
        public bool HasComponent(Type componentType)
        {
            if (componentType == null) return false;
            return _components.ContainsKey(componentType.Name);
        }

        /// <summary>
        /// Add a component to this entity
        /// </summary>
        public void AddComponent<T>(T component) where T : ScriptableObject
        {
            if (component == null) return;

            var componentType = typeof(T);
            var componentName = componentType.Name;

            // Replace existing component if present
            if (_components.ContainsKey(componentName))
            {
                var oldComponent = _components[componentName];
                RemoveComponent(componentType);
            }

            // Add new component
            _components[componentName] = component;

            // Initialize component if it implements IEntityComponent
            if (component is IEntityComponent entityComponent)
            {
                try
                {
                    entityComponent.Initialize(this);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"GameEntity: Error initializing component {componentName} on entity {_entityId}: {ex}");
                }
            }

            // Update registry index
            EntityRegistry.Instance.OnComponentAdded(_entityId, componentType);

            // Trigger event
            EventBus.Instance.TriggerEvent(new ComponentAddedEvent(this, component));

            Debug.Log($"GameEntity: Added component {componentName} to entity {_entityId}");
        }

        /// <summary>
        /// Remove a component from this entity
        /// </summary>
        public bool RemoveComponent<T>() where T : ScriptableObject
        {
            return RemoveComponent(typeof(T));
        }

        /// <summary>
        /// Remove a component from this entity
        /// </summary>
        public bool RemoveComponent(Type componentType)
        {
            if (componentType == null) return false;

            var componentName = componentType.Name;
            if (!_components.ContainsKey(componentName)) return false;

            var component = _components[componentName];

            // Cleanup component if it implements IEntityComponent
            if (component is IEntityComponent entityComponent)
            {
                try
                {
                    entityComponent.Cleanup();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"GameEntity: Error cleaning up component {componentName} on entity {_entityId}: {ex}");
                }
            }

            // Remove from dictionary
            _components.Remove(componentName);

            // Update registry index
            EntityRegistry.Instance.OnComponentRemoved(_entityId, componentType);

            // Trigger event
            EventBus.Instance.TriggerEvent(new ComponentRemovedEvent(this, component));

            Debug.Log($"GameEntity: Removed component {componentName} from entity {_entityId}");
            return true;
        }

        /// <summary>
        /// Get all components attached to this entity
        /// </summary>
        public IEnumerable<ScriptableObject> GetAllComponents()
        {
            return _components.Values;
        }

        /// <summary>
        /// Get all component types attached to this entity
        /// </summary>
        public IEnumerable<Type> GetComponentTypes()
        {
            return _components.Values.Select(c => c.GetType());
        }

        /// <summary>
        /// Change the faction of this entity
        /// </summary>
        public void SetFaction(EntityFaction newFaction)
        {
            if (_faction == newFaction) return;

            var oldFaction = _faction;
            _faction = newFaction;

            // Update registry
            if (_isRegistered)
            {
                EntityRegistry.Instance.OnFactionChanged(_entityId, oldFaction, newFaction);
            }

            // Trigger event
            EventBus.Instance.TriggerEvent(new FactionChangedEvent(this, _entityId, oldFaction, newFaction));

            Debug.Log($"GameEntity: Changed faction of entity {_entityId} from {oldFaction} to {newFaction}");
        }

        /// <summary>
        /// Generate a unique entity ID
        /// </summary>
        protected virtual string GenerateEntityId()
        {
            return $"{GetType().Name}_{Guid.NewGuid().ToString("N")[..8]}_{DateTimeOffset.UtcNow.Ticks}";
        }

        /// <summary>
        /// Initialize the entity (called automatically)
        /// </summary>
        protected virtual void InitializeEntity()
        {
            if (string.IsNullOrEmpty(_entityId))
            {
                _entityId = GenerateEntityId();
            }

            // Register with EntityRegistry
            if (!_isRegistered)
            {
                _isRegistered = EntityRegistry.Instance.RegisterEntity(this);
            }
        }

        /// <summary>
        /// Cleanup the entity (called automatically)
        /// </summary>
        protected virtual void CleanupEntity()
        {
            // Cleanup all components
            var componentsToCleanup = _components.Values.ToList();
            foreach (var component in componentsToCleanup)
            {
                if (component is IEntityComponent entityComponent)
                {
                    try
                    {
                        entityComponent.Cleanup();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"GameEntity: Error cleaning up component on entity {_entityId}: {ex}");
                    }
                }
            }

            // Unregister from EntityRegistry
            if (_isRegistered)
            {
                EntityRegistry.Instance.UnregisterEntity(_entityId);
                _isRegistered = false;
            }
        }

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            InitializeEntity();
        }

        protected virtual void OnDestroy()
        {
            CleanupEntity();
        }

        protected virtual void Update()
        {
            // Update spatial index if position changed
            if (_isRegistered)
            {
                EntityRegistry.Instance.UpdateSpatialIndex(_entityId, transform.position);
            }
        }

        #endregion

        #region Serialization

        public void OnBeforeSerialize()
        {
            // Convert dictionary to list for serialization
            _serializedComponents.Clear();
            _serializedComponents.AddRange(_components.Values);
        }

        public void OnAfterDeserialize()
        {
            // Convert list back to dictionary after deserialization
            _components.Clear();
            foreach (var component in _serializedComponents)
            {
                if (component != null)
                {
                    _components[component.GetType().Name] = component;
                }
            }
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure entity ID is set in editor
            if (string.IsNullOrEmpty(_entityId))
            {
                _entityId = GenerateEntityId();
            }

            // Validate components
            foreach (var component in _components.Values)
            {
                if (component is IEntityComponent entityComponent)
                {
                    if (!entityComponent.IsValid())
                    {
                        Debug.LogWarning($"GameEntity: Component {component.GetType().Name} on entity {_entityId} is in invalid state");
                    }
                }
            }
        }

        /// <summary>
        /// Get debug information for the inspector
        /// </summary>
        public string GetDebugInfo()
        {
            var info = $"Entity ID: {_entityId}\n";
            info += $"Faction: {_faction}\n";
            info += $"Registered: {_isRegistered}\n";
            info += $"Components ({_components.Count}):\n";
            
            foreach (var kvp in _components)
            {
                var component = kvp.Value;
                var isValid = component is IEntityComponent entityComponent ? entityComponent.IsValid() : true;
                info += $"  - {kvp.Key} {(isValid ? "✓" : "✗")}\n";
            }
            
            return info;
        }
#endif

        #endregion
    }
}