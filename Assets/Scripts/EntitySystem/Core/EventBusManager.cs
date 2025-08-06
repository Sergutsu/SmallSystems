using UnityEngine;

namespace GalacticVentures.EntitySystem.Core
{
    /// <summary>
    /// MonoBehaviour component that manages EventBus processing in the Unity update loop
    /// </summary>
    public class EventBusManager : MonoBehaviour
    {
        [SerializeField] private EventBus _eventBus;
        [SerializeField] private bool _processEventsInUpdate = true;
        [SerializeField] private bool _showStats = false;

        private void Awake()
        {
            // Ensure we have an EventBus reference
            if (_eventBus == null)
            {
                _eventBus = EventBus.Instance;
            }
        }

        private void Update()
        {
            if (_processEventsInUpdate && _eventBus != null)
            {
                _eventBus.ProcessQueuedEvents();
            }
        }

        private void OnGUI()
        {
            if (_showStats && _eventBus != null)
            {
                var stats = _eventBus.GetStats();
                GUILayout.BeginArea(new Rect(10, 10, 300, 100));
                GUILayout.Label($"EventBus Stats:");
                GUILayout.Label($"Handlers: {stats.HandlerCount}");
                GUILayout.Label($"Queued Events: {stats.QueuedEventCount}");
                GUILayout.Label($"Event Types: {stats.RegisteredEventTypes}");
                GUILayout.EndArea();
            }
        }

        /// <summary>
        /// Manually process queued events
        /// </summary>
        public void ProcessEvents()
        {
            _eventBus?.ProcessQueuedEvents();
        }
    }
}