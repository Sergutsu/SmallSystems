using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GalacticVentures.EntitySystem.Core;
using GalacticVentures.EntitySystem.Events;

namespace GalacticVentures.EntitySystem.Tests
{
    /// <summary>
    /// Integration tests for the complete Entity System
    /// </summary>
    public class IntegrationTests
    {
        private EntityRegistry _registry;
        private EventBus _eventBus;
        private ComponentManager _componentManager;
        private EntityPersistenceManager _persistenceManager;

        [SetUp]
        public void Setup()
        {
            // Initialize all systems
            _registry = ScriptableObject.CreateInstance<EntityRegistry>();
            _eventBus = ScriptableObject.CreateInstance<EventBus>();
            
            var managerGO = new GameObject("ComponentManager");
            _componentManager = managerGO.AddComponent<ComponentManager>();
            
            var persistenceGO = new GameObject("PersistenceManager");
            _persistenceManager = persistenceGO.AddComponent<EntityPersistenceManager>();

            // Configure logging
            EntitySystemLogger.Configure(EntitySystemLogger.LogLevel.Debug, false, 100);
            EntitySystemLogger.ClearHistory();
        }

        [TearDown]
        public void TearDown()
        {
            _registry?.Clear();
            
            if (_registry != null)
                ScriptableObject.DestroyImmediate(_registry);
            if (_eventBus != null)
                ScriptableObject.DestroyImmediate(_eventBus);
            if (_componentManager != null)
                Object.DestroyImmediate(_componentManager.gameObject);
            if (_persistenceManager != null)
                Object.DestroyImmediate(_persistenceManager.gameObject);
        }

        [Test]
        public void FullEntityLifecycle_CreateModifyDestroy_WorksCorrectly()
        {
            // Arrange
            var eventReceived = false;
            var receivedEvents = new System.Collections.Generic.List<IGameEvent>();
            
            _eventBus.Subscribe<EntityCreatedEvent>((evt) => {
                eventReceived = true;
                receivedEvents.Add(evt);
            });
            _eventBus.Subscribe<ComponentAddedEvent>((evt) => receivedEvents.Add(evt));
            _eventBus.Subscribe<FactionChangedEvent>((evt) => receivedEvents.Add(evt));
            _eventBus.Subscribe<EntityDestroyedEvent>((evt) => receivedEvents.Add(evt));

            // Act - Create entity
            var gameObject = new GameObject("TestEntity");
            var entity = gameObject.AddComponent<TestGameEntity>();
            entity.Initialize("test-entity-1", EntityFaction.Player);
            
            _registry.RegisterEntity(entity);
            _eventBus.ProcessQueuedEvents();

            // Add component
            var component = ScriptableObject.CreateInstance<TestComponent>();
            entity.AddComponent(component);
            _eventBus.ProcessQueuedEvents();

            // Change faction
            entity.SetFaction(EntityFaction.TradingGuild);
            _eventBus.ProcessQueuedEvents();

            // Destroy entity
            Object.DestroyImmediate(gameObject);
            _eventBus.ProcessQueuedEvents();

            // Assert
            Assert.IsTrue(eventReceived);
            Assert.AreEqual(4, receivedEvents.Count);
            Assert.IsInstanceOf<EntityCreatedEvent>(receivedEvents[0]);
            Assert.IsInstanceOf<ComponentAddedEvent>(receivedEvents[1]);
            Assert.IsInstanceOf<FactionChangedEvent>(receivedEvents[2]);
            Assert.IsInstanceOf<EntityDestroyedEvent>(receivedEvents[3]);

            // Cleanup
            ScriptableObject.DestroyImmediate(component);
        }

