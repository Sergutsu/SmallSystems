#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using GalacticVentures.EntitySystem.Core;

namespace GalacticVentures.EntitySystem.Editor
{
    /// <summary>
    /// Debug tools and utilities for the Entity System
    /// </summary>
    public class EntitySystemDebugger : EditorWindow
    {
        private Vector2 _scrollPosition;
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "Performance", "Events", "Components", "Queries" };

        // Performance tracking
        private bool _trackPerformance = false;
        private float _lastUpdateTime;
        private int _frameCount;
        private float _averageFPS;

        // Event tracking
        private bool _trackEvents = false;
        private System.Collections.Generic.List<string> _eventLog = new();
        private int _maxEventLogSize = 100;

        // Query testing
        private string _queryTestResults = "";

        [MenuItem("Tools/Entity System/System Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<EntitySystemDebugger>("Entity System Debugger");
            window.Show();
        }

        private void OnEnable()
        {
            _lastUpdateTime = Time.realtimeSinceStartup;
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Entity System Debugger is only available during Play Mode", MessageType.Info);
                return;
            }

            DrawTabs();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            switch (_selectedTab)
            {
                case 0:
                    DrawPerformanceTab();
                    break;
                case 1:
                    DrawEventsTab();
                    break;
                case 2:
                    DrawComponentsTab();
                    break;
                case 3:
                    DrawQueriesTab();
                    break;
            }

            EditorGUILayout.EndScrollView();

            UpdatePerformanceTracking();
        }

        private void DrawTabs()
        {
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            EditorGUILayout.Space();
        }

