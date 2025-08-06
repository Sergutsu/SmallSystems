using System.Linq;
using NUnit.Framework;
using UnityEngine;
using GalacticVentures.EntitySystem.Core;

namespace GalacticVentures.EntitySystem.Tests
{
    /// <summary>
    /// Unit tests for the entity querying system
    /// </summary>
    public class EntityQueryTests
    {
        private EntityRegistry _registry;
        private GameObject[] _testGameObjects;
        private TestGameEntity[] _testEntities;
        private TestComponent[] _testComponents;

        [SetUp]
        public void Setup()
        {
            _registry = ScriptableObject.CreateInstance<EntityRegistry>();
            
            // Create test entities with different configurations
            _testGameObjects = new GameObject[5];
            _testEntities = new TestGameEntity[5];
            _testComponents = new TestComponent[3];

            for (int i = 0; i < 5; i++)
            {
                _testGameObjects[i] = new GameObject($"TestEntity{i}");
                _testEntities[i] = _testGameObjects[i].AddComponent<TestGameEntity>();
            }

            // Configure entities with different properties
            _testEntities[0].Initialize($"entity-0", EntityFaction.Player);
            _testEntities[1].Initialize($"entity-1", EntityFaction.Player);
            _testEntities[2].Initialize($"entity-2", EntityFaction.TradingGuild);
            _testEntities[3].Initialize($"entity-3", EntityFaction.TradingGuild);
            _testEntities[4].Initialize($"entity-4", EntityFaction.PirateClans);

            // Set positions
            _testGameObjects[0].transform.position = Vector3.zero;
            _testGameObjects[1].transform.position = new Vector3(50, 0, 0);
            _testGameObjects[2].transform.position = new Vector3(100, 0, 0);
            _testGameObjects[3].transform.position = new Vector3(150, 0, 0);
            _testGameObjects[4].transform.position = new Vector3(200, 0, 0);

            // Add components to some entities
            for (int i = 0; i < 3; i++)
            {
                _testComponents[i] = ScriptableObject.CreateInstance<TestComponent>();
                _testEntities[i].AddComponent(_testComponents[i]);
            }

            // Register all entities
            foreach (var entity in _testEntities)
            {
                _registry.RegisterEntity(entity);
            }
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _testComponents.Length; i++)
            {
                if (_testComponents[i] != null)
                {
                    ScriptableObject.DestroyImmediate(_testComponents[i]);
                }
            }

            for (int i = 0; i < _testGameObjects.Length; i++)
            {
                if (_testGameObjects[i] != null)
                {
                    Object.DestroyImmediate(_testGameObjects[i]);
                }
            }

            _registry?.Clear();
            if (_registry != null)
            {
                ScriptableObject.DestroyImmediate(_registry);
            }
        }

        [Test]
        public void CreateQuery_NewQuery_ReturnsValidQuery()
        {
            // Act
            var query = _registry.CreateQuery();

            // Assert
            Assert.IsNotNull(query);
            Assert.IsNotNull(query.QueryId);
        }

        [Test]
        public void WithComponent_ValidComponent_AddsToRequiredComponents()
        {
            // Arrange
            var query = _registry.CreateQuery();

            // Act
            query.WithComponent<TestComponent>();

            // Assert
            Assert.AreEqual(1, query.RequiredComponents.Count);
            Assert.Contains(typeof(TestComponent), query.RequiredComponents.ToList());
        }

        [Test]
        public void WithoutComponent_ValidComponent_AddsToExcludedComponents()
        {
            // Arrange
            var query = _registry.CreateQuery();

            // Act
            query.WithoutComponent<TestComponent>();

            // Assert
            Assert.AreEqual(1, query.ExcludedComponents.Count);
            Assert.Contains(typeof(TestComponent), query.ExcludedComponents.ToList());
        }

        [Test]
        public void WithFaction_ValidFaction_AddsToAllowedFactions()
        {
            // Arrange
            var query = _registry.CreateQuery();

            // Act
            query.WithFaction(EntityFaction.Player);

            // Assert
            Assert.AreEqual(1, query.AllowedFactions.Count);
            Assert.Contains(EntityFaction.Player, query.AllowedFactions.ToList());
        }

        [Test]
        public void WithinRadius_ValidRadius_SetsSpatialConstraints()
        {
            // Arrange
            var query = _registry.CreateQuery();
            var center = Vector3.zero;
            var radius = 100f;

            // Act
            query.WithinRadius(center, radius);

            // Assert
            Assert.AreEqual(center, query.CenterPosition);
            Assert.AreEqual(radius, query.Radius);
        }

        [Test]
        public void ExecuteQuery_ComponentQuery_ReturnsEntitiesWithComponent()
        {
            // Arrange
            var query = _registry.CreateQuery().WithComponent<TestComponent>();

            // Act
            var result = _registry.ExecuteQuery(query);

            // Assert
            Assert.AreEqual(3, result.Count); // First 3 entities have TestComponent
            Assert.IsTrue(result.Entities.All(e => e.HasComponent<TestComponent>()));
        }

