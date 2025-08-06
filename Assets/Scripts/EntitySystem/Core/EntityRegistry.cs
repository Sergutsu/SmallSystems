using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GalacticVentures.EntitySystem.Events;

namespace GalacticVentures.EntitySystem.Core
{
    /// <summary>
    /// Central registry for all game entities with indexing and querying capabilities
    /// </summary>
    [CreateAssetMenu(fileName = "EntityRegistry", menuName = "Galactic/EntityRegistry")]
    public class EntityRegistry : ScriptableObject
    {
        private static EntityRegistry _instance;
        public static EntityRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<EntityRegistry>("EntityRegistry");
                    if (_instance == null)
                    {
                        _instance = CreateInstance<EntityRegistry>();
                        _instance.Initialize();
                    }
                }
                return _instance;
            }
        }

        [SerializeField] private bool _enableLogging = true;
        
        // Primary entity storage
        private Dictionary<string, GameEntity> _entities = new();
        
        // Faction-based groupings for efficient queries
        private Dictionary<EntityFaction, HashSet<string>> _factionGroups = new();
        
        // Component-based indexing for fast component queries
        private Dictionary<Type, HashSet<string>> _componentIndex = new();
        
        // Spatial indexing for location-based queries (optional)
        private Dictionary<Vector3Int, HashSet<string>> _spatialIndex = new();
        private const float SPATIAL_GRID_SIZE = 100f;

        private void Initialize()
        {
            _entities = new Dictionary<string, GameEntity>();
            _factionGroups = new Dictionary<EntityFaction, HashSet<string>>();
            _componentIndex = new Dictionary<Type, HashSet<string>>();
            _spatialIndex = new Dictionary<Vector3Int, HashSet<string>>();
            
            // Initialize faction groups
            foreach (EntityFaction faction in Enum.GetValues(typeof(EntityFaction)))
            {
                _factionGroups[faction] = new HashSet<string>();
            }
        }

        /// <summary>
        /// Register a new entity in the registry
        /// </summary>
        public bool RegisterEntity(GameEntity entity)
        {
            if (entity == null || string.IsNullOrEmpty(entity.EntityId))
            {
                if (_enableLogging)
                    Debug.LogError("EntityRegistry: Cannot register null entity or entity with empty ID");
                return false;
            }

            if (_entities.ContainsKey(entity.EntityId))
            {
                if (_enableLogging)
                    Debug.LogWarning($"EntityRegistry: Entity {entity.EntityId} already registered");
                return false;
            }

            // Add to primary storage
            _entities[entity.EntityId] = entity;
            
            // Add to faction group
            _factionGroups[entity.Faction].Add(entity.EntityId);
            
            // Add to spatial index if entity has position
            UpdateSpatialIndex(entity.EntityId, entity.transform.position);
            
            // Trigger event
            EventBus.Instance.TriggerEvent(new EntityCreatedEvent(entity, entity.EntityId, entity.Faction));
            
            if (_enableLogging)
                Debug.Log($"EntityRegistry: Registered entity {entity.EntityId} with faction {entity.Faction}");
            
            return true;
        }

        /// <summary>
        /// Unregister an entity from the registry
        /// </summary>
        public bool UnregisterEntity(string entityId)
        {
            if (string.IsNullOrEmpty(entityId) || !_entities.ContainsKey(entityId))
            {
                return false;
            }

            var entity = _entities[entityId];
            var faction = entity.Faction;
            
            // Remove from primary storage
            _entities.Remove(entityId);
            
            // Remove from faction group
            _factionGroups[faction].Remove(entityId);
            
            // Remove from component indices
            foreach (var componentSet in _componentIndex.Values)
            {
                componentSet.Remove(entityId);
            }
            
            // Remove from spatial index
            RemoveFromSpatialIndex(entityId);
            
            // Trigger event
            EventBus.Instance.TriggerEvent(new EntityDestroyedEvent(entity, entityId, faction));
            
            if (_enableLogging)
                Debug.Log($"EntityRegistry: Unregistered entity {entityId}");
            
            return true;
        }

        /// <summary>
        /// Get an entity by ID
        /// </summary>
        public GameEntity GetEntity(string entityId)
        {
            if (string.IsNullOrEmpty(entityId))
                return null;
                
            _entities.TryGetValue(entityId, out var entity);
            return entity;
        }

        /// <summary>
        /// Get all entities belonging to a specific faction
        /// </summary>
        public IEnumerable<GameEntity> GetEntitiesByFaction(EntityFaction faction)
        {
            if (!_factionGroups.ContainsKey(faction))
                return Enumerable.Empty<GameEntity>();

            return _factionGroups[faction]
                .Where(id => _entities.ContainsKey(id))
                .Select(id => _entities[id]);
        }

        /// <summary>
        /// Get all entities that have a specific component type
        /// </summary>
        public IEnumerable<GameEntity> GetEntitiesWithComponent<T>() where T : ScriptableObject
        {
            return GetEntitiesWithComponent(typeof(T));
        }

        /// <summary>
        /// Get all entities that have a specific component type
        /// </summary>
        public IEnumerable<GameEntity> GetEntitiesWithComponent(Type componentType)
        {
            if (!_componentIndex.ContainsKey(componentType))
                return Enumerable.Empty<GameEntity>();

            return _componentIndex[componentType]
                .Where(id => _entities.ContainsKey(id))
                .Select(id => _entities[id]);
        }

        /// <summary>
        /// Update component index when a component is added to an entity
        /// </summary>
        public void OnComponentAdded(string entityId, Type componentType)
        {
            if (string.IsNullOrEmpty(entityId) || componentType == null)
                return;

            if (!_componentIndex.ContainsKey(componentType))
            {
                _componentIndex[componentType] = new HashSet<string>();
            }

            _componentIndex[componentType].Add(entityId);
        }

        /// <summary>
        /// Update component index when a component is removed from an entity
        /// </summary>
        public void OnComponentRemoved(string entityId, Type componentType)
        {
            if (string.IsNullOrEmpty(entityId) || componentType == null)
                return;

            if (_componentIndex.ContainsKey(componentType))
            {
                _componentIndex[componentType].Remove(entityId);
            }
        }

        /// <summary>
        /// Update faction grouping when an entity's faction changes
        /// </summary>
        public void OnFactionChanged(string entityId, EntityFaction oldFaction, EntityFaction newFaction)
        {
            if (string.IsNullOrEmpty(entityId))
                return;

            // Remove from old faction group
            if (_factionGroups.ContainsKey(oldFaction))
            {
                _factionGroups[oldFaction].Remove(entityId);
            }

            // Add to new faction group
            if (!_factionGroups.ContainsKey(newFaction))
            {
                _factionGroups[newFaction] = new HashSet<string>();
            }
            _factionGroups[newFaction].Add(entityId);
        }

        /// <summary>
        /// Update spatial index when an entity moves
        /// </summary>
        public void UpdateSpatialIndex(string entityId, Vector3 position)
        {
            if (string.IsNullOrEmpty(entityId))
                return;

            // Remove from old spatial cell
            RemoveFromSpatialIndex(entityId);

            // Add to new spatial cell
            var gridPos = WorldToGrid(position);
            if (!_spatialIndex.ContainsKey(gridPos))
            {
                _spatialIndex[gridPos] = new HashSet<string>();
            }
            _spatialIndex[gridPos].Add(entityId);
        }

        /// <summary>
        /// Remove entity from spatial index
        /// </summary>
        private void RemoveFromSpatialIndex(string entityId)
        {
            foreach (var spatialSet in _spatialIndex.Values)
            {
                spatialSet.Remove(entityId);
            }
        }

        /// <summary>
        /// Get entities within a specific radius of a position
        /// </summary>
        public IEnumerable<GameEntity> GetEntitiesInRadius(Vector3 center, float radius)
        {
            var results = new HashSet<GameEntity>();
            var radiusSquared = radius * radius;

            // Calculate grid cells to check
            var minGrid = WorldToGrid(center - Vector3.one * radius);
            var maxGrid = WorldToGrid(center + Vector3.one * radius);

            for (int x = minGrid.x; x <= maxGrid.x; x++)
            {
                for (int y = minGrid.y; y <= maxGrid.y; y++)
                {
                    for (int z = minGrid.z; z <= maxGrid.z; z++)
                    {
                        var gridPos = new Vector3Int(x, y, z);
                        if (_spatialIndex.ContainsKey(gridPos))
                        {
                            foreach (var entityId in _spatialIndex[gridPos])
                            {
                                if (_entities.TryGetValue(entityId, out var entity))
                                {
                                    var distanceSquared = (entity.transform.position - center).sqrMagnitude;
                                    if (distanceSquared <= radiusSquared)
                                    {
                                        results.Add(entity);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        private Vector3Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector3Int(
                Mathf.FloorToInt(worldPos.x / SPATIAL_GRID_SIZE),
                Mathf.FloorToInt(worldPos.y / SPATIAL_GRID_SIZE),
                Mathf.FloorToInt(worldPos.z / SPATIAL_GRID_SIZE)
            );
        }

        /// <summary>
        /// Get registry statistics
        /// </summary>
        public EntityRegistryStats GetStats()
        {
            return new EntityRegistryStats
            {
                TotalEntities = _entities.Count,
                FactionCounts = _factionGroups.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count),
                ComponentTypesIndexed = _componentIndex.Count,
                SpatialCellsUsed = _spatialIndex.Count(kvp => kvp.Value.Count > 0)
            };
        }

        /// <summary>
        /// Clear all entities and indices
        /// </summary>
        public void Clear()
        {
            _entities.Clear();
            foreach (var factionGroup in _factionGroups.Values)
            {
                factionGroup.Clear();
            }
            _componentIndex.Clear();
            _spatialIndex.Clear();
            
            if (_enableLogging)
                Debug.Log("EntityRegistry: Cleared all entities and indices");
        }

        private void OnEnable()
        {
            if (_entities == null)
            {
                Initialize();
            }
        }
    }

    /// <summary>
    /// Statistics about the EntityRegistry
    /// </summary>
    [Serializable]
    public struct EntityRegistryStats
    {
        public int TotalEntities;
        public Dictionary<EntityFaction, int> FactionCounts;
        public int ComponentTypesIndexed;
        public int SpatialCellsUsed;
    }
}