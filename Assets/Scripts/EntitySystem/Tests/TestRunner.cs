using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GalacticVentures.EntitySystem.Core;

namespace GalacticVentures.EntitySystem.Tests
{
    /// <summary>
    /// Test runner for automated Entity System testing
    /// </summary>
    public class TestRunner : MonoBehaviour
    {
        [SerializeField] private bool _runOnStart = false;
        [SerializeField] private bool _runPerformanceTests = true;
        [SerializeField] private bool _runStressTests = false;
        [SerializeField] private bool _generateReport = true;
        [SerializeField] private float _testTimeout = 300f; // 5 minutes

        private List<TestResult> _testResults = new();
        private bool _testsRunning = false;

        private void Start()
        {
            if (_runOnStart)
            {
                StartCoroutine(RunAllTests());
            }
        }

        /// <summary>
        /// Run all Entity System tests
        /// </summary>
        public void RunAllTests()
        {
            if (!_testsRunning)
            {
                StartCoroutine(RunAllTests());
            }
        }

        private IEnumerator RunAllTests()
        {
            _testsRunning = true;
            _testResults.Clear();

            EntitySystemLogger.LogInfo("TestRunner", "Starting Entity System test suite");
            
            // Initialize systems
            EntitySystemLogger.Configure(EntitySystemLogger.LogLevel.Info, false, 1000);
            MemoryManager.Initialize();
            PerformanceProfiler.SetEnabled(true);
            PerformanceProfiler.ClearProfiles();

            var startTime = Time.realtimeSinceStartup;

            // Run basic functionality tests
            yield return StartCoroutine(RunBasicTests());

            // Run performance tests if enabled
            if (_runPerformanceTests)
            {
                yield return StartCoroutine(RunPerformanceTests());
            }

            // Run stress tests if enabled
            if (_runStressTests)
            {
                yield return StartCoroutine(RunStressTests());
            }

            var totalTime = Time.realtimeSinceStartup - startTime;

            // Generate report
            if (_generateReport)
            {
                GenerateTestReport(totalTime);
            }

            EntitySystemLogger.LogInfo("TestRunner", $"Test suite completed in {totalTime:F2} seconds");
            _testsRunning = false;
        }

        private IEnumerator RunBasicTests()
        {
            EntitySystemLogger.LogInfo("TestRunner", "Running basic functionality tests");

            // Test 1: Entity Creation and Registration
            yield return StartCoroutine(RunTest("Entity Creation", TestEntityCreation));

            // Test 2: Component Management
            yield return StartCoroutine(RunTest("Component Management", TestComponentManagement));

            // Test 3: Event System
            yield return StartCoroutine(RunTest("Event System", TestEventSystem));

            // Test 4: Query System
            yield return StartCoroutine(RunTest("Query System", TestQuerySystem));

            // Test 5: Serialization
            yield return StartCoroutine(RunTest("Serialization", TestSerialization));
        }

        private IEnumerator RunPerformanceTests()
        {
            EntitySystemLogger.LogInfo("TestRunner", "Running performance tests");

            // Test 1: Large Scale Entity Operations
            yield return StartCoroutine(RunTest("Large Scale Operations", TestLargeScaleOperations));

            // Test 2: Query Performance
            yield return StartCoroutine(RunTest("Query Performance", TestQueryPerformance));

            // Test 3: Memory Usage
            yield return StartCoroutine(RunTest("Memory Usage", TestMemoryUsage));
        }

        private IEnumerator RunStressTests()
        {
            EntitySystemLogger.LogInfo("TestRunner", "Running stress tests");

            // Test 1: Massive Entity Creation
            yield return StartCoroutine(RunTest("Massive Entity Creation", TestMassiveEntityCreation));

            // Test 2: High Frequency Operations
            yield return StartCoroutine(RunTest("High Frequency Operations", TestHighFrequencyOperations));
        }

