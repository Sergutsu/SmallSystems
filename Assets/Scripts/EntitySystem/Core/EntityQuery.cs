using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GalacticVentures.EntitySystem.Core
{
    /// <summary>
    /// Represents a query for entities with specific criteria
    /// </summary>
    [Serializable]
    public class EntityQuery
    {
        [SerializeField] private List<Type> _requiredComponents = new();
        [SerializeField] private List<Type> _excludedComponents = new();
        [SerializeField] private List<EntityFaction> _allowedFactions = new();
        [SerializeField] private List<EntityFaction> _excludedFactions = new();
        [SerializeField] private Vector3 _centerPosition;
        [SerializeField] private float _radius = -1f; // -1 means no spatial constraint
        [SerializeField] private bool _useCache = true;
        [SerializeField] private string _queryId;

        public IReadOnlyList<Type> RequiredComponents => _requiredComponents;
        public IReadOnlyList<Type> ExcludedComponents => _excludedComponents;
        public IReadOnlyList<EntityFaction> AllowedFactions => _allowedFactions;
        public IReadOnlyList<EntityFaction> ExcludedFactions => _excludedFactions;
        public Vector3 CenterPosition => _centerPosition;
        public float Radius => _radius;
        public bool UseCache => _useCache;
        public string QueryId => _queryId;

        public EntityQuery()
        {
            _queryId = GenerateQueryId();
        }

        /// <summary>
        /// Require entities to have a specific component
        /// </summary>
        public EntityQuery WithComponent<T>() where T : ScriptableObject
        {
            return WithComponent(typeof(T));
        }

        /// <summary>
        /// Require entities to have a specific component
        /// </summary>
        public EntityQuery WithComponent(Type componentType)
        {
            if (componentType != null && !_requiredComponents.Contains(componentType))
            {
                _requiredComponents.Add(componentType);
            }
            return this;
        }

        /// <summary>
        /// Exclude entities that have a specific component
        /// </summary>
        public EntityQuery WithoutComponent<T>() where T : ScriptableObject
        {
            return WithoutComponent(typeof(T));
        }

        /// <summary>
        /// Exclude entities that have a specific component
        /// </summary>
        public EntityQuery WithoutComponent(Type componentType)
        {
            if (componentType != null && !_excludedComponents.Contains(componentType))
            {
                _excludedComponents.Add(componentType);
            }
            return this;
        }

        /// <summary>
        /// Only include entities from specific factions
        /// </summary>
        public EntityQuery WithFaction(EntityFaction faction)
        {
            if (!_allowedFactions.Contains(faction))
            {
                _allowedFactions.Add(faction);
            }
            return this;
        }

        /// <summary>
        /// Only include entities from specific factions
        /// </summary>
        public EntityQuery WithFactions(params EntityFaction[] factions)
        {
            foreach (var faction in factions)
            {
                WithFaction(faction);
            }
            return this;
        }

        /// <summary>
        /// Exclude entities from specific factions
        /// </summary>
        public EntityQuery WithoutFaction(EntityFaction faction)
        {
            if (!_excludedFactions.Contains(faction))
            {
                _excludedFactions.Add(faction);
            }
            return this;
        }

        /// <summary>
        /// Only include entities within a specific radius of a position
        /// </summary>
        public EntityQuery WithinRadius(Vector3 center, float radius)
        {
            _centerPosition = center;
            _radius = radius;
            return this;
        }

        /// <summary>
        /// Enable or disable query result caching
        /// </summary>
        public EntityQuery WithCaching(bool useCache)
        {
            _useCache = useCache;
            return this;
        }

        /// <summary>
        /// Create a copy of this query
        /// </summary>
        public EntityQuery Clone()
        {
            var clone = new EntityQuery();
            clone._requiredComponents.AddRange(_requiredComponents);
            clone._excludedComponents.AddRange(_excludedComponents);
            clone._allowedFactions.AddRange(_allowedFactions);
            clone._excludedFactions.AddRange(_excludedFactions);
            clone._centerPosition = _centerPosition;
            clone._radius = _radius;
            clone._useCache = _useCache;
            return clone;
        }

        /// <summary>
        /// Generate a unique query ID for caching
        /// </summary>
        private string GenerateQueryId()
        {
            return $"Query_{Guid.NewGuid().ToString("N")[..8]}";
        }

        /// <summary>
        /// Generate a hash code for this query (used for caching)
        /// </summary>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            
            foreach (var component in _requiredComponents.OrderBy(t => t.Name))
                hash.Add(component);
            
            foreach (var component in _excludedComponents.OrderBy(t => t.Name))
                hash.Add(component);
            
            foreach (var faction in _allowedFactions.OrderBy(f => f))
                hash.Add(faction);
            
            foreach (var faction in _excludedFactions.OrderBy(f => f))
                hash.Add(faction);
            
            if (_radius > 0)
            {
                hash.Add(_centerPosition);
                hash.Add(_radius);
            }
            
            return hash.ToHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is not EntityQuery other) return false;
            
            return _requiredComponents.SequenceEqual(other._requiredComponents) &&
                   _excludedComponents.SequenceEqual(other._excludedComponents) &&
                   _allowedFactions.SequenceEqual(other._allowedFactions) &&
                   _excludedFactions.SequenceEqual(other._excludedFactions) &&
                   _centerPosition.Equals(other._centerPosition) &&
                   Mathf.Approximately(_radius, other._radius);
        }
    }
}