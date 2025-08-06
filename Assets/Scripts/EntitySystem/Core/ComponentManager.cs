using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GalacticVentures.EntitySystem.Events;

namespace GalacticVentures.EntitySystem.Core
{
    /// <summary>
    /// Manages component lifecycle and dependencies across the entity system
    /// </summary>
    public class ComponentManager : MonoBehaviour
    {
        private static ComponentManager _instance;
        public static ComponentManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ComponentManager");
                    _instance = go.AddComponent<ComponentManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private int _maxInitializationQueueSize = 1000;

        // Lifecycle handlers for different component types
        private Dictionary<Type, IComponentLifecycle> _lifecycleHandlers = new();
        
        // Initialization queue for components that need deferred initialization
        private Queue<ComponentInitializationRequest> _initializationQueue = new();
        
        // Cleanup queue for components that need deferred cleanup
        private Queue<ComponentCleanupRequest> _cleanupQueue = new();
        
        // Component dependencies for initialization ordering
        private Dictionary<Type, List<Type>> _componentDependencies = new();
        
        // Active components being managed
        private HashSet<ComponentInstance> _activeComponents = new();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            // Subscribe to entity system events
            EventBus.Instance.Subscribe<ComponentAddedEvent>(OnComponentAdded);
            EventBus.Instance.Subscribe<ComponentRemovedEvent>(OnComponentRemoved);
            EventBus.Instance.Subscribe<EntityDestroyedEvent>(OnEntityDestroyed);

            if (_enableLogging)
                Debug.Log("ComponentManager: Initialized");
        }

        private void Update()
        {
            ProcessInitializationQueue();
            ProcessCleanupQueue();
        }

        /// <summary>
        /// Register a lifecycle handler for a specific component type
        /// </summary>
        public void RegisterLifecycleHandler<T>(IComponentLifecycle handler) where T : ScriptableObject
        {
            RegisterLifecycleHandler(typeof(T), handler);
        }

        /// <summary>
        /// Register a lifecycle handler for a specific component type
        /// </summary>
        public void RegisterLifecycleHandler(Type componentType, IComponentLifecycle handler)
        {
            if (componentType == null || handler == null) return;

            _lifecycleHandlers[componentType] = handler;

            if (_enableLogging)
                Debug.Log($"ComponentManager: Registered lifecycle handler for {componentType.Name}");
        }

        /// <summary>
        /// Unregister a lifecycle handler for a specific component type
        /// </summary>
        public void UnregisterLifecycleHandler<T>() where T : ScriptableObject
        {
            UnregisterLifecycleHandler(typeof(T));
        }

        /// <summary>
        /// Unregister a lifecycle handler for a specific component type
        /// </summary>
        public void UnregisterLifecycleHandler(Type componentType)
        {
            if (componentType == null) return;

            _lifecycleHandlers.Remove(componentType);

            if (_enableLogging)
                Debug.Log($"ComponentManager: Unregistered lifecycle handler for {componentType.Name}");
        }

        /// <summary>
        /// Register component dependencies for initialization ordering
        /// </summary>
        public void RegisterComponentDependency<TComponent, TDependency>() 
            where TComponent : ScriptableObject 
            where TDependency : ScriptableObject
        {
            RegisterComponentDependency(typeof(TComponent), typeof(TDependency));
        }

        /// <summary>
        /// Register component dependencies for initialization ordering
        /// </summary>
        public void RegisterComponentDependency(Type componentType, Type dependencyType)
        {
            if (componentType == null || dependencyType == null) return;

            if (!_componentDependencies.ContainsKey(componentType))
            {
                _componentDependencies[componentType] = new List<Type>();
            }

            if (!_componentDependencies[componentType].Contains(dependencyType))
            {
                _componentDependencies[componentType].Add(dependencyType);

                if (_enableLogging)
                    Debug.Log($"ComponentManager: Registered dependency {componentType.Name} -> {dependencyType.Name}");
            }
        }