        private IEnumerator RunTest(string testName, System.Func<IEnumerator> testMethod)
        {
            var startTime = Time.realtimeSinceStartup;
            var result = new TestResult { TestName = testName, StartTime = startTime };

            EntitySystemLogger.LogInfo("TestRunner", $"Starting test: {testName}");

            try
            {
                yield return StartCoroutine(testMethod());
                result.Passed = true;
                result.Message = "Test passed successfully";
            }
            catch (System.Exception ex)
            {
                result.Passed = false;
                result.Message = ex.Message;
                EntitySystemLogger.LogError("TestRunner", $"Test failed: {testName}", ex);
            }

            result.Duration = Time.realtimeSinceStartup - startTime;
            _testResults.Add(result);

            EntitySystemLogger.LogInfo("TestRunner", $"Test {testName} completed in {result.Duration:F2}s - {(result.Passed ? "PASSED" : "FAILED")}");
        }

        // Test implementations
        private IEnumerator TestEntityCreation()
        {
            var registry = ScriptableObject.CreateInstance<EntityRegistry>();
            
            try
            {
                // Create test entities
                var entities = new List<GameEntity>();
                for (int i = 0; i < 100; i++)
                {
                    var go = new GameObject($"TestEntity_{i}");
                    var entity = go.AddComponent<TestGameEntity>();
                    entity.Initialize($"test-{i}", EntityFaction.Player);
                    registry.RegisterEntity(entity);
                    entities.Add(entity);
                }

                // Verify registration
                var stats = registry.GetStats();
                if (stats.TotalEntities != 100)
                    throw new System.Exception($"Expected 100 entities, got {stats.TotalEntities}");

                // Cleanup
                foreach (var entity in entities)
                {
                    if (entity != null && entity.gameObject != null)
                        DestroyImmediate(entity.gameObject);
                }
            }
            finally
            {
                if (registry != null)
                    ScriptableObject.DestroyImmediate(registry);
            }

            yield return null;
        }

        private IEnumerator TestComponentManagement()
        {
            var registry = ScriptableObject.CreateInstance<EntityRegistry>();
            
            try
            {
                var go = new GameObject("TestEntity");
                var entity = go.AddComponent<TestGameEntity>();
                entity.Initialize("test-component", EntityFaction.Player);
                registry.RegisterEntity(entity);

                // Test component operations
                var component = ScriptableObject.CreateInstance<TestComponent>();
                entity.AddComponent(component);

                if (!entity.HasComponent<TestComponent>())
                    throw new System.Exception("Component was not added correctly");

                entity.RemoveComponent<TestComponent>();

                if (entity.HasComponent<TestComponent>())
                    throw new System.Exception("Component was not removed correctly");

                // Cleanup
                DestroyImmediate(go);
                ScriptableObject.DestroyImmediate(component);
            }
            finally
            {
                if (registry != null)
                    ScriptableObject.DestroyImmediate(registry);
            }

            yield return null;
        }

        private IEnumerator TestEventSystem()
        {
            var eventBus = ScriptableObject.CreateInstance<EventBus>();
            
            try
            {
                var eventReceived = false;
                eventBus.Subscribe<TestEvent>((evt) => eventReceived = true);

                eventBus.TriggerEvent(new TestEvent());
                eventBus.ProcessQueuedEvents();

                if (!eventReceived)
                    throw new System.Exception("Event was not received");
            }
            finally
            {
                if (eventBus != null)
                    ScriptableObject.DestroyImmediate(eventBus);
            }

            yield return null;
        }

