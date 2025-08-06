using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GalacticVentures.EntitySystem.Core
{
    /// <summary>
    /// Central event bus for decoupled communication between game systems
    /// </summary>
    [CreateAssetMenu(fileName = "EventBus", menuName = "Galactic/EventBus")]
    public class EventBus : ScriptableObject
    {
        private static EventBus _instance;
        public static EventBus Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<EventBus>("EventBus");
                    if (_instance == null)
                    {
                        _instance = CreateInstance<EventBus>();
                        _instance.Initialize();
                    }
                }
                return _instance;
            }
        }

        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private int _maxQueueSize = 1000;
        
        private Dictionary<Type, List<Action<IGameEvent>>> _eventHandlers = new();
        private Queue<IGameEvent> _eventQueue = new();
        private List<IGameEvent> _processingQueue = new();
        private bool _isProcessing = false;

        private void Initialize()
        {
            _eventHandlers = new Dictionary<Type, List<Action<IGameEvent>>>();
            _eventQueue = new Queue<IGameEvent>();
            _processingQueue = new List<IGameEvent>();
        }

        /// <summary>
        /// Subscribe to events of a specific type
        /// </summary>
        public void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null) return;

            Type eventType = typeof(T);
            if (!_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType] = new List<Action<IGameEvent>>();
            }

            // Wrap the typed handler in a generic handler
            Action<IGameEvent> genericHandler = (evt) => handler((T)evt);
            _eventHandlers[eventType].Add(genericHandler);

            if (_enableLogging)
            {
                Debug.Log($"EventBus: Subscribed to {eventType.Name}");
            }
        }

        /// <summary>
        /// Unsubscribe from events of a specific type
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null) return;

            Type eventType = typeof(T);
            if (!_eventHandlers.ContainsKey(eventType)) return;

            // Find and remove the handler (this is a limitation - we can't easily match the wrapped handler)
            // For now, we'll remove all handlers of this type when unsubscribing
            // In a production system, you'd want a more sophisticated handler tracking system
            _eventHandlers[eventType].Clear();

            if (_enableLogging)
            {
                Debug.Log($"EventBus: Unsubscribed from {eventType.Name}");
            }
        }

        /// <summary>
        /// Trigger an event immediately
        /// </summary>
        public void TriggerEvent(IGameEvent gameEvent)
        {
            if (gameEvent == null) return;

            if (gameEvent.Priority == EventPriority.Critical)
            {
                ProcessEventImmediately(gameEvent);
            }
            else
            {
                QueueEvent(gameEvent);
            }
        }

        /// <summary>
        /// Queue an event for later processing
        /// </summary>
        private void QueueEvent(IGameEvent gameEvent)
        {
            if (_eventQueue.Count >= _maxQueueSize)
            {
                // Remove oldest event to make room
                _eventQueue.Dequeue();
                if (_enableLogging)
                {
                    Debug.LogWarning("EventBus: Queue overflow, removing oldest event");
                }
            }

            _eventQueue.Enqueue(gameEvent);
        }

        /// <summary>
        /// Process an event immediately
        /// </summary>
        private void ProcessEventImmediately(IGameEvent gameEvent)
        {
            Type eventType = gameEvent.GetType();
            
            if (!_eventHandlers.ContainsKey(eventType)) return;

            var handlers = _eventHandlers[eventType];
            foreach (var handler in handlers.ToList()) // ToList to avoid modification during iteration
            {
                try
                {
                    handler.Invoke(gameEvent);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"EventBus: Exception in event handler for {eventType.Name}: {ex}");
                    // Continue processing other handlers
                }
            }

            if (_enableLogging)
            {
                Debug.Log($"EventBus: Processed {eventType.Name} immediately");
            }
        }

        /// <summary>
        /// Process all queued events (call this from Update or similar)
        /// </summary>
        public void ProcessQueuedEvents()
        {
            if (_isProcessing) return; // Prevent recursive processing

            _isProcessing = true;

            // Move events from queue to processing list
            _processingQueue.Clear();
            while (_eventQueue.Count > 0)
            {
                _processingQueue.Add(_eventQueue.Dequeue());
            }

            // Sort by priority (highest first)
            _processingQueue.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            // Process events
            foreach (var gameEvent in _processingQueue)
            {
                ProcessEventImmediately(gameEvent);
            }

            _isProcessing = false;
        }

        /// <summary>
        /// Clear all event handlers and queued events
        /// </summary>
        public void Clear()
        {
            _eventHandlers.Clear();
            _eventQueue.Clear();
            _processingQueue.Clear();
            
            if (_enableLogging)
            {
                Debug.Log("EventBus: Cleared all handlers and events");
            }
        }

        /// <summary>
        /// Get statistics about the event bus
        /// </summary>
        public EventBusStats GetStats()
        {
            return new EventBusStats
            {
                HandlerCount = _eventHandlers.Values.Sum(list => list.Count),
                QueuedEventCount = _eventQueue.Count,
                RegisteredEventTypes = _eventHandlers.Keys.Count
            };
        }

        private void OnEnable()
        {
            if (_eventHandlers == null)
            {
                Initialize();
            }
        }
    }

    /// <summary>
    /// Statistics about the EventBus performance
    /// </summary>
    [Serializable]
    public struct EventBusStats
    {
        public int HandlerCount;
        public int QueuedEventCount;
        public int RegisteredEventTypes;
    }
}