        [Test]
        public void MultiEntitySystem_ComplexQueries_WorkCorrectly()
        {
            // Arrange - Create diverse entities
            var entities = new TestGameEntity[10];
            var gameObjects = new GameObject[10];
            var components = new TestComponent[5];

            for (int i = 0; i < 10; i++)
            {
                gameObjects[i] = new GameObject($"Entity_{i}");
                entities[i] = gameObjects[i].AddComponent<TestGameEntity>();
                entities[i].Initialize($"entity-{i}", (EntityFaction)(i % 3 + 1)); // Player, TradingGuild, MilitaryAlliance
                
                // Position entities in a line
                gameObjects[i].transform.position = new Vector3(i * 10, 0, 0);
                
                _registry.RegisterEntity(entities[i]);
            }

            // Add components to some entities
            for (int i = 0; i < 5; i++)
            {
                components[i] = ScriptableObject.CreateInstance<TestComponent>();
                entities[i].AddComponent(components[i]);
            }

            // Act & Assert - Test various query combinations
            
            // Query by faction
            var playerEntities = _registry.ExecuteQuery(
                _registry.CreateQuery().WithFaction(EntityFaction.Player)
            );
            Assert.AreEqual(4, playerEntities.Count); // Entities 0, 3, 6, 9

            // Query by component
            var componentEntities = _registry.ExecuteQuery(
                _registry.CreateQuery().WithComponent<TestComponent>()
            );
            Assert.AreEqual(5, componentEntities.Count); // Entities 0-4

            // Query by spatial location
            var nearbyEntities = _registry.ExecuteQuery(
                _registry.CreateQuery().WithinRadius(Vector3.zero, 25f)
            );
            Assert.AreEqual(3, nearbyEntities.Count); // Entities 0, 1, 2

            // Complex query: Player faction with components
            var complexQuery = _registry.ExecuteQuery(
                _registry.CreateQuery()
                    .WithFaction(EntityFaction.Player)
                    .WithComponent<TestComponent>()
            );
            Assert.AreEqual(2, complexQuery.Count); // Entities 0, 3

            // Cleanup
            for (int i = 0; i < 10; i++)
            {
                if (gameObjects[i] != null)
                    Object.DestroyImmediate(gameObjects[i]);
            }
            for (int i = 0; i < 5; i++)
            {
                if (components[i] != null)
                    ScriptableObject.DestroyImmediate(components[i]);
            }
        }

        [Test]
        public void SerializationIntegration_SaveLoadRoundTrip_PreservesData()
        {
            // Arrange - Create entities with components
            var entity1 = CreateTestEntity("entity-1", EntityFaction.Player, new Vector3(10, 20, 30));
            var entity2 = CreateTestEntity("entity-2", EntityFaction.TradingGuild, new Vector3(40, 50, 60));

            var component1 = ScriptableObject.CreateInstance<TestSerializableComponent>();
            component1.TestValue = 42;
            component1.TestString = "Test Data";
            entity1.AddComponent(component1);

            var component2 = ScriptableObject.CreateInstance<TestSerializableComponent>();
            component2.TestValue = 99;
            component2.TestString = "Another Test";
            entity2.AddComponent(component2);

            var testFileName = "integration_test_save.json";

            // Act - Save entities
            var entities = new[] { entity1, entity2 };
            bool saveResult = _persistenceManager.SaveEntities(entities, testFileName);
            Assert.IsTrue(saveResult);

            // Clear registry
            Object.DestroyImmediate(entity1.gameObject);
            Object.DestroyImmediate(entity2.gameObject);
            _registry.Clear();

            // Load entities
            bool loadResult = _persistenceManager.LoadEntities(testFileName, true);
            Assert.IsTrue(loadResult);

            // Assert - Verify restored data
            var restoredEntity1 = _registry.GetEntity("entity-1");
            var restoredEntity2 = _registry.GetEntity("entity-2");

            Assert.IsNotNull(restoredEntity1);
            Assert.IsNotNull(restoredEntity2);

            Assert.AreEqual(EntityFaction.Player, restoredEntity1.Faction);
            Assert.AreEqual(EntityFaction.TradingGuild, restoredEntity2.Faction);

            Assert.AreEqual(new Vector3(10, 20, 30), restoredEntity1.transform.position);
            Assert.AreEqual(new Vector3(40, 50, 60), restoredEntity2.transform.position);

            var restoredComponent1 = restoredEntity1.GetComponent<TestSerializableComponent>();
            var restoredComponent2 = restoredEntity2.GetComponent<TestSerializableComponent>();

            Assert.IsNotNull(restoredComponent1);
            Assert.IsNotNull(restoredComponent2);

            Assert.AreEqual(42, restoredComponent1.TestValue);
            Assert.AreEqual("Test Data", restoredComponent1.TestString);
            Assert.AreEqual(99, restoredComponent2.TestValue);
            Assert.AreEqual("Another Test", restoredComponent2.TestString);

            // Cleanup
            _persistenceManager.DeleteSaveFile(testFileName);
            ScriptableObject.DestroyImmediate(component1);
            ScriptableObject.DestroyImmediate(component2);
        }