        private IEnumerator TestQuerySystem()
        {
            var registry = ScriptableObject.CreateInstance<EntityRegistry>();
            
            try
            {
                // Create test entities
                var entities = new List<GameEntity>();
                for (int i = 0; i < 50; i++)
                {
                    var go = new GameObject($"QueryTestEntity_{i}");
                    var entity = go.AddComponent<TestGameEntity>();
                    entity.Initialize($"query-test-{i}", i % 2 == 0 ? EntityFaction.Player : EntityFaction.TradingGuild);
                    registry.RegisterEntity(entity);
                    entities.Add(entity);
                }

                // Test faction query
                var playerQuery = registry.CreateQuery().WithFaction(EntityFaction.Player);
                var playerResults = registry.ExecuteQuery(playerQuery);

                if (playerResults.Count != 25)
                    throw new System.Exception($"Expected 25 player entities, got {playerResults.Count}");

                // Cleanup
                foreach (var entity in entities)
                {
                    if (entity != null && entity.gameObject != null)
                        DestroyImmediate(entity.gameObject);
                }
            }
            finally
            {
                if (registry != null)
                    ScriptableObject.DestroyImmediate(registry);
            }

            yield return null;
        }

        private IEnumerator TestSerialization()
        {
            var persistenceManager = gameObject.AddComponent<EntityPersistenceManager>();
            var registry = ScriptableObject.CreateInstance<EntityRegistry>();
            
            try
            {
                // Create test entity
                var go = new GameObject("SerializationTestEntity");
                var entity = go.AddComponent<TestGameEntity>();
                entity.Initialize("serialization-test", EntityFaction.Player);
                registry.RegisterEntity(entity);

                var component = ScriptableObject.CreateInstance<TestSerializableComponent>();
                component.TestValue = 42;
                component.TestString = "Test";
                entity.AddComponent(component);

                // Test save/load
                var testFile = "test_serialization.json";
                var saveResult = persistenceManager.SaveEntities(new[] { entity }, testFile);
                
                if (!saveResult)
                    throw new System.Exception("Failed to save entity");

                // Cleanup and reload
                DestroyImmediate(go);
                registry.Clear();

                var loadResult = persistenceManager.LoadEntities(testFile, true);
                
                if (!loadResult)
                    throw new System.Exception("Failed to load entity");

                var restoredEntity = registry.GetEntity("serialization-test");
                if (restoredEntity == null)
                    throw new System.Exception("Entity was not restored");

                var restoredComponent = restoredEntity.GetComponent<TestSerializableComponent>();
                if (restoredComponent == null || restoredComponent.TestValue != 42)
                    throw new System.Exception("Component was not restored correctly");

                // Cleanup
                persistenceManager.DeleteSaveFile(testFile);
                ScriptableObject.DestroyImmediate(component);
            }
            finally
            {
                if (registry != null)
                    ScriptableObject.DestroyImmediate(registry);
                if (persistenceManager != null)
                    DestroyImmediate(persistenceManager);
            }

            yield return null;
        }

        private IEnumerator TestLargeScaleOperations()
        {
            var registry = ScriptableObject.CreateInstance<EntityRegistry>();
            
            try
            {
                using (PerformanceProfiler.Profile("LargeScaleTest"))
                {
                    var entities = new List<GameEntity>();
                    
                    // Create 1000 entities
                    for (int i = 0; i < 1000; i++)
                    {
                        var go = new GameObject($"LargeScaleEntity_{i}");
                        var entity = go.AddComponent<TestGameEntity>();
                        entity.Initialize($"large-scale-{i}", (EntityFaction)(i % 10 + 1));
                        registry.RegisterEntity(entity);
                        entities.Add(entity);
                        
                        if (i % 100 == 0)
                            yield return null; // Prevent frame drops
                    }

                    var stats = registry.GetStats();
                    if (stats.TotalEntities != 1000)
                        throw new System.Exception($"Expected 1000 entities, got {stats.TotalEntities}");

                    // Cleanup
                    foreach (var entity in entities)
                    {
                        if (entity != null && entity.gameObject != null)
                            DestroyImmediate(entity.gameObject);
                    }
                }
            }
            finally
            {
                if (registry != null)
                    ScriptableObject.DestroyImmediate(registry);
            }
        }

