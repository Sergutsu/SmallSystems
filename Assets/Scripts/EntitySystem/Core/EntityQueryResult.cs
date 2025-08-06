using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GalacticVentures.EntitySystem.Core
{
    /// <summary>
    /// Result of an entity query with caching and performance information
    /// </summary>
    [Serializable]
    public class EntityQueryResult
    {
        [SerializeField] private List<GameEntity> _entities = new();
        [SerializeField] private DateTime _timestamp;
        [SerializeField] private float _executionTimeMs;
        [SerializeField] private string _queryId;
        [SerializeField] private int _totalEntitiesChecked;
        [SerializeField] private bool _wasCached;

        public IReadOnlyList<GameEntity> Entities => _entities;
        public DateTime Timestamp => _timestamp;
        public float ExecutionTimeMs => _executionTimeMs;
        public string QueryId => _queryId;
        public int Count => _entities.Count;
        public int TotalEntitiesChecked => _totalEntitiesChecked;
        public bool WasCached => _wasCached;

        public EntityQueryResult(string queryId)
        {
            _queryId = queryId;
            _timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Add entities to the result
        /// </summary>
        internal void AddEntities(IEnumerable<GameEntity> entities)
        {
            _entities.AddRange(entities.Where(e => e != null));
        }

        /// <summary>
        /// Set performance metrics
        /// </summary>
        internal void SetMetrics(float executionTimeMs, int totalEntitiesChecked, bool wasCached = false)
        {
            _executionTimeMs = executionTimeMs;
            _totalEntitiesChecked = totalEntitiesChecked;
            _wasCached = wasCached;
        }

        /// <summary>
        /// Get the first entity in the result, or null if empty
        /// </summary>
        public GameEntity FirstOrDefault()
        {
            return _entities.FirstOrDefault();
        }

        /// <summary>
        /// Get entities of a specific type
        /// </summary>
        public IEnumerable<T> OfType<T>() where T : GameEntity
        {
            return _entities.OfType<T>();
        }

        /// <summary>
        /// Filter entities by an additional predicate
        /// </summary>
        public IEnumerable<GameEntity> Where(Func<GameEntity, bool> predicate)
        {
            return _entities.Where(predicate);
        }

        /// <summary>
        /// Sort entities by distance from a point
        /// </summary>
        public IEnumerable<GameEntity> OrderByDistanceFrom(Vector3 point)
        {
            return _entities.OrderBy(e => Vector3.Distance(e.transform.position, point));
        }

        /// <summary>
        /// Group entities by faction
        /// </summary>
        public IEnumerable<IGrouping<EntityFaction, GameEntity>> GroupByFaction()
        {
            return _entities.GroupBy(e => e.Faction);
        }

        /// <summary>
        /// Check if the result is still valid (entities haven't been destroyed)
        /// </summary>
        public bool IsValid()
        {
            return _entities.All(e => e != null);
        }

        /// <summary>
        /// Remove null entities from the result
        /// </summary>
        public void CleanupNullEntities()
        {
            _entities.RemoveAll(e => e == null);
        }
    }
}