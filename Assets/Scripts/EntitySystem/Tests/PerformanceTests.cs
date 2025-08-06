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
    /// Performance tests for the Entity System
    /// </summary>
    public class PerformanceTests
    {
        private EntityRegistry _registry;
        private const int LARGE_ENTITY_COUNT = 1000;
        private const int PERFORMANCE_ITERATIONS = 100;

        [SetUp]
        public void Setup()
        {
            _registry = ScriptableObject.CreateInstance<EntityRegistry>();
            PerformanceProfiler.SetEnabled(true);
            PerformanceProfiler.ClearProfiles();
        }

        [TearDown]
        public void TearDown()
        {
            _registry?.Clear();
            if (_registry != null)
            {
                ScriptableObject.DestroyImmediate(_registry);
            }
            PerformanceProfiler.SetEnabled(false);
        }

        [Test]
        public void EntityRegistration_LargeScale_PerformsWithinLimits()
        {
            // Arrange
            var entities = new GameEntity[LARGE_ENTITY_COUNT];
            var gameObjects = new GameObject[LARGE_ENTITY_COUNT];

            for (int i = 0; i < LARGE_ENTITY_COUNT; i++)
            {
                gameObjects[i] = new GameObject($"Entity_{i}");
                entities[i] = gameObjects[i].AddComponent<TestGameEntity>();
                entities[i].Initialize($"entity-{i}", (EntityFaction)(i % 10));
            }

            var stopwatch = Stopwatch.StartNew();

            // Act
            using (PerformanceProfiler.Profile("EntityRegistration_LargeScale"))
            {
                for (int i = 0; i < LARGE_ENTITY_COUNT; i++)
                {
                    _registry.RegisterEntity(entities[i]);
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000, "Entity registration should complete within 1 second");
            Assert.AreEqual(LARGE_ENTITY_COUNT, _registry.GetStats().TotalEntities);

            var profileData = PerformanceProfiler.GetProfileData("EntityRegistration_LargeScale");
            Assert.IsNotNull(profileData);
            Assert.Less(profileData.AverageMs, 1000, "Average registration time should be under 1 second");

            // Cleanup
            for (int i = 0; i < LARGE_ENTITY_COUNT; i++)
            {
                if (gameObjects[i] != null)
                {
                    Object.DestroyImmediate(gameObjects[i]);
                }
            }
        }

        [Test]
        public void EntityQuery_ComponentBased_PerformsWithinLimits()
        {
            // Arrange
            CreateTestEntitiesWithComponents(LARGE_ENTITY_COUNT);

            var stopwatch = Stopwatch.StartNew();
            EntityQueryResult result = null;

            // Act
            using (PerformanceProfiler.Profile("EntityQuery_ComponentBased"))
            {
                for (int i = 0; i < PERFORMANCE_ITERATIONS; i++)
                {
                    var query = _registry.CreateQuery().WithComponent<TestComponent>();
                    result = _registry.ExecuteQuery(query);
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 500, "Component queries should complete within 500ms");
            Assert.IsNotNull(result);
            Assert.Greater(result.Count, 0);

            var profileData = PerformanceProfiler.GetProfileData("EntityQuery_ComponentBased");
            Assert.IsNotNull(profileData);
            Assert.Less(profileData.AverageMs, 5, "Average query time should be under 5ms");
        }

        [Test]
        public void EntityQuery_FactionBased_PerformsWithinLimits()
        {
            // Arrange
            CreateTestEntitiesWithComponents(LARGE_ENTITY_COUNT);

            var stopwatch = Stopwatch.StartNew();
            EntityQueryResult result = null;

            // Act
            using (PerformanceProfiler.Profile("EntityQuery_FactionBased"))
            {
                for (int i = 0; i < PERFORMANCE_ITERATIONS; i++)
                {
                    var query = _registry.CreateQuery().WithFaction(EntityFaction.Player);
                    result = _registry.ExecuteQuery(query);
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 300, "Faction queries should complete within 300ms");
            Assert.IsNotNull(result);

            var profileData = PerformanceProfiler.GetProfileData("EntityQuery_FactionBased");
            Assert.IsNotNull(profileData);
            Assert.Less(profileData.AverageMs, 3, "Average faction query time should be under 3ms");
        }

        [Test]
        public void EntityQuery_Spatial_PerformsWithinLimits()
        {
            // Arrange
            CreateTestEntitiesWithPositions(LARGE_ENTITY_COUNT);

            var stopwatch = Stopwatch.StartNew();
            EntityQueryResult result = null;

            // Act
            using (PerformanceProfiler.Profile("EntityQuery_Spatial"))
            {
                for (int i = 0; i < PERFORMANCE_ITERATIONS; i++)
                {
                    var query = _registry.CreateQuery().WithinRadius(Vector3.zero, 100f);
                    result = _registry.ExecuteQuery(query);
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000, "Spatial queries should complete within 1 second");
            Assert.IsNotNull(result);

            var profileData = PerformanceProfiler.GetProfileData("EntityQuery_Spatial");
            Assert.IsNotNull(profileData);
            Assert.Less(profileData.AverageMs, 10, "Average spatial query time should be under 10ms");
        }

        [Test]
        public void ComponentAddRemove_HighFrequency_PerformsWithinLimits()
        {
            // Arrange
            var entity = CreateSingleTestEntity();
            var components = new TestComponent[100];
            for (int i = 0; i < components.Length; i++)
            {
                components[i] = ScriptableObject.CreateInstance<TestComponent>();
            }

            var stopwatch = Stopwatch.StartNew();

            // Act
            using (PerformanceProfiler.Profile("ComponentAddRemove_HighFrequency"))
            {
                for (int i = 0; i < components.Length; i++)
                {
                    entity.AddComponent(components[i]);
                    entity.RemoveComponent<TestComponent>();
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 100, "Component add/remove should complete within 100ms");

            var profileData = PerformanceProfiler.GetProfileData("ComponentAddRemove_HighFrequency");
            Assert.IsNotNull(profileData);
            Assert.Less(profileData.AverageMs, 100, "Component operations should be fast");

            // Cleanup
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                {
                    ScriptableObject.DestroyImmediate(components[i]);
                }
            }
        }

        [Test]
        public void EventBus_HighVolume_PerformsWithinLimits()
        {
            // Arrange
            var eventBus = EventBus.Instance;
            var eventCount = 1000;
            var receivedCount = 0;

            eventBus.Subscribe<TestEvent>((evt) => receivedCount++);

            var stopwatch = Stopwatch.StartNew();

            // Act
            using (PerformanceProfiler.Profile("EventBus_HighVolume"))
            {
                for (int i = 0; i < eventCount; i++)
                {
                    eventBus.TriggerEvent(new TestEvent());
                }
                eventBus.ProcessQueuedEvents();
            }

            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 500, "Event processing should complete within 500ms");
            Assert.AreEqual(eventCount, receivedCount);

            var profileData = PerformanceProfiler.GetProfileData("EventBus_HighVolume");
            Assert.IsNotNull(profileData);
            Assert.Less(profileData.AverageMs, 500, "Event processing should be efficient");
        }

        [UnityTest]
        public IEnumerator MemoryUsage_LargeScale_StaysWithinLimits()
        {
            // Arrange
            MemoryManager.Initialize();
            var initialMemory = GC.GetTotalMemory(false);

            // Act - Create and destroy many entities
            for (int batch = 0; batch < 10; batch++)
            {
                var entities = CreateTestEntitiesWithComponents(100);
                
                // Let a frame pass
                yield return null;
                
                // Cleanup entities
                foreach (var entity in entities)
                {
                    if (entity != null && entity.gameObject != null)
                    {
                        Object.DestroyImmediate(entity.gameObject);
                    }
                }
                
                // Let cleanup happen
                yield return null;
            }

            // Force GC to get accurate measurement
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = (finalMemory - initialMemory) / 1024f / 1024f; // MB

            // Assert
            Assert.Less(memoryIncrease, 50f, "Memory increase should be less than 50MB after large-scale operations");

            var memoryStats = MemoryManager.GetMemoryStats();
            Assert.Greater(memoryStats.ListPoolAvailable, 0, "Object pools should have available objects");
        }

        [Test]
        public void ObjectPool_Performance_EfficiencyTest()
        {
            // Arrange
            var pool = new ObjectPool<TestPoolableObject>(100, () => new TestPoolableObject(), obj => obj.Reset());
            var stopwatch = Stopwatch.StartNew();

            // Act - Test pool performance vs direct allocation
            using (PerformanceProfiler.Profile("ObjectPool_GetReturn"))
            {
                for (int i = 0; i < 1000; i++)
                {
                    var obj = pool.Get();
                    pool.Return(obj);
                }
            }

            stopwatch.Stop();

            // Compare with direct allocation
            var directStopwatch = Stopwatch.StartNew();
            using (PerformanceProfiler.Profile("DirectAllocation"))
            {
                for (int i = 0; i < 1000; i++)
                {
                    var obj = new TestPoolableObject();
                    obj.Reset(); // Simulate cleanup
                }
            }
            directStopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, directStopwatch.ElapsedMilliseconds * 2, 
                "Object pool should not be significantly slower than direct allocation");

            var poolData = PerformanceProfiler.GetProfileData("ObjectPool_GetReturn");
            var directData = PerformanceProfiler.GetProfileData("DirectAllocation");
            
            Assert.IsNotNull(poolData);
            Assert.IsNotNull(directData);
        }

        // Helper methods
        private GameEntity[] CreateTestEntitiesWithComponents(int count)
        {
            var entities = new GameEntity[count];
            var gameObjects = new GameObject[count];

            for (int i = 0; i < count; i++)
            {
                gameObjects[i] = new GameObject($"Entity_{i}");
                entities[i] = gameObjects[i].AddComponent<TestGameEntity>();
                entities[i].Initialize($"entity-{i}", (EntityFaction)(i % 10));

                // Add components to some entities
                if (i % 3 == 0)
                {
                    var component = ScriptableObject.CreateInstance<TestComponent>();
                    entities[i].AddComponent(component);
                }

                _registry.RegisterEntity(entities[i]);
            }

            return entities;
        }

        private GameEntity[] CreateTestEntitiesWithPositions(int count)
        {
            var entities = new GameEntity[count];
            var gameObjects = new GameObject[count];

            for (int i = 0; i < count; i++)
            {
                gameObjects[i] = new GameObject($"Entity_{i}");
                entities[i] = gameObjects[i].AddComponent<TestGameEntity>();
                entities[i].Initialize($"entity-{i}", EntityFaction.Player);

                // Distribute entities in a grid
                var x = (i % 50) * 10f;
                var z = (i / 50) * 10f;
                gameObjects[i].transform.position = new Vector3(x, 0, z);

                _registry.RegisterEntity(entities[i]);
            }

            return entities;
        }

        private GameEntity CreateSingleTestEntity()
        {
            var gameObject = new GameObject("TestEntity");
            var entity = gameObject.AddComponent<TestGameEntity>();
            entity.Initialize("test-entity", EntityFaction.Player);
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

        private class TestEvent : GameEvent
        {
            public TestEvent() : base(null, EventPriority.Normal) { }
        }

        private class TestPoolableObject
        {
            public int Value { get; set; }

            public void Reset()
            {
                Value = 0;
            }
        }
    }
}