        private IEnumerator TestQueryPerformance()
        {
            var registry = ScriptableObject.CreateInstance<EntityRegistry>();
            
            try
            {
                // Create test entities
                var entities = new List<GameEntity>();
                for (int i = 0; i < 500; i++)
                {
                    var go = new GameObject($"QueryPerfEntity_{i}");
                    var entity = go.AddComponent<TestGameEntity>();
                    entity.Initialize($"query-perf-{i}", (EntityFaction)(i % 5 + 1));
                    go.transform.position = new Vector3(i % 25, 0, i / 25);
                    registry.RegisterEntity(entity);
                    entities.Add(entity);
                }

                using (PerformanceProfiler.Profile("QueryPerformanceTest"))
                {
                    // Run multiple queries
                    for (int i = 0; i < 100; i++)
                    {
                        var query = registry.CreateQuery().WithFaction(EntityFaction.Player);
                        var result = registry.ExecuteQuery(query);
                        
                        if (result.ExecutionTimeMs > 50f) // 50ms threshold
                            throw new System.Exception($"Query took too long: {result.ExecutionTimeMs}ms");
                            
                        if (i % 10 == 0)
                            yield return null;
                    }
                }

                // Cleanup
                foreach (var entity in entities)
                {
                    if (entity != null && entity.gameObject != null)
                        DestroyImmediate(entity.gameObject);
                }
            }
            finally
            {
                if (registry != null)
                    ScriptableObject.DestroyImmediate(registry);
            }
        }

