using System.Collections;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GalacticVentures.EntitySystem.Core;

namespace GalacticVentures.EntitySystem.Tests
{
    /// <summary>
    /// Stress tests for the Entity System under high load conditions
    /// </summary>
    public class StressTests
    {
        private EntityRegistry _registry;
        private EventBus _eventBus;
        private const int STRESS_ENTITY_COUNT = 5000;
        private const int STRESS_ITERATIONS = 1000;

        [SetUp]
        public void Setup()
        {
            _registry = ScriptableObject.CreateInstance<EntityRegistry>();
            _eventBus = ScriptableObject.CreateInstance<EventBus>();
            
            // Configure for stress testing
            EntitySystemLogger.Configure(EntitySystemLogger.LogLevel.Warning, false, 10000);
            MemoryManager.Initialize();
            PerformanceProfiler.SetEnabled(true);
            PerformanceProfiler.ClearProfiles();
        }

        [TearDown]
        public void TearDown()
        {
            _registry?.Clear();
            
            if (_registry != null)
                ScriptableObject.DestroyImmediate(_registry);
            if (_eventBus != null)
                ScriptableObject.DestroyImmediate(_eventBus);
                
            PerformanceProfiler.SetEnabled(false);
        }

        [Test]
        public void MassiveEntityCreation_ThousandsOfEntities_HandlesGracefully()
        {
            // Arrange
            var entities = new TestGameEntity[STRESS_ENTITY_COUNT];
            var gameObjects = new GameObject[STRESS_ENTITY_COUNT];
            var stopwatch = Stopwatch.StartNew();

            // Act
            using (PerformanceProfiler.Profile("MassiveEntityCreation"))
            {
                for (int i = 0; i < STRESS_ENTITY_COUNT; i++)
                {
                    gameObjects[i] = new GameObject($"StressEntity_{i}");
                    entities[i] = gameObjects[i].AddComponent<TestGameEntity>();
                    entities[i].Initialize($"stress-entity-{i}", (EntityFaction)(i % 10 + 1));
                    
                    // Distribute entities across space
                    var x = (i % 100) * 5f;
                    var z = (i / 100) * 5f;
                    gameObjects[i].transform.position = new Vector3(x, 0, z);
                    
                    _registry.RegisterEntity(entities[i]);
                    
                    // Add components to some entities
                    if (i % 10 == 0)
                    {
                        var component = ScriptableObject.CreateInstance<TestComponent>();
                        entities[i].AddComponent(component);
                    }
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 30000, "Mass entity creation should complete within 30 seconds");
            Assert.AreEqual(STRESS_ENTITY_COUNT, _registry.GetStats().TotalEntities);

            var profileData = PerformanceProfiler.GetProfileData("MassiveEntityCreation");
            Assert.IsNotNull(profileData);
            EntitySystemLogger.LogInfo("StressTest", $"Created {STRESS_ENTITY_COUNT} entities in {stopwatch.ElapsedMilliseconds}ms");

            // Cleanup
            for (int i = 0; i < STRESS_ENTITY_COUNT; i++)
            {
                if (gameObjects[i] != null)
                    Object.DestroyImmediate(gameObjects[i]);
            }
        }

        [Test]
        public void HighFrequencyQueries_ThousandsOfQueries_MaintainsPerformance()
        {
            // Arrange
            CreateStressTestEntities(1000);
            var stopwatch = Stopwatch.StartNew();
            var queryResults = new System.Collections.Generic.List<EntityQueryResult>();

            // Act
            using (PerformanceProfiler.Profile("HighFrequencyQueries"))
            {
                for (int i = 0; i < STRESS_ITERATIONS; i++)
                {
                    // Mix different query types
                    switch (i % 4)
                    {
                        case 0:
                            var factionQuery = _registry.CreateQuery().WithFaction(EntityFaction.Player);
                            queryResults.Add(_registry.ExecuteQuery(factionQuery));
                            break;
                        case 1:
                            var componentQuery = _registry.CreateQuery().WithComponent<TestComponent>();
                            queryResults.Add(_registry.ExecuteQuery(componentQuery));
                            break;
                        case 2:
                            var spatialQuery = _registry.CreateQuery().WithinRadius(Vector3.zero, 50f);
                            queryResults.Add(_registry.ExecuteQuery(spatialQuery));
                            break;
                        case 3:
                            var complexQuery = _registry.CreateQuery()
                                .WithFaction(EntityFaction.TradingGuild)
                                .WithComponent<TestComponent>()
                                .WithinRadius(new Vector3(25, 0, 25), 30f);
                            queryResults.Add(_registry.ExecuteQuery(complexQuery));
                            break;
                    }
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 10000, "High frequency queries should complete within 10 seconds");
            Assert.AreEqual(STRESS_ITERATIONS, queryResults.Count);
            Assert.IsTrue(queryResults.All(r => r != null), "All queries should return valid results");

            var avgQueryTime = (float)stopwatch.ElapsedMilliseconds / STRESS_ITERATIONS;
            Assert.Less(avgQueryTime, 10f, "Average query time should be under 10ms");

            EntitySystemLogger.LogInfo("StressTest", $"Executed {STRESS_ITERATIONS} queries in {stopwatch.ElapsedMilliseconds}ms (avg: {avgQueryTime:F2}ms)");
        }

        [Test]
        public void MassiveComponentOperations_AddRemoveComponents_HandlesEfficiently()
        {
            // Arrange
            var entities = CreateStressTestEntities(500);
            var components = new TestComponent[entities.Length * 2];
            var stopwatch = Stopwatch.StartNew();

            // Pre-create components to avoid allocation during test
            for (int i = 0; i < components.Length; i++)
            {
                components[i] = ScriptableObject.CreateInstance<TestComponent>();
            }

            // Act
            using (PerformanceProfiler.Profile("MassiveComponentOperations"))
            {
                // Add components
                for (int i = 0; i < entities.Length; i++)
                {
                    entities[i].AddComponent(components[i]);
                }

                // Remove and re-add components
                for (int i = 0; i < entities.Length; i++)
                {
                    entities[i].RemoveComponent<TestComponent>();
                    entities[i].AddComponent(components[i + entities.Length]);
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 5000, "Component operations should complete within 5 seconds");
            
            // Verify all entities have components
            foreach (var entity in entities)
            {
                Assert.IsTrue(entity.HasComponent<TestComponent>(), "All entities should have components after operations");
            }

            EntitySystemLogger.LogInfo("StressTest", $"Performed {entities.Length * 3} component operations in {stopwatch.ElapsedMilliseconds}ms");

            // Cleanup
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                    ScriptableObject.DestroyImmediate(components[i]);
            }
        }

        [UnityTest]
        public IEnumerator EventSystemStress_HighVolumeEvents_ProcessesCorrectly()
        {
            // Arrange
            const int eventCount = 10000;
            var receivedEvents = 0;
            var stopwatch = Stopwatch.StartNew();

            _eventBus.Subscribe<TestStressEvent>((evt) => receivedEvents++);

            // Act
            using (PerformanceProfiler.Profile("EventSystemStress"))
            {
                // Generate high volume of events
                for (int i = 0; i < eventCount; i++)
                {
                    _eventBus.TriggerEvent(new TestStressEvent(i));
                    
                    // Process events in batches to avoid frame drops
                    if (i % 100 == 0)
                    {
                        _eventBus.ProcessQueuedEvents();
                        yield return null; // Let Unity process a frame
                    }
                }

                // Process remaining events
                _eventBus.ProcessQueuedEvents();
            }

            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 15000, "Event processing should complete within 15 seconds");
            Assert.AreEqual(eventCount, receivedEvents, "All events should be processed");

            var eventStats = _eventBus.GetStats();
            Assert.AreEqual(0, eventStats.QueuedEventCount, "Event queue should be empty after processing");

            EntitySystemLogger.LogInfo("StressTest", $"Processed {eventCount} events in {stopwatch.ElapsedMilliseconds}ms");
        }

        [Test]
        public void MemoryStress_CreateDestroyLoop_MaintainsMemoryUsage()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(false);
            const int cycles = 50;
            const int entitiesPerCycle = 100;

            // Act
            using (PerformanceProfiler.Profile("MemoryStress"))
            {
                for (int cycle = 0; cycle < cycles; cycle++)
                {
                    // Create entities
                    var entities = new TestGameEntity[entitiesPerCycle];
                    var gameObjects = new GameObject[entitiesPerCycle];
                    var components = new TestComponent[entitiesPerCycle];

                    for (int i = 0; i < entitiesPerCycle; i++)
                    {
                        gameObjects[i] = new GameObject($"MemStressEntity_{cycle}_{i}");
                        entities[i] = gameObjects[i].AddComponent<TestGameEntity>();
                        entities[i].Initialize($"mem-stress-{cycle}-{i}", EntityFaction.Player);
                        
                        components[i] = ScriptableObject.CreateInstance<TestComponent>();
                        entities[i].AddComponent(components[i]);
                        
                        _registry.RegisterEntity(entities[i]);
                    }

                    // Destroy entities
                    for (int i = 0; i < entitiesPerCycle; i++)
                    {
                        if (gameObjects[i] != null)
                            Object.DestroyImmediate(gameObjects[i]);
                        if (components[i] != null)
                            ScriptableObject.DestroyImmediate(components[i]);
                    }

                    // Force cleanup every 10 cycles
                    if (cycle % 10 == 0)
                    {
                        _registry.Clear();
                        MemoryManager.CleanupUnusedObjects();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }
            }

            // Final cleanup
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = (finalMemory - initialMemory) / 1024f / 1024f; // MB

            // Assert
            Assert.Less(memoryIncrease, 100f, "Memory increase should be less than 100MB after stress test");
            
            var memoryStats = MemoryManager.GetMemoryStats();
            EntitySystemLogger.LogInfo("StressTest", $"Memory stress test completed. Memory increase: {memoryIncrease:F2}MB");
        }

        [Test]
        public void ConcurrentOperations_SimulatedMultithreading_HandlesCorrectly()
        {
            // Note: This simulates concurrent operations in a single thread
            // Real multithreading would require thread-safe implementations
            
            // Arrange
            var entities = CreateStressTestEntities(200);
            var stopwatch = Stopwatch.StartNew();
            var operationCount = 0;

            // Act
            using (PerformanceProfiler.Profile("ConcurrentOperations"))
            {
                // Simulate concurrent operations by interleaving different operations
                for (int i = 0; i < 1000; i++)
                {
                    var entityIndex = i % entities.Length;
                    var entity = entities[entityIndex];

                    switch (i % 6)
                    {
                        case 0: // Query operation
                            var query = _registry.CreateQuery().WithFaction(entity.Faction);
                            _registry.ExecuteQuery(query);
                            operationCount++;
                            break;
                            
                        case 1: // Component add
                            if (!entity.HasComponent<TestComponent>())
                            {
                                var component = ScriptableObject.CreateInstance<TestComponent>();
                                entity.AddComponent(component);
                                operationCount++;
                            }
                            break;
                            
                        case 2: // Component remove
                            if (entity.HasComponent<TestComponent>())
                            {
                                entity.RemoveComponent<TestComponent>();
                                operationCount++;
                            }
                            break;
                            
                        case 3: // Faction change
                            var newFaction = (EntityFaction)((int)entity.Faction % 10 + 1);
                            entity.SetFaction(newFaction);
                            operationCount++;
                            break;
                            
                        case 4: // Spatial query
                            var spatialQuery = _registry.CreateQuery().WithinRadius(entity.transform.position, 10f);
                            _registry.ExecuteQuery(spatialQuery);
                            operationCount++;
                            break;
                            
                        case 5: // Registry lookup
                            _registry.GetEntity(entity.EntityId);
                            operationCount++;
                            break;
                    }
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 10000, "Concurrent operations should complete within 10 seconds");
            Assert.Greater(operationCount, 800, "Most operations should have been executed");

            EntitySystemLogger.LogInfo("StressTest", $"Executed {operationCount} concurrent operations in {stopwatch.ElapsedMilliseconds}ms");
        }

        [Test]
        public void SystemLimits_ExtremeValues_HandlesGracefully()
        {
            // Test system behavior at extreme values
            
            // Test with very long entity IDs
            var longIdEntity = CreateTestEntity(new string('A', 1000), EntityFaction.Player);
            Assert.IsNotNull(_registry.GetEntity(longIdEntity.EntityId));

            // Test with many components on single entity
            var multiComponentEntity = CreateTestEntity("multi-component", EntityFaction.Player);
            var components = new TestComponent[100];
            
            for (int i = 0; i < 100; i++)
            {
                components[i] = ScriptableObject.CreateInstance<TestComponent>();
                multiComponentEntity.AddComponent(components[i]);
                // Only the last one should remain due to type replacement
            }
            
            Assert.IsTrue(multiComponentEntity.HasComponent<TestComponent>());
            Assert.AreEqual(1, multiComponentEntity.GetAllComponents().Count());

            // Test with extreme positions
            var extremeEntity = CreateTestEntity("extreme-position", EntityFaction.Player);
            extremeEntity.transform.position = new Vector3(float.MaxValue / 2, float.MaxValue / 2, float.MaxValue / 2);
            
            var extremeQuery = _registry.CreateQuery().WithinRadius(extremeEntity.transform.position, 1000f);
            var extremeResult = _registry.ExecuteQuery(extremeQuery);
            Assert.IsNotNull(extremeResult);

            // Cleanup
            Object.DestroyImmediate(longIdEntity.gameObject);
            Object.DestroyImmediate(multiComponentEntity.gameObject);
            Object.DestroyImmediate(extremeEntity.gameObject);
            
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                    ScriptableObject.DestroyImmediate(components[i]);
            }
        }

        // Helper methods
        private TestGameEntity[] CreateStressTestEntities(int count)
        {
            var entities = new TestGameEntity[count];
            var gameObjects = new GameObject[count];

            for (int i = 0; i < count; i++)
            {
                gameObjects[i] = new GameObject($"StressEntity_{i}");
                entities[i] = gameObjects[i].AddComponent<TestGameEntity>();
                entities[i].Initialize($"stress-entity-{i}", (EntityFaction)(i % 10 + 1));
                
                // Distribute entities in space
                var x = (i % 50) * 2f;
                var z = (i / 50) * 2f;
                gameObjects[i].transform.position = new Vector3(x, 0, z);
                
                _registry.RegisterEntity(entities[i]);
            }

            return entities;
        }

        private TestGameEntity CreateTestEntity(string id, EntityFaction faction)
        {
            var gameObject = new GameObject($"Entity_{id}");
            var entity = gameObject.AddComponent<TestGameEntity>();
            entity.Initialize(id, faction);
            _registry.RegisterEntity(entity);
            return entity;
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

        private class TestComponent : ScriptableObject
        {
            // Empty test component
        }

        private class TestStressEvent : GameEvent
        {
            public int EventNumber { get; }

            public TestStressEvent(int eventNumber) : base(null, EventPriority.Normal)
            {
                EventNumber = eventNumber;
            }
        }
    }
}