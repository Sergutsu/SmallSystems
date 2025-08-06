using System.Linq;
using NUnit.Framework;
using UnityEngine;
using GalacticVentures.EntitySystem.Core;

namespace GalacticVentures.EntitySystem.Tests
{
    /// <summary>
    /// Unit tests for the EntityRegistry system
    /// </summary>
    public class EntityRegistryTests
    {
        private EntityRegistry _registry;
        private GameObject _testGameObject;
        private TestGameEntity _testEntity;

        [SetUp]
        public void Setup()
        {
            _registry = ScriptableObject.CreateInstance<EntityRegistry>();
            
            // Create a test game object with TestGameEntity component
            _testGameObject = new GameObject("TestEntity");
            _testEntity = _testGameObject.AddComponent<TestGameEntity>();
            _testEntity.Initialize("test-entity-1", EntityFaction.Player);
        }

        [TearDown]
        public void TearDown()
        {
            _registry?.Clear();
            if (_registry != null)
            {
                ScriptableObject.DestroyImmediate(_registry);
            }
            
            if (_testGameObject != null)
            {
                Object.DestroyImmediate(_testGameObject);
            }
        }

        [Test]
        public void RegisterEntity_ValidEntity_EntityRegistered()
        {
            // Act
            bool result = _registry.RegisterEntity(_testEntity);

            // Assert
            Assert.IsTrue(result);
            var retrievedEntity = _registry.GetEntity("test-entity-1");
            Assert.AreEqual(_testEntity, retrievedEntity);
        }

