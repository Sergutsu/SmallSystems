using System.Linq;
using NUnit.Framework;
using UnityEngine;
using GalacticVentures.EntitySystem.Core;

namespace GalacticVentures.EntitySystem.Tests
{
    /// <summary>
    /// Unit tests for the GameEntity system
    /// </summary>
    public class GameEntityTests
    {
        private GameObject _testGameObject;
        private TestGameEntity _testEntity;
        private TestComponent _testComponent;

        [SetUp]
        public void Setup()
        {
            _testGameObject = new GameObject("TestEntity");
            _testEntity = _testGameObject.AddComponent<TestGameEntity>();
            _testComponent = ScriptableObject.CreateInstance<TestComponent>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testComponent != null)
            {
                ScriptableObject.DestroyImmediate(_testComponent);
            }
            
            if (_testGameObject != null)
            {
                Object.DestroyImmediate(_testGameObject);
            }
        }

        [Test]
        public void EntityId_AfterAwake_IsGenerated()
        {
            // Act - Awake is called automatically when component is added
            
            // Assert
            Assert.IsNotNull(_testEntity.EntityId);
            Assert.IsNotEmpty(_testEntity.EntityId);
        }

        [Test]
        public void Faction_DefaultValue_IsNone()
        {
            // Assert
            Assert.AreEqual(EntityFaction.None, _testEntity.Faction);
        }

        [Test]
        public void AddComponent_ValidComponent_ComponentAdded()
        {
            // Act
            _testEntity.AddComponent(_testComponent);

            // Assert
            Assert.IsTrue(_testEntity.HasComponent<TestComponent>());
            var retrievedComponent = _testEntity.GetComponent<TestComponent>();
            Assert.AreEqual(_testComponent, retrievedComponent);
        }

        [Test]
        public void AddComponent_NullComponent_NoComponentAdded()
        {
            // Act
            _testEntity.AddComponent<TestComponent>(null);

            // Assert
            Assert.IsFalse(_testEntity.HasComponent<TestComponent>());
        }

        [Test]
        public void AddComponent_DuplicateType_ReplacesExistingComponent()
        {
            // Arrange
            var component1 = ScriptableObject.CreateInstance<TestComponent>();
            var component2 = ScriptableObject.CreateInstance<TestComponent>();
            
            _testEntity.AddComponent(component1);

            // Act
            _testEntity.AddComponent(component2);

            // Assert
            Assert.IsTrue(_testEntity.HasComponent<TestComponent>());
            var retrievedComponent = _testEntity.GetComponent<TestComponent>();
            Assert.AreEqual(component2, retrievedComponent);
            Assert.AreNotEqual(component1, retrievedComponent);

            // Cleanup
            ScriptableObject.DestroyImmediate(component1);
            ScriptableObject.DestroyImmediate(component2);
        }

        [Test]
        public void RemoveComponent_ExistingComponent_ComponentRemoved()
        {
            // Arrange
            _testEntity.AddComponent(_testComponent);
            Assert.IsTrue(_testEntity.HasComponent<TestComponent>());

            // Act
            bool result = _testEntity.RemoveComponent<TestComponent>();

            // Assert
            Assert.IsTrue(result);
            Assert.IsFalse(_testEntity.HasComponent<TestComponent>());
        }

        [Test]
        public void RemoveComponent_NonExistentComponent_ReturnsFalse()
        {
            // Act
            bool result = _testEntity.RemoveComponent<TestComponent>();

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void HasComponent_ExistingComponent_ReturnsTrue()
        {
            // Arrange
            _testEntity.AddComponent(_testComponent);

            // Act & Assert
            Assert.IsTrue(_testEntity.HasComponent<TestComponent>());
            Assert.IsTrue(_testEntity.HasComponent(typeof(TestComponent)));
        }

        [Test]
        public void HasComponent_NonExistentComponent_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(_testEntity.HasComponent<TestComponent>());
            Assert.IsFalse(_testEntity.HasComponent(typeof(TestComponent)));
        }