        /// <summary>
        /// Initialize a component immediately (bypassing queue)
        /// </summary>
        public void InitializeComponentImmediate(GameEntity entity, ScriptableObject component)
        {
            if (entity == null || component == null) return;

            var componentType = component.GetType();
            var instance = new ComponentInstance(entity, component);

            try
            {
                // Call IEntityComponent.Initialize if implemented
                if (component is IEntityComponent entityComponent)
                {
                    entityComponent.Initialize(entity);
                }

                // Call lifecycle handler if registered
                if (_lifecycleHandlers.TryGetValue(componentType, out var handler))
                {
                    handler.OnComponentAdded(entity, component);
                }

                _activeComponents.Add(instance);

                if (_enableLogging)
                    Debug.Log($"ComponentManager: Initialized component {componentType.Name} on entity {entity.EntityId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ComponentManager: Error initializing component {componentType.Name} on entity {entity.EntityId}: {ex}");
            }
        }

        /// <summary>
        /// Cleanup a component immediately (bypassing queue)
        /// </summary>
        public void CleanupComponentImmediate(GameEntity entity, ScriptableObject component)
        {
            if (entity == null || component == null) return;

            var componentType = component.GetType();
            var instance = new ComponentInstance(entity, component);

            try
            {
                // Call lifecycle handler if registered
                if (_lifecycleHandlers.TryGetValue(componentType, out var handler))
                {
                    handler.OnComponentRemoved(entity, component);
                }

                // Call IEntityComponent.Cleanup if implemented
                if (component is IEntityComponent entityComponent)
                {
                    entityComponent.Cleanup();
                }

                _activeComponents.Remove(instance);

                if (_enableLogging)
                    Debug.Log($"ComponentManager: Cleaned up component {componentType.Name} on entity {entity.EntityId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ComponentManager: Error cleaning up component {componentType.Name} on entity {entity.EntityId}: {ex}");
            }
        }

        /// <summary>
        /// Queue a component for initialization
        /// </summary>
        private void QueueComponentInitialization(GameEntity entity, ScriptableObject component)
        {
            if (_initializationQueue.Count >= _maxInitializationQueueSize)
            {
                // Process oldest request immediately to make room
                var oldestRequest = _initializationQueue.Dequeue();
                InitializeComponentImmediate(oldestRequest.Entity, oldestRequest.Component);
            }

            _initializationQueue.Enqueue(new ComponentInitializationRequest(entity, component));
        }

        /// <summary>
        /// Queue a component for cleanup
        /// </summary>
        private void QueueComponentCleanup(GameEntity entity, ScriptableObject component)
        {
            _cleanupQueue.Enqueue(new ComponentCleanupRequest(entity, component));
        }

        /// <summary>
        /// Process the initialization queue
        /// </summary>
        private void ProcessInitializationQueue()
        {
            int processedCount = 0;
            int maxProcessPerFrame = 10; // Limit processing to avoid frame drops

            while (_initializationQueue.Count > 0 && processedCount < maxProcessPerFrame)
            {
                var request = _initializationQueue.Dequeue();
                
                // Check if dependencies are satisfied
                if (AreDependenciesSatisfied(request.Entity, request.Component.GetType()))
                {
                    InitializeComponentImmediate(request.Entity, request.Component);
                    processedCount++;
                }
                else
                {
                    // Re-queue for later processing
                    _initializationQueue.Enqueue(request);
                    break; // Avoid infinite loop
                }
            }
        }

        /// <summary>
        /// Process the cleanup queue
        /// </summary>
        private void ProcessCleanupQueue()
        {
            int processedCount = 0;
            int maxProcessPerFrame = 20; // Cleanup can be faster than initialization

            while (_cleanupQueue.Count > 0 && processedCount < maxProcessPerFrame)
            {
                var request = _cleanupQueue.Dequeue();
                CleanupComponentImmediate(request.Entity, request.Component);
                processedCount++;
            }
        }