        private void DrawPerformanceTab()
        {
            EditorGUILayout.LabelField("Performance Monitoring", EditorStyles.boldLabel);

            _trackPerformance = EditorGUILayout.Toggle("Track Performance", _trackPerformance);

            if (_trackPerformance)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Average FPS: {_averageFPS:F1}");
                EditorGUILayout.LabelField($"Frame Count: {_frameCount}");
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            // Entity Registry Stats
            var registry = EntityRegistry.Instance;
            if (registry != null)
            {
                EditorGUILayout.LabelField("Entity Registry Performance", EditorStyles.boldLabel);
                var stats = registry.GetStats();

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Total Entities: {stats.TotalEntities}");
                EditorGUILayout.LabelField($"Component Types Indexed: {stats.ComponentTypesIndexed}");
                EditorGUILayout.LabelField($"Spatial Cells Used: {stats.SpatialCellsUsed}");

                if (GUILayout.Button("Clear Query Cache"))
                {
                    registry.ClearQueryCache();
                }
                EditorGUILayout.EndVertical();
            }

            // EventBus Stats
            var eventBus = EventBus.Instance;
            if (eventBus != null)
            {
                EditorGUILayout.LabelField("EventBus Performance", EditorStyles.boldLabel);
                var eventStats = eventBus.GetStats();

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Registered Handlers: {eventStats.HandlerCount}");
                EditorGUILayout.LabelField($"Queued Events: {eventStats.QueuedEventCount}");
                EditorGUILayout.LabelField($"Event Types: {eventStats.RegisteredEventTypes}");

                if (GUILayout.Button("Clear Event Queue"))
                {
                    eventBus.Clear();
                }
                EditorGUILayout.EndVertical();
            }

            // ComponentManager Stats
            var componentManager = ComponentManager.Instance;
            if (componentManager != null)
            {
                EditorGUILayout.LabelField("ComponentManager Performance", EditorStyles.boldLabel);
                var componentStats = componentManager.GetStats();

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Active Components: {componentStats.ActiveComponents}");
                EditorGUILayout.LabelField($"Initialization Queue: {componentStats.InitializationQueueSize}");
                EditorGUILayout.LabelField($"Cleanup Queue: {componentStats.CleanupQueueSize}");
                EditorGUILayout.LabelField($"Lifecycle Handlers: {componentStats.RegisteredLifecycleHandlers}");
                EditorGUILayout.LabelField($"Dependencies: {componentStats.RegisteredDependencies}");
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawEventsTab()
        {
            EditorGUILayout.LabelField("Event System Monitoring", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _trackEvents = EditorGUILayout.Toggle("Track Events", _trackEvents);
            if (GUILayout.Button("Clear Log"))
            {
                _eventLog.Clear();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (_trackEvents)
            {
                EditorGUILayout.LabelField($"Event Log ({_eventLog.Count}/{_maxEventLogSize})", EditorStyles.boldLabel);

                EditorGUILayout.BeginVertical("box");
                foreach (var logEntry in _eventLog.TakeLast(20)) // Show last 20 events
                {
                    EditorGUILayout.LabelField(logEntry, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            // Manual event testing
            EditorGUILayout.LabelField("Manual Event Testing", EditorStyles.boldLabel);

            if (GUILayout.Button("Trigger Test Event"))
            {
                // Create a test event
                var testEvent = new TestDebugEvent();
                EventBus.Instance.TriggerEvent(testEvent);
                LogEvent($"Triggered TestDebugEvent: {testEvent.EventId}");
            }
        }

        private void DrawComponentsTab()
        {
            EditorGUILayout.LabelField("Component Analysis", EditorStyles.boldLabel);

            var registry = EntityRegistry.Instance;
            if (registry == null) return;

            // Component type distribution
            var allEntities = registry.CreateQuery().ExecuteQuery(registry.CreateQuery()).Entities;
            var componentTypes = new System.Collections.Generic.Dictionary<System.Type, int>();

            foreach (var entity in allEntities)
            {
                foreach (var componentType in entity.GetComponentTypes())
                {
                    componentTypes[componentType] = componentTypes.GetValueOrDefault(componentType, 0) + 1;
                }
            }

            EditorGUILayout.LabelField("Component Distribution", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            foreach (var kvp in componentTypes.OrderByDescending(kvp => kvp.Value))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(kvp.Key.Name, GUILayout.Width(200));
                EditorGUILayout.LabelField($"{kvp.Value} entities");
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Component validation
            EditorGUILayout.LabelField("Component Validation", EditorStyles.boldLabel);

            if (GUILayout.Button("Validate All Components"))
            {
                ValidateAllComponents();
            }
        }

        private void DrawQueriesTab()
        {
            EditorGUILayout.LabelField("Query Testing", EditorStyles.boldLabel);

            var registry = EntityRegistry.Instance;
            if (registry == null) return;

            // Predefined query tests
            if (GUILayout.Button("Test: All Entities"))
            {
                var query = registry.CreateQuery();
                var result = registry.ExecuteQuery(query);
                _queryTestResults = $"Found {result.Count} entities in {result.ExecutionTimeMs:F2}ms";
            }

            if (GUILayout.Button("Test: Player Faction"))
            {
                var query = registry.CreateQuery().WithFaction(EntityFaction.Player);
                var result = registry.ExecuteQuery(query);
                _queryTestResults = $"Found {result.Count} Player entities in {result.ExecutionTimeMs:F2}ms";
            }

            if (GUILayout.Button("Test: Spatial Query (Origin, 100 radius)"))
            {
                var query = registry.CreateQuery().WithinRadius(Vector3.zero, 100f);
                var result = registry.ExecuteQuery(query);
                _queryTestResults = $"Found {result.Count} entities within 100 units of origin in {result.ExecutionTimeMs:F2}ms";
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Query Results", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(_queryTestResults);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Query performance testing
            EditorGUILayout.LabelField("Performance Testing", EditorStyles.boldLabel);

            if (GUILayout.Button("Run Query Performance Test"))
            {
                RunQueryPerformanceTest();
            }
        }

        private void UpdatePerformanceTracking()
        {
            if (!_trackPerformance) return;

            _frameCount++;
            var currentTime = Time.realtimeSinceStartup;
            var deltaTime = currentTime - _lastUpdateTime;

            if (deltaTime >= 1f) // Update every second
            {
                _averageFPS = _frameCount / deltaTime;
                _frameCount = 0;
                _lastUpdateTime = currentTime;
                Repaint();
            }
        }

        private void LogEvent(string message)
        {
            if (!_trackEvents) return;

            var timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
            _eventLog.Add($"[{timestamp}] {message}");

            if (_eventLog.Count > _maxEventLogSize)
            {
                _eventLog.RemoveAt(0);
            }

            Repaint();
        }

        private void ValidateAllComponents()
        {
            var registry = EntityRegistry.Instance;
            var allEntities = registry.CreateQuery().ExecuteQuery(registry.CreateQuery()).Entities;

            int validComponents = 0;
            int invalidComponents = 0;

            foreach (var entity in allEntities)
            {
                foreach (var component in entity.GetAllComponents())
                {
                    if (component is IEntityComponent entityComponent)
                    {
                        if (entityComponent.IsValid())
                        {
                            validComponents++;
                        }
                        else
                        {
                            invalidComponents++;
                            Debug.LogWarning($"Invalid component {component.GetType().Name} on entity {entity.EntityId}");
                        }
                    }
                }
            }

            Debug.Log($"Component validation complete: {validComponents} valid, {invalidComponents} invalid");
        }

        private void RunQueryPerformanceTest()
        {
            var registry = EntityRegistry.Instance;
            var iterations = 1000;
            var totalTime = 0f;

            for (int i = 0; i < iterations; i++)
            {
                var query = registry.CreateQuery().WithFaction(EntityFaction.Player);
                var result = registry.ExecuteQuery(query);
                totalTime += result.ExecutionTimeMs;
            }

            var averageTime = totalTime / iterations;
            _queryTestResults = $"Performance test: {iterations} queries averaged {averageTime:F3}ms each";
            Debug.Log(_queryTestResults);
        }

        // Test event class
        private class TestDebugEvent : GameEvent
        {
            public TestDebugEvent() : base(null, EventPriority.Normal)
            {
            }
        }
    }
}
#endif