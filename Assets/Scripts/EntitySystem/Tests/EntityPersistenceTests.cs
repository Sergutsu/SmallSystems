using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using GalacticVentures.EntitySystem.Core;

namespace GalacticVentures.EntitySystem.Tests
{
    /// <summary>
    /// Unit tests for entity persistence system
    /// </summary>
    public class EntityPersistenceTests
    {
        private EntityPersistenceManager _persistenceManager;
        private EntityRegistry _registry;
        private GameObject _testGameObject;
        private TestGameEntity _testEntity;
        private TestSerializableComponent _testComponent;
        private string _testSaveFile = "test_save.json";

        [SetUp]
        public void Setup()
        {
            // Create persistence manager
            var managerGO = new GameObject("EntityPersistenceManager");
            _persistenceManager = managerGO.AddComponent<EntityPersistenceManager>();

            // Create registry
            _registry = ScriptableObject.CreateInstance<EntityRegistry>();

            // Create test entity
            _testGameObject = new GameObject("TestEntity");
            _testEntity = _testGameObject.AddComponent<TestGameEntity>();
            _testEntity.Initialize("test-entity-1", EntityFaction.Player);

            // Create test component
            _testComponent = ScriptableObject.CreateInstance<TestSerializableComponent>();
            _testComponent.TestValue = 42;
            _testComponent.TestString = "Hello World";

            // Add component to entity
            _testEntity.AddComponent(_testComponent);

            // Register entity
            _registry.RegisterEntity(_testEntity);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test save file
            var saveFiles = _persistenceManager.GetSaveFiles();
            foreach (var file in saveFiles.Where(f => f.Contains("test")))
            {
                _persistenceManager.DeleteSaveFile(file);
            }

            if (_testComponent != null)
            {
                ScriptableObject.DestroyImmediate(_testComponent);
            }

            if (_testGameObject != null)
            {
                Object.DestroyImmediate(_testGameObject);
            }

            if (_registry != null)
            {
                _registry.Clear();
                ScriptableObject.DestroyImmediate(_registry);
            }

            if (_persistenceManager != null)
            {
                Object.DestroyImmediate(_persistenceManager.gameObject);
            }
        }

        [Test]
        public void EntityData_FromGameEntity_SerializesCorrectly()
        {
            // Arrange
            _testGameObject.transform.position = new Vector3(10, 20, 30);
            _testGameObject.transform.rotation = Quaternion.Euler(45, 90, 135);

            // Act
            var entityData = new EntityData(_testEntity);

            // Assert
            Assert.AreEqual(_testEntity.EntityId, entityData.Id);
            Assert.AreEqual(_testEntity.Faction, entityData.Faction);
            Assert.AreEqual(_testGameObject.transform.position, entityData.Position);
            Assert.AreEqual(_testGameObject.transform.rotation, entityData.Rotation);
            Assert.AreEqual(1, entityData.Components.Count);
            Assert.AreEqual(typeof(TestSerializableComponent).AssemblyQualifiedName, entityData.Components[0].TypeName);
        }

        [Test]
        public void ComponentData_FromScriptableObject_SerializesCorrectly()
        {
            // Act
            var componentData = new ComponentData(_testComponent);

            // Assert
            Assert.AreEqual(typeof(TestSerializableComponent).AssemblyQualifiedName, componentData.TypeName);
            Assert.IsTrue(componentData.IsScriptableObject);
            Assert.IsNotEmpty(componentData.JsonData);
            Assert.IsTrue(componentData.JsonData.Contains("42")); // TestValue
            Assert.IsTrue(componentData.JsonData.Contains("Hello World")); // TestString
        }

        [Test]
        public void ComponentData_CreateComponent_DeserializesCorrectly()
        {
            // Arrange
            var componentData = new ComponentData(_testComponent);

            // Act
            var restoredComponent = componentData.CreateComponent<TestSerializableComponent>();

            // Assert
            Assert.IsNotNull(restoredComponent);
            Assert.AreEqual(_testComponent.TestValue, restoredComponent.TestValue);
            Assert.AreEqual(_testComponent.TestString, restoredComponent.TestString);

            // Cleanup
            ScriptableObject.DestroyImmediate(restoredComponent);
        }

        [Test]
        public void SaveEntities_ValidEntities_SavesSuccessfully()
        {
            // Arrange
            var entities = new[] { _testEntity };

            // Act
            bool result = _persistenceManager.SaveEntities(entities, _testSaveFile);

            // Assert
            Assert.IsTrue(result);
            var saveFiles = _persistenceManager.GetSaveFiles();
            Assert.Contains(_testSaveFile, saveFiles);
        }