        [Test]
        public void ExecuteQuery_FactionQuery_ReturnsEntitiesFromFaction()
        {
            // Arrange
            var query = _registry.CreateQuery().WithFaction(EntityFaction.Player);

            // Act
            var result = _registry.ExecuteQuery(query);

            // Assert
            Assert.AreEqual(2, result.Count); // First 2 entities are Player faction
            Assert.IsTrue(result.Entities.All(e => e.Faction == EntityFaction.Player));
        }

        [Test]
        public void ExecuteQuery_SpatialQuery_ReturnsEntitiesInRadius()
        {
            // Arrange
            var query = _registry.CreateQuery().WithinRadius(Vector3.zero, 75f);

            // Act
            var result = _registry.ExecuteQuery(query);

            // Assert
            Assert.AreEqual(2, result.Count); // Entities at 0 and 50 distance
            Assert.IsTrue(result.Entities.All(e => Vector3.Distance(e.transform.position, Vector3.zero) <= 75f));
        }

        [Test]
        public void ExecuteQuery_CombinedQuery_ReturnsEntitiesMatchingAllCriteria()
        {
            // Arrange
            var query = _registry.CreateQuery()
                .WithComponent<TestComponent>()
                .WithFaction(EntityFaction.Player)
                .WithinRadius(Vector3.zero, 75f);

            // Act
            var result = _registry.ExecuteQuery(query);

            // Assert
            Assert.AreEqual(2, result.Count); // Entities 0 and 1 match all criteria
            Assert.IsTrue(result.Entities.All(e => 
                e.HasComponent<TestComponent>() && 
                e.Faction == EntityFaction.Player &&
                Vector3.Distance(e.transform.position, Vector3.zero) <= 75f));
        }

        [Test]
        public void ExecuteQuery_ExclusionQuery_ReturnsEntitiesWithoutExcludedComponents()
        {
            // Arrange
            var query = _registry.CreateQuery().WithoutComponent<TestComponent>();

            // Act
            var result = _registry.ExecuteQuery(query);

            // Assert
            Assert.AreEqual(2, result.Count); // Last 2 entities don't have TestComponent
            Assert.IsTrue(result.Entities.All(e => !e.HasComponent<TestComponent>()));
        }

        [Test]
        public void ExecuteQuery_MultipleFactions_ReturnsEntitiesFromAnyAllowedFaction()
        {
            // Arrange
            var query = _registry.CreateQuery()
                .WithFactions(EntityFaction.Player, EntityFaction.TradingGuild);

            // Act
            var result = _registry.ExecuteQuery(query);

            // Assert
            Assert.AreEqual(4, result.Count); // First 4 entities are Player or TradingGuild
            Assert.IsTrue(result.Entities.All(e => 
                e.Faction == EntityFaction.Player || e.Faction == EntityFaction.TradingGuild));
        }

        [Test]
        public void ExecuteQuery_WithCaching_SecondCallReturnsCachedResult()
        {
            // Arrange
            var query = _registry.CreateQuery().WithFaction(EntityFaction.Player).WithCaching(true);

            // Act
            var result1 = _registry.ExecuteQuery(query);
            var result2 = _registry.ExecuteQuery(query);

            // Assert
            Assert.AreEqual(result1.Count, result2.Count);
            Assert.IsTrue(result2.WasCached);
        }

        [Test]
        public void ExecuteQuery_NoMatches_ReturnsEmptyResult()
        {
            // Arrange
            var query = _registry.CreateQuery().WithFaction(EntityFaction.AlienSpecies);

            // Act
            var result = _registry.ExecuteQuery(query);

            // Assert
            Assert.AreEqual(0, result.Count);
            Assert.IsNotNull(result.Entities);
        }

        [Test]
        public void QueryResult_OrderByDistanceFrom_ReturnsEntitiesInDistanceOrder()
        {
            // Arrange
            var query = _registry.CreateQuery();
            var result = _registry.ExecuteQuery(query);
            var referencePoint = new Vector3(75, 0, 0);

            // Act
            var orderedEntities = result.OrderByDistanceFrom(referencePoint).ToList();

            // Assert
            Assert.AreEqual(5, orderedEntities.Count);
            // Verify they are in distance order
            for (int i = 1; i < orderedEntities.Count; i++)
            {
                var dist1 = Vector3.Distance(orderedEntities[i-1].transform.position, referencePoint);
                var dist2 = Vector3.Distance(orderedEntities[i].transform.position, referencePoint);
                Assert.LessOrEqual(dist1, dist2);
            }
        }

        [Test]
        public void QueryResult_GroupByFaction_ReturnsEntitiesGroupedByFaction()
        {
            // Arrange
            var query = _registry.CreateQuery();
            var result = _registry.ExecuteQuery(query);

            // Act
            var groupedEntities = result.GroupByFaction().ToList();

            // Assert
            Assert.AreEqual(3, groupedEntities.Count); // Player, TradingGuild, PirateClans
            
            var playerGroup = groupedEntities.First(g => g.Key == EntityFaction.Player);
            Assert.AreEqual(2, playerGroup.Count());
            
            var guildGroup = groupedEntities.First(g => g.Key == EntityFaction.TradingGuild);
            Assert.AreEqual(2, guildGroup.Count());
            
            var pirateGroup = groupedEntities.First(g => g.Key == EntityFaction.PirateClans);
            Assert.AreEqual(1, pirateGroup.Count());
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
    }
}