        private IEnumerator TestMemoryUsage()
        {
            var initialMemory = GC.GetTotalMemory(false);
            
            // Create and destroy entities multiple times
            for (int cycle = 0; cycle < 10; cycle++)
            {
                var registry = ScriptableObject.CreateInstance<EntityRegistry>();
                var entities = new List<GameEntity>();
                
                // Create entities
                for (int i = 0; i < 100; i++)
                {
                    var go = new GameObject($"MemTestEntity_{cycle}_{i}");
                    var entity = go.AddComponent<TestGameEntity>();
                    entity.Initialize($"mem-test-{cycle}-{i}", EntityFaction.Player);
                    registry.RegisterEntity(entity);
                    entities.Add(entity);
                }

                // Cleanup
                foreach (var entity in entities)
                {
                    if (entity != null && entity.gameObject != null)
                        DestroyImmediate(entity.gameObject);
                }
                
                ScriptableObject.DestroyImmediate(registry);
                
                if (cycle % 3 == 0)
                {
                    GC.Collect();
                    yield return null;
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = (finalMemory - initialMemory) / 1024f / 1024f;

            if (memoryIncrease > 50f) // 50MB threshold
                throw new System.Exception($"Memory usage increased by {memoryIncrease:F2}MB");
        }

        private IEnumerator TestMassiveEntityCreation()
        {
            var registry = ScriptableObject.CreateInstance<EntityRegistry>();
            
            try
            {
                var entities = new List<GameEntity>();
                var startTime = Time.realtimeSinceStartup;

                // Create 2000 entities
                for (int i = 0; i < 2000; i++)
                {
                    var go = new GameObject($"MassiveEntity_{i}");
                    var entity = go.AddComponent<TestGameEntity>();
                    entity.Initialize($"massive-{i}", (EntityFaction)(i % 10 + 1));
                    registry.RegisterEntity(entity);
                    entities.Add(entity);
                    
                    if (i % 200 == 0)
                        yield return null;
                }

                var duration = Time.realtimeSinceStartup - startTime;
                if (duration > 30f) // 30 second threshold
                    throw new System.Exception($"Massive entity creation took too long: {duration:F2}s");

                // Cleanup
                foreach (var entity in entities)
                {
                    if (entity != null && entity.gameObject != null)
                        DestroyImmediate(entity.gameObject);
                }
            }
            finally
            {
                if (registry != null)
                    ScriptableObject.DestroyImmediate(registry);
            }
        }

        private IEnumerator TestHighFrequencyOperations()
        {
            var registry = ScriptableObject.CreateInstance<EntityRegistry>();
            
            try
            {
                var entity = CreateTestEntity(registry, "high-freq-test", EntityFaction.Player);
                var startTime = Time.realtimeSinceStartup;

                // Perform high frequency operations
                for (int i = 0; i < 1000; i++)
                {
                    var component = ScriptableObject.CreateInstance<TestComponent>();
                    entity.AddComponent(component);
                    entity.RemoveComponent<TestComponent>();
                    ScriptableObject.DestroyImmediate(component);
                    
                    if (i % 100 == 0)
                        yield return null;
                }

                var duration = Time.realtimeSinceStartup - startTime;
                if (duration > 10f) // 10 second threshold
                    throw new System.Exception($"High frequency operations took too long: {duration:F2}s");

                // Cleanup
                DestroyImmediate(entity.gameObject);
            }
            finally
            {
                if (registry != null)
                    ScriptableObject.DestroyImmediate(registry);
            }
        }

        private TestGameEntity CreateTestEntity(EntityRegistry registry, string id, EntityFaction faction)
        {
            var go = new GameObject($"Entity_{id}");
            var entity = go.AddComponent<TestGameEntity>();
            entity.Initialize(id, faction);
            registry.RegisterEntity(entity);
            return entity;
        }

        private void GenerateTestReport(float totalTime)
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("ENTITY SYSTEM TEST REPORT");
            report.AppendLine("========================");
            report.AppendLine($"Generated: {System.DateTime.Now}");
            report.AppendLine($"Total Duration: {totalTime:F2} seconds");
            report.AppendLine();

            var passedTests = _testResults.Count(r => r.Passed);
            var failedTests = _testResults.Count(r => !r.Passed);

            report.AppendLine("SUMMARY");
            report.AppendLine("-------");
            report.AppendLine($"Total Tests: {_testResults.Count}");
            report.AppendLine($"Passed: {passedTests}");
            report.AppendLine($"Failed: {failedTests}");
            report.AppendLine($"Success Rate: {(passedTests / (float)_testResults.Count * 100):F1}%");
            report.AppendLine();

            report.AppendLine("TEST RESULTS");
            report.AppendLine("------------");
            foreach (var result in _testResults)
            {
                var status = result.Passed ? "PASS" : "FAIL";
                report.AppendLine($"{result.TestName,-30} | {status,-4} | {result.Duration,6:F2}s | {result.Message}");
            }

            report.AppendLine();
            report.AppendLine("PERFORMANCE DATA");
            report.AppendLine("----------------");
            report.Append(PerformanceProfiler.GenerateReport());

            report.AppendLine();
            report.AppendLine("MEMORY DATA");
            report.AppendLine("-----------");
            report.Append(MemoryManager.GenerateMemoryReport());

            var reportPath = System.IO.Path.Combine(Application.dataPath, "EntitySystemTestReport.txt");
            try
            {
                System.IO.File.WriteAllText(reportPath, report.ToString());
                EntitySystemLogger.LogInfo("TestRunner", $"Test report saved to: {reportPath}");
            }
            catch (System.Exception ex)
            {
                EntitySystemLogger.LogError("TestRunner", "Failed to save test report", ex);
            }

            Debug.Log(report.ToString());
        }

        // Test classes
        public class TestGameEntity : GameEntity
        {
            public void Initialize(string id, EntityFaction faction)
            {
                _entityId = id;
                _faction = faction;
            }
        }

        private class TestComponent : ScriptableObject { }

        [System.Serializable]
        private class TestSerializableComponent : ScriptableObject
        {
            [SerializeField] public int TestValue;
            [SerializeField] public string TestString;
        }

        private class TestEvent : GameEvent
        {
            public TestEvent() : base(null, EventPriority.Normal) { }
        }

        private struct TestResult
        {
            public string TestName;
            public bool Passed;
            public float StartTime;
            public float Duration;
            public string Message;
        }
    }
}