        [Test]
        public void SaveEntities_NullEntities_ReturnsFalse()
        {
            // Act
            bool result = _persistenceManager.SaveEntities(null, _testSaveFile);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void SaveEntities_EmptyFileName_ReturnsFalse()
        {
            // Arrange
            var entities = new[] { _testEntity };

            // Act
            bool result = _persistenceManager.SaveEntities(entities, "");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void LoadEntities_ValidSaveFile_LoadsSuccessfully()
        {
            // Arrange
            var entities = new[] { _testEntity };
            _persistenceManager.SaveEntities(entities, _testSaveFile);

            // Clear existing entities
            Object.DestroyImmediate(_testGameObject);
            _registry.Clear();

            // Act
            bool result = _persistenceManager.LoadEntities(_testSaveFile, true);

            // Assert
            Assert.IsTrue(result);
            
            // Verify entity was restored
            var restoredEntity = _registry.GetEntity("test-entity-1");
            Assert.IsNotNull(restoredEntity);
            Assert.AreEqual(EntityFaction.Player, restoredEntity.Faction);
            Assert.IsTrue(restoredEntity.HasComponent<TestSerializableComponent>());
            
            var restoredComponent = restoredEntity.GetComponent<TestSerializableComponent>();
            Assert.AreEqual(42, restoredComponent.TestValue);
            Assert.AreEqual("Hello World", restoredComponent.TestString);
        }

        [Test]
        public void LoadEntities_NonExistentFile_ReturnsFalse()
        {
            // Act
            bool result = _persistenceManager.LoadEntities("non_existent_file.json");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void LoadEntities_EmptyFileName_ReturnsFalse()
        {
            // Act
            bool result = _persistenceManager.LoadEntities("");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void SaveAllEntities_WithEntities_SavesAllEntities()
        {
            // Act
            bool result = _persistenceManager.SaveAllEntities(_testSaveFile);

            // Assert
            Assert.IsTrue(result);
            var saveFiles = _persistenceManager.GetSaveFiles();
            Assert.Contains(_testSaveFile, saveFiles);
        }

        [Test]
        public void GetSaveFiles_WithSaveFiles_ReturnsFileList()
        {
            // Arrange
            _persistenceManager.SaveAllEntities(_testSaveFile);

            // Act
            var saveFiles = _persistenceManager.GetSaveFiles();

            // Assert
            Assert.IsNotEmpty(saveFiles);
            Assert.Contains(_testSaveFile, saveFiles);
        }

        [Test]
        public void DeleteSaveFile_ExistingFile_DeletesSuccessfully()
        {
            // Arrange
            _persistenceManager.SaveAllEntities(_testSaveFile);
            Assert.Contains(_testSaveFile, _persistenceManager.GetSaveFiles());

            // Act
            bool result = _persistenceManager.DeleteSaveFile(_testSaveFile);

            // Assert
            Assert.IsTrue(result);
            var saveFiles = _persistenceManager.GetSaveFiles();
            Assert.IsFalse(saveFiles.Contains(_testSaveFile));
        }

        [Test]
        public void DeleteSaveFile_NonExistentFile_ReturnsFalse()
        {
            // Act
            bool result = _persistenceManager.DeleteSaveFile("non_existent_file.json");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void RoundTripSerialization_ComplexEntity_MaintainsData()
        {
            // Arrange - Create a more complex entity
            _testGameObject.transform.position = new Vector3(100, 200, 300);
            _testGameObject.transform.rotation = Quaternion.Euler(30, 60, 90);
            _testGameObject.transform.localScale = new Vector3(2, 3, 4);

            var component2 = ScriptableObject.CreateInstance<TestSerializableComponent>();
            component2.TestValue = 999;
            component2.TestString = "Second Component";
            _testEntity.AddComponent(component2);

            // Save
            var entities = new[] { _testEntity };
            _persistenceManager.SaveEntities(entities, _testSaveFile);

            // Clear and load
            Object.DestroyImmediate(_testGameObject);
            _registry.Clear();
            _persistenceManager.LoadEntities(_testSaveFile, true);

            // Act - Get restored entity
            var restoredEntity = _registry.GetEntity("test-entity-1");

            // Assert
            Assert.IsNotNull(restoredEntity);
            Assert.AreEqual(EntityFaction.Player, restoredEntity.Faction);
            Assert.AreEqual(new Vector3(100, 200, 300), restoredEntity.transform.position);
            Assert.AreEqual(Quaternion.Euler(30, 60, 90), restoredEntity.transform.rotation);
            Assert.AreEqual(new Vector3(2, 3, 4), restoredEntity.transform.localScale);
            
            // Check components
            var components = restoredEntity.GetAllComponents().OfType<TestSerializableComponent>().ToList();
            Assert.AreEqual(2, components.Count);
            
            // Verify component data (order might vary)
            var values = components.Select(c => c.TestValue).OrderBy(v => v).ToList();
            var strings = components.Select(c => c.TestString).OrderBy(s => s).ToList();
            
            Assert.AreEqual(new[] { 42, 999 }, values);
            Assert.AreEqual(new[] { "Hello World", "Second Component" }, strings);

            // Cleanup
            ScriptableObject.DestroyImmediate(component2);
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

        [System.Serializable]
        private class TestSerializableComponent : ScriptableObject
        {
            [SerializeField] public int TestValue;
            [SerializeField] public string TestString;
        }
    }
}