        [Test]
        public void GetComponent_ExistingComponent_ReturnsComponent()
        {
            // Arrange
            _testEntity.AddComponent(_testComponent);

            // Act
            var retrievedComponent1 = _testEntity.GetComponent<TestComponent>();
            var retrievedComponent2 = _testEntity.GetComponent(typeof(TestComponent));

            // Assert
            Assert.AreEqual(_testComponent, retrievedComponent1);
            Assert.AreEqual(_testComponent, retrievedComponent2);
        }

        [Test]
        public void GetComponent_NonExistentComponent_ReturnsNull()
        {
            // Act
            var retrievedComponent1 = _testEntity.GetComponent<TestComponent>();
            var retrievedComponent2 = _testEntity.GetComponent(typeof(TestComponent));

            // Assert
            Assert.IsNull(retrievedComponent1);
            Assert.IsNull(retrievedComponent2);
        }

        [Test]
        public void SetFaction_NewFaction_FactionChanged()
        {
            // Arrange
            var originalFaction = _testEntity.Faction;

            // Act
            _testEntity.SetFaction(EntityFaction.Player);

            // Assert
            Assert.AreEqual(EntityFaction.Player, _testEntity.Faction);
            Assert.AreNotEqual(originalFaction, _testEntity.Faction);
        }

        [Test]
        public void SetFaction_SameFaction_NoChange()
        {
            // Arrange
            _testEntity.SetFaction(EntityFaction.Player);
            var eventCountBefore = GetEventCount();

            // Act
            _testEntity.SetFaction(EntityFaction.Player);

            // Assert
            Assert.AreEqual(EntityFaction.Player, _testEntity.Faction);
            // Should not trigger additional events
        }

        [Test]
        public void GetAllComponents_WithComponents_ReturnsAllComponents()
        {
            // Arrange
            var component2 = ScriptableObject.CreateInstance<TestComponent2>();
            _testEntity.AddComponent(_testComponent);
            _testEntity.AddComponent(component2);

            // Act
            var allComponents = _testEntity.GetAllComponents().ToList();

            // Assert
            Assert.AreEqual(2, allComponents.Count);
            Assert.Contains(_testComponent, allComponents);
            Assert.Contains(component2, allComponents);

            // Cleanup
            ScriptableObject.DestroyImmediate(component2);
        }

        [Test]
        public void GetComponentTypes_WithComponents_ReturnsAllTypes()
        {
            // Arrange
            var component2 = ScriptableObject.CreateInstance<TestComponent2>();
            _testEntity.AddComponent(_testComponent);
            _testEntity.AddComponent(component2);

            // Act
            var componentTypes = _testEntity.GetComponentTypes().ToList();

            // Assert
            Assert.AreEqual(2, componentTypes.Count);
            Assert.Contains(typeof(TestComponent), componentTypes);
            Assert.Contains(typeof(TestComponent2), componentTypes);

            // Cleanup
            ScriptableObject.DestroyImmediate(component2);
        }

        [Test]
        public void IsRegistered_AfterAwake_IsTrue()
        {
            // Assert
            Assert.IsTrue(_testEntity.IsRegistered);
        }

        private int GetEventCount()
        {
            // This would require access to EventBus internals or a test helper
            // For now, we'll just return 0 as a placeholder
            return 0;
        }

        // Test classes
        public class TestGameEntity : GameEntity
        {
            // Empty test implementation
        }

        private class TestComponent : ScriptableObject, IEntityComponent
        {
            public string ComponentId => "test-component";

            public void Initialize(GameEntity owner)
            {
                // Test implementation
            }

            public void Cleanup()
            {
                // Test implementation
            }

            public bool IsValid()
            {
                return true;
            }
        }

        private class TestComponent2 : ScriptableObject
        {
            // Empty test component
        }
    }
}