        /// <summary>
        /// Check if all dependencies for a component are satisfied
        /// </summary>
        private bool AreDependenciesSatisfied(GameEntity entity, Type componentType)
        {
            if (!_componentDependencies.TryGetValue(componentType, out var dependencies))
                return true; // No dependencies

            foreach (var dependencyType in dependencies)
            {
                if (!entity.HasComponent(dependencyType))
                    return false;
            }

            return true;
        }

        #region Event Handlers

        private void OnComponentAdded(ComponentAddedEvent evt)
        {
            if (evt.Source != null && evt.Component != null)
            {
                QueueComponentInitialization(evt.Source, evt.Component);
            }
        }

        private void OnComponentRemoved(ComponentRemovedEvent evt)
        {
            if (evt.Source != null && evt.Component != null)
            {
                QueueComponentCleanup(evt.Source, evt.Component);
            }
        }

        private void OnEntityDestroyed(EntityDestroyedEvent evt)
        {
            if (evt.Source == null) return;

            // Cleanup all components on the destroyed entity
            var componentsToCleanup = _activeComponents
                .Where(instance => instance.Entity == evt.Source)
                .ToList();

            foreach (var instance in componentsToCleanup)
            {
                var componentType = instance.Component.GetType();
                
                try
                {
                    // Call lifecycle handler if registered
                    if (_lifecycleHandlers.TryGetValue(componentType, out var handler))
                    {
                        handler.OnEntityDestroyed(instance.Entity, instance.Component);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"ComponentManager: Error in entity destroyed handler for component {componentType.Name}: {ex}");
                }

                _activeComponents.Remove(instance);
            }
        }

        #endregion

        /// <summary>
        /// Get statistics about the component manager
        /// </summary>
        public ComponentManagerStats GetStats()
        {
            return new ComponentManagerStats
            {
                ActiveComponents = _activeComponents.Count,
                InitializationQueueSize = _initializationQueue.Count,
                CleanupQueueSize = _cleanupQueue.Count,
                RegisteredLifecycleHandlers = _lifecycleHandlers.Count,
                RegisteredDependencies = _componentDependencies.Values.Sum(list => list.Count)
            };
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (EventBus.Instance != null)
            {
                EventBus.Instance.Unsubscribe<ComponentAddedEvent>(OnComponentAdded);
                EventBus.Instance.Unsubscribe<ComponentRemovedEvent>(OnComponentRemoved);
                EventBus.Instance.Unsubscribe<EntityDestroyedEvent>(OnEntityDestroyed);
            }
        }
    }

    /// <summary>
    /// Request for component initialization
    /// </summary>
    internal struct ComponentInitializationRequest
    {
        public GameEntity Entity;
        public ScriptableObject Component;

        public ComponentInitializationRequest(GameEntity entity, ScriptableObject component)
        {
            Entity = entity;
            Component = component;
        }
    }

    /// <summary>
    /// Request for component cleanup
    /// </summary>
    internal struct ComponentCleanupRequest
    {
        public GameEntity Entity;
        public ScriptableObject Component;

        public ComponentCleanupRequest(GameEntity entity, ScriptableObject component)
        {
            Entity = entity;
            Component = component;
        }
    }

    /// <summary>
    /// Represents an active component instance
    /// </summary>
    internal struct ComponentInstance : IEquatable<ComponentInstance>
    {
        public GameEntity Entity;
        public ScriptableObject Component;

        public ComponentInstance(GameEntity entity, ScriptableObject component)
        {
            Entity = entity;
            Component = component;
        }

        public bool Equals(ComponentInstance other)
        {
            return Entity == other.Entity && Component == other.Component;
        }

        public override bool Equals(object obj)
        {
            return obj is ComponentInstance other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Entity, Component);
        }
    }

    /// <summary>
    /// Statistics about the ComponentManager
    /// </summary>
    [Serializable]
    public struct ComponentManagerStats
    {
        public int ActiveComponents;
        public int InitializationQueueSize;
        public int CleanupQueueSize;
        public int RegisteredLifecycleHandlers;
        public int RegisteredDependencies;
    }
}