        [UnityTest]
        public IEnumerator ComponentLifecycle_WithManager_HandlesCorrectly()
        {
            // Arrange
            var lifecycleHandler = new TestLifecycleHandler();
            _componentManager.RegisterLifecycleHandler<TestLifecycleComponent>(lifecycleHandler);

            var entity = CreateTestEntity("lifecycle-test", EntityFaction.Player, Vector3.zero);
            var component = ScriptableObject.CreateInstance<TestLifecycleComponent>();

            // Act - Add component
            entity.AddComponent(component);
            
            // Wait for component manager to process
            yield return new WaitForSeconds(0.1f);

            // Assert - Component should be initialized
            Assert.IsTrue(component.IsInitialized);
            Assert.IsTrue(lifecycleHandler.OnComponentAddedCalled);
            Assert.AreEqual(entity, lifecycleHandler.LastEntity);

            // Act - Remove component
            entity.RemoveComponent<TestLifecycleComponent>();
            
            // Wait for component manager to process
            yield return new WaitForSeconds(0.1f);

            // Assert - Component should be cleaned up
            Assert.IsTrue(component.IsCleanedUp);
            Assert.IsTrue(lifecycleHandler.OnComponentRemovedCalled);

            // Cleanup
            Object.DestroyImmediate(entity.gameObject);
            ScriptableObject.DestroyImmediate(component);
        }

        [Test]
        public void ErrorHandling_InvalidOperations_LoggedCorrectly()
        {
            // Arrange
            var initialLogCount = EntitySystemLogger.GetStats().TotalEntries;

            // Act - Perform invalid operations
            
            // Try to register null entity
            _registry.RegisterEntity(null);
            
            // Try to get non-existent entity
            var nonExistent = _registry.GetEntity("non-existent-id");
            
            // Try to unregister non-existent entity
            _registry.UnregisterEntity("non-existent-id");

            // Create entity with invalid component operations
            var entity = CreateTestEntity("error-test", EntityFaction.Player, Vector3.zero);
            
            // Try to remove non-existent component
            entity.RemoveComponent<TestComponent>();

            // Assert - Errors should be logged
            var finalLogCount = EntitySystemLogger.GetStats().TotalEntries;
            Assert.Greater(finalLogCount, initialLogCount);

            var errorLogs = EntitySystemLogger.GetLogsByLevel(EntitySystemLogger.LogLevel.Error);
            Assert.Greater(errorLogs.Length, 0);

            // Verify specific error was logged
            var nullEntityError = errorLogs.FirstOrDefault(log => 
                log.Message.Contains("Cannot register null entity"));
            Assert.IsNotNull(nullEntityError);

            // Cleanup
            Object.DestroyImmediate(entity.gameObject);
        }

        [Test]
        public void PerformanceIntegration_LargeScale_MaintainsPerformance()
        {
            // Arrange
            PerformanceProfiler.SetEnabled(true);
            PerformanceProfiler.ClearProfiles();
            
            const int entityCount = 500;
            var entities = new TestGameEntity[entityCount];
            var gameObjects = new GameObject[entityCount];

            // Act - Create large number of entities with operations
            using (PerformanceProfiler.Profile("LargeScaleIntegration"))
            {
                // Create entities
                for (int i = 0; i < entityCount; i++)
                {
                    gameObjects[i] = new GameObject($"Entity_{i}");
                    entities[i] = gameObjects[i].AddComponent<TestGameEntity>();
                    entities[i].Initialize($"entity-{i}", (EntityFaction)(i % 5 + 1));
                    gameObjects[i].transform.position = new Vector3(i % 50, 0, i / 50);
                    _registry.RegisterEntity(entities[i]);

                    // Add components to some entities
                    if (i % 3 == 0)
                    {
                        var component = ScriptableObject.CreateInstance<TestComponent>();
                        entities[i].AddComponent(component);
                    }
                }

                // Perform queries
                for (int i = 0; i < 10; i++)
                {
                    var query1 = _registry.ExecuteQuery(_registry.CreateQuery().WithFaction(EntityFaction.Player));
                    var query2 = _registry.ExecuteQuery(_registry.CreateQuery().WithComponent<TestComponent>());
                    var query3 = _registry.ExecuteQuery(_registry.CreateQuery().WithinRadius(Vector3.zero, 100f));
                }

                // Process events
                _eventBus.ProcessQueuedEvents();
            }

            // Assert - Performance should be acceptable
            var profileData = PerformanceProfiler.GetProfileData("LargeScaleIntegration");
            Assert.IsNotNull(profileData);
            Assert.Less(profileData.AverageMs, 5000, "Large scale operations should complete within 5 seconds");

            var stats = _registry.GetStats();
            Assert.AreEqual(entityCount, stats.TotalEntities);

            // Cleanup
            for (int i = 0; i < entityCount; i++)
            {
                if (gameObjects[i] != null)
                    Object.DestroyImmediate(gameObjects[i]);
            }
        }

