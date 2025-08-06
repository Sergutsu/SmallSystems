using System;
using UnityEngine;

namespace GalacticVentures.EntitySystem.Core
{
    /// <summary>
    /// Base class for all game events in the entity system
    /// </summary>
    [Serializable]
    public abstract class GameEvent : IGameEvent
    {
        [SerializeField] private string _eventId;
        [SerializeField] private DateTime _timestamp;
        [SerializeField] private GameEntity _source;
        [SerializeField] private EventPriority _priority;

        public string EventId => _eventId;
        public DateTime Timestamp => _timestamp;
        public GameEntity Source => _source;
        public EventPriority Priority => _priority;

        protected GameEvent(GameEntity source = null, EventPriority priority = EventPriority.Normal)
        {
            _eventId = GenerateEventId();
            _timestamp = DateTime.UtcNow;
            _source = source;
            _priority = priority;
        }

        private string GenerateEventId()
        {
            return $"{GetType().Name}_{Guid.NewGuid().ToString("N")[..8]}_{DateTimeOffset.UtcNow.Ticks}";
        }
    }
}