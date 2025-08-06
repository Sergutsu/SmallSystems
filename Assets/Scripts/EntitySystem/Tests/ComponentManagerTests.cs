using System;
using NUnit.Framework;
using UnityEngine;
using GalacticVentures.EntitySystem.Core;

namespace GalacticVentures.EntitySystem.Tests
{
    /// <summary>
    /// Unit tests for the ComponentManager system
    /// </summary>
    public class ComponentManagerTests
    {
        private ComponentManager _componentManager;
        private GameObject _testGameObject;
        private TestGameEntity _testEntity;
        private TestLifecycleComponent _testComponent;
        private TestLifecycleHandler _testHandler;

        [SetUp]
        public void Setup()
        {
            // Create ComponentManager
            var managerGO = new GameObject("ComponentManager");
            _componentManager = managerGO.AddComponent<ComponentManager>();

            // Create test entity
            _testGameObject = new GameObject("TestEntity");
            _testEntity = _testGameObject.AddComponent<TestGameEntity>();

            // Create test component and handler
            _testComponent = ScriptableObject.CreateInstance<TestLifecycleComponent>();
            _testHandler = new TestLifecycleHandler();
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
                UnityEngine.Object.DestroyImmediate(_testGameObject);
            }

            if (_componentManager != null)
            {
                UnityEngine.Object.DestroyImmediate(_componentManager.gameObject);
            }
        }

        [Test]
        public void RegisterLifecycleHandler_ValidHandler_HandlerRegistered()
        {
            // Act
            _componentManager.RegisterLifecycleHandler<TestLifecycleComponent>(_testHandler);

            // Assert
            var stats = _componentManager.GetStats();
            Assert.AreEqual(1, stats.RegisteredLifecycleHandlers);
        }

        [Test]
        public void UnregisterLifecycleHandler_ExistingHandler_HandlerRemoved()
        {
            // Arrange
            _componentManager.RegisterLifecycleHandler<TestLifecycleComponent>(_testHandler);

            // Act
            _componentManager.UnregisterLifecycleHandler<TestLifecycleComponent>();

            // Assert
            var stats = _componentManager.GetStats();
            Assert.AreEqual(0, stats.RegisteredLifecycleHandlers);
        }

        [Test]
        public void InitializeComponentImmediate_ValidComponent_ComponentInitialized()
        {
            // Arrange
            _componentManager.RegisterLifecycleHandler<TestLifecycleComponent>(_testHandler);

            // Act
            _componentManager.InitializeComponentImmediate(_testEntity, _testComponent);

            // Assert
            Assert.IsTrue(_testComponent.IsInitialized);
            Assert.IsTrue(_testHandler.OnComponentAddedCalled);
            Assert.AreEqual(_testEntity, _testHandler.LastEntity);
            Assert.AreEqual(_testComponent, _testHandler.LastComponent);
        }

        [Test]
        public void CleanupComponentImmediate_ValidComponent_ComponentCleanedUp()
        {
            // Arrange
            _componentManager.RegisterLifecycleHandler<TestLifecycleComponent>(_testHandler);
            _componentManager.InitializeComponentImmediate(_testEntity, _testComponent);

            // Act
            _componentManager.CleanupComponentImmediate(_testEntity, _testComponent);

            // Assert
            Assert.IsTrue(_testComponent.IsCleanedUp);
            Assert.IsTrue(_testHandler.OnComponentRemovedCalled);
        }

        [Test]
        public void RegisterComponentDependency_ValidDependency_DependencyRegistered()
        {
            // Act
            _componentManager.RegisterComponentDependency<TestLifecycleComponent, TestDependencyComponent>();

            // Assert
            var stats = _componentManager.GetStats();
            Assert.AreEqual(1, stats.RegisteredDependencies);
        }

        [Test]
        public void InitializeComponentImmediate_WithoutHandler_StillInitializesIEntityComponent()
        {
            // Act
            _componentManager.InitializeComponentImmediate(_testEntity, _testComponent);

            // Assert
            Assert.IsTrue(_testComponent.IsInitialized);
        }

        [Test]
        public void InitializeComponentImmediate_NullComponent_NoException()
        {
            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => _componentManager.InitializeComponentImmediate(_testEntity, null));
        }

        [Test]
        public void InitializeComponentImmediate_NullEntity_NoException()
        {
            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => _componentManager.InitializeComponentImmediate(null, _testComponent));
        }

        [Test]
        public void GetStats_WithActiveComponents_ReturnsCorrectStats()
        {
            // Arrange
            _componentManager.RegisterLifecycleHandler<TestLifecycleComponent>(_testHandler);
            _componentManager.InitializeComponentImmediate(_testEntity, _testComponent);

            // Act
            var stats = _componentManager.GetStats();

            // Assert
            Assert.AreEqual(1, stats.ActiveComponents);
            Assert.AreEqual(1, stats.RegisteredLifecycleHandlers);
        }

        // Test classes
        public class TestGameEntity : GameEntity
        {
            // Empty test implementation
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

        private class TestDependencyComponent : ScriptableObject
        {
            // Empty dependency component
        }

        private class TestLifecycleHandler : IComponentLifecycle
        {
            public bool OnComponentAddedCalled { get; private set; }
            public bool OnComponentRemovedCalled { get; private set; }
            public bool OnEntityDestroyedCalled { get; private set; }
            public GameEntity LastEntity { get; private set; }
            public ScriptableObject LastComponent { get; private set; }

            public void OnComponentAdded(GameEntity entity, ScriptableObject component)
            {
                OnComponentAddedCalled = true;
                LastEntity = entity;
                LastComponent = component;
            }

            public void OnComponentRemoved(GameEntity entity, ScriptableObject component)
            {
                OnComponentRemovedCalled = true;
                LastEntity = entity;
                LastComponent = component;
            }

            public void OnEntityDestroyed(GameEntity entity, ScriptableObject component)
            {
                OnEntityDestroyedCalled = true;
                LastEntity = entity;
                LastComponent = component;
            }

            public bool CanHandle(Type componentType)
            {
                return componentType == typeof(TestLifecycleComponent);
            }
        }
    }
}