        [Test]
        public void SystemIntegration_AllComponents_WorkTogether()
        {
            // This test verifies that all major components work together correctly
            
            // Arrange
            var eventCount = 0;
            _eventBus.Subscribe<EntityCreatedEvent>((evt) => eventCount++);
            _eventBus.Subscribe<ComponentAddedEvent>((evt) => eventCount++);
            _eventBus.Subscribe<FactionChangedEvent>((evt) => eventCount++);

            // Act - Perform comprehensive operations
            
            // 1. Create entities
            var entity1 = CreateTestEntity("system-test-1", EntityFaction.Player, Vector3.zero);
            var entity2 = CreateTestEntity("system-test-2", EntityFaction.TradingGuild, new Vector3(10, 0, 0));

            // 2. Add components
            var component1 = ScriptableObject.CreateInstance<TestComponent>();
            var component2 = ScriptableObject.CreateInstance<TestSerializableComponent>();
            component2.TestValue = 123;
            component2.TestString = "Integration Test";

            entity1.AddComponent(component1);
            entity1.AddComponent(component2);

            // 3. Process events
            _eventBus.ProcessQueuedEvents();

            // 4. Perform queries
            var allEntities = _registry.ExecuteQuery(_registry.CreateQuery());
            var playerEntities = _registry.ExecuteQuery(_registry.CreateQuery().WithFaction(EntityFaction.Player));
            var componentEntities = _registry.ExecuteQuery(_registry.CreateQuery().WithComponent<TestComponent>());

            // 5. Change faction
            entity1.SetFaction(EntityFaction.MilitaryAlliance);
            _eventBus.ProcessQueuedEvents();

            // 6. Save and load
            var saveFile = "system_integration_test.json";
            var saveResult = _persistenceManager.SaveEntities(new[] { entity1, entity2 }, saveFile);

            // Assert - All operations should work correctly
            Assert.AreEqual(2, allEntities.Count);
            Assert.AreEqual(1, playerEntities.Count); // Only entity2 is still Player faction
            Assert.AreEqual(1, componentEntities.Count);
            Assert.IsTrue(saveResult);
            Assert.Greater(eventCount, 0);

            // Verify entity state
            Assert.AreEqual(EntityFaction.MilitaryAlliance, entity1.Faction);
            Assert.IsTrue(entity1.HasComponent<TestComponent>());
            Assert.IsTrue(entity1.HasComponent<TestSerializableComponent>());

            var retrievedComponent = entity1.GetComponent<TestSerializableComponent>();
            Assert.AreEqual(123, retrievedComponent.TestValue);
            Assert.AreEqual("Integration Test", retrievedComponent.TestString);

            // Cleanup
            _persistenceManager.DeleteSaveFile(saveFile);
            Object.DestroyImmediate(entity1.gameObject);
            Object.DestroyImmediate(entity2.gameObject);
            ScriptableObject.DestroyImmediate(component1);
            ScriptableObject.DestroyImmediate(component2);
        }

        // Helper methods
        private TestGameEntity CreateTestEntity(string id, EntityFaction faction, Vector3 position)
        {
            var gameObject = new GameObject($"Entity_{id}");
            gameObject.transform.position = position;
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

        [System.Serializable]
        private class TestSerializableComponent : ScriptableObject
        {
            [SerializeField] public int TestValue;
            [SerializeField] public string TestString;
        }

        private class TestLifecycleComponent : ScriptableObject, IEntityComponent
        {
            public string ComponentId => "test-lifecycle-component";
            public bool IsInitialized { get; private set; }
            public bool IsCleanedUp { get; private set; }

            public void Initialize(GameEntity owner)
            {
                IsInitialized = true;
            }

            public void Cleanup()
            {
                IsCleanedUp = true;
            }

            public bool IsValid()
            {
                return true;
            }
        }

        private class TestLifecycleHandler : IComponentLifecycle
        {
            public bool OnComponentAddedCalled { get; private set; }
            public bool OnComponentRemovedCalled { get; private set; }
            public GameEntity LastEntity { get; private set; }

            public void OnComponentAdded(GameEntity entity, ScriptableObject component)
            {
                OnComponentAddedCalled = true;
                LastEntity = entity;
            }

            public void OnComponentRemoved(GameEntity entity, ScriptableObject component)
            {
                OnComponentRemovedCalled = true;
                LastEntity = entity;
            }

            public void OnEntityDestroyed(GameEntity entity, ScriptableObject component)
            {
                LastEntity = entity;
            }

            public bool CanHandle(System.Type componentType)
            {
                return componentType == typeof(TestLifecycleComponent);
            }
        }
    }
}