        [Test]
        public void RegisterEntity_NullEntity_ReturnsFalse()
        {
            // Act
            bool result = _registry.RegisterEntity(null);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void RegisterEntity_DuplicateId_ReturnsFalse()
        {
            // Arrange
            _registry.RegisterEntity(_testEntity);

            // Act
            bool result = _registry.RegisterEntity(_testEntity);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void UnregisterEntity_ExistingEntity_EntityRemoved()
        {
            // Arrange
            _registry.RegisterEntity(_testEntity);

            // Act
            bool result = _registry.UnregisterEntity("test-entity-1");

            // Assert
            Assert.IsTrue(result);
            var retrievedEntity = _registry.GetEntity("test-entity-1");
            Assert.IsNull(retrievedEntity);
        }

        [Test]
        public void UnregisterEntity_NonExistentEntity_ReturnsFalse()
        {
            // Act
            bool result = _registry.UnregisterEntity("non-existent-id");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void GetEntity_InvalidId_ReturnsNull()
        {
            // Act
            var entity1 = _registry.GetEntity(null);
            var entity2 = _registry.GetEntity("");
            var entity3 = _registry.GetEntity("non-existent");

            // Assert
            Assert.IsNull(entity1);
            Assert.IsNull(entity2);
            Assert.IsNull(entity3);
        }

        [Test]
        public void GetEntitiesByFaction_ExistingFaction_ReturnsCorrectEntities()
        {
            // Arrange
            var entity2GameObject = new GameObject("TestEntity2");
            var entity2 = entity2GameObject.AddComponent<TestGameEntity>();
            entity2.Initialize("test-entity-2", EntityFaction.TradingGuild);

            _registry.RegisterEntity(_testEntity); // Player faction
            _registry.RegisterEntity(entity2); // TradingGuild faction

            // Act
            var playerEntities = _registry.GetEntitiesByFaction(EntityFaction.Player).ToList();
            var guildEntities = _registry.GetEntitiesByFaction(EntityFaction.TradingGuild).ToList();

            // Assert
            Assert.AreEqual(1, playerEntities.Count);
            Assert.AreEqual(_testEntity, playerEntities[0]);
            Assert.AreEqual(1, guildEntities.Count);
            Assert.AreEqual(entity2, guildEntities[0]);

            // Cleanup
            Object.DestroyImmediate(entity2GameObject);
        }

        [Test]
        public void GetEntitiesByFaction_NonExistentFaction_ReturnsEmpty()
        {
            // Act
            var entities = _registry.GetEntitiesByFaction(EntityFaction.AlienSpecies).ToList();

            // Assert
            Assert.AreEqual(0, entities.Count);
        }

        [Test]
        public void OnComponentAdded_ValidComponent_UpdatesIndex()
        {
            // Arrange
            _registry.RegisterEntity(_testEntity);
            var componentType = typeof(TestComponent);

            // Act
            _registry.OnComponentAdded("test-entity-1", componentType);
            var entitiesWithComponent = _registry.GetEntitiesWithComponent(componentType).ToList();

            // Assert
            Assert.AreEqual(1, entitiesWithComponent.Count);
            Assert.AreEqual(_testEntity, entitiesWithComponent[0]);
        }

        [Test]
        public void OnComponentRemoved_ExistingComponent_UpdatesIndex()
        {
            // Arrange
            _registry.RegisterEntity(_testEntity);
            var componentType = typeof(TestComponent);
            _registry.OnComponentAdded("test-entity-1", componentType);

            // Act
            _registry.OnComponentRemoved("test-entity-1", componentType);
            var entitiesWithComponent = _registry.GetEntitiesWithComponent(componentType).ToList();

            // Assert
            Assert.AreEqual(0, entitiesWithComponent.Count);
        }

        [Test]
        public void OnFactionChanged_ValidFactionChange_UpdatesFactionGroups()
        {
            // Arrange
            _registry.RegisterEntity(_testEntity);

            // Act
            _registry.OnFactionChanged("test-entity-1", EntityFaction.Player, EntityFaction.TradingGuild);

            // Assert
            var playerEntities = _registry.GetEntitiesByFaction(EntityFaction.Player).ToList();
            var guildEntities = _registry.GetEntitiesByFaction(EntityFaction.TradingGuild).ToList();
            
            Assert.AreEqual(0, playerEntities.Count);
            Assert.AreEqual(1, guildEntities.Count);
            Assert.AreEqual(_testEntity, guildEntities[0]);
        }

        [Test]
        public void GetEntitiesInRadius_EntitiesInRange_ReturnsCorrectEntities()
        {
            // Arrange
            _testGameObject.transform.position = Vector3.zero;
            _registry.RegisterEntity(_testEntity);

            var entity2GameObject = new GameObject("TestEntity2");
            var entity2 = entity2GameObject.AddComponent<TestGameEntity>();
            entity2.Initialize("test-entity-2", EntityFaction.Player);
            entity2GameObject.transform.position = new Vector3(50, 0, 0); // Within range
            _registry.RegisterEntity(entity2);

            var entity3GameObject = new GameObject("TestEntity3");
            var entity3 = entity3GameObject.AddComponent<TestGameEntity>();
            entity3.Initialize("test-entity-3", EntityFaction.Player);
            entity3GameObject.transform.position = new Vector3(200, 0, 0); // Out of range
            _registry.RegisterEntity(entity3);

            // Act
            var entitiesInRadius = _registry.GetEntitiesInRadius(Vector3.zero, 100f).ToList();

            // Assert
            Assert.AreEqual(2, entitiesInRadius.Count);
            Assert.Contains(_testEntity, entitiesInRadius);
            Assert.Contains(entity2, entitiesInRadius);
            Assert.IsFalse(entitiesInRadius.Contains(entity3));

            // Cleanup
            Object.DestroyImmediate(entity2GameObject);
            Object.DestroyImmediate(entity3GameObject);
        }

        [Test]
        public void GetStats_WithEntities_ReturnsCorrectStats()
        {
            // Arrange
            _registry.RegisterEntity(_testEntity);

            // Act
            var stats = _registry.GetStats();

            // Assert
            Assert.AreEqual(1, stats.TotalEntities);
            Assert.AreEqual(1, stats.FactionCounts[EntityFaction.Player]);
        }

        [Test]
        public void Clear_WithEntities_RemovesAllEntities()
        {
            // Arrange
            _registry.RegisterEntity(_testEntity);
            var statsBefore = _registry.GetStats();
            Assert.Greater(statsBefore.TotalEntities, 0);

            // Act
            _registry.Clear();

            // Assert
            var statsAfter = _registry.GetStats();
            Assert.AreEqual(0, statsAfter.TotalEntities);
            var entity = _registry.GetEntity("test-entity-1");
            Assert.IsNull(entity);
        }

        // Test classes
        private class TestGameEntity : GameEntity
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
    }
}