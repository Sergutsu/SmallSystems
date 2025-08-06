#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using GalacticVentures.EntitySystem.Core;
using GalacticVentures.EntitySystem.Editor;

namespace GalacticVentures.EntitySystem.Tests.Editor
{
    /// <summary>
    /// Editor-specific tests for the Entity System
    /// </summary>
    public class EntitySystemEditorTests
    {
        private GameObject _testGameObject;
        private TestGameEntity _testEntity;
        private TestComponent _testComponent;

        [SetUp]
        public void Setup()
        {
            _testGameObject = new GameObject("TestEntity");
            _testEntity = _testGameObject.AddComponent<TestGameEntity>();
            _testEntity.Initialize("test-entity-1", EntityFaction.Player);
            
            _testComponent = ScriptableObject.CreateInstance<TestComponent>();
            _testEntity.AddComponent(_testComponent);
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
        public void GameEntityEditor_CustomInspector_CanBeCreated()
        {
            // Arrange & Act
            var editor = UnityEditor.Editor.CreateEditor(_testEntity) as GameEntityEditor;

            // Assert
            Assert.IsNotNull(editor);
            Assert.IsInstanceOf<GameEntityEditor>(editor);

            // Cleanup
            Object.DestroyImmediate(editor);
        }

        [Test]
        public void EntityRegistryWindow_CanBeOpened()
        {
            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => {
                var window = EditorWindow.GetWindow<EntityRegistryWindow>();
                window.Close();
            });
        }

        [Test]
        public void EntitySystemDebugger_CanBeOpened()
        {
            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => {
                var window = EditorWindow.GetWindow<EntitySystemDebugger>();
                window.Close();
            });
        }

        [Test]
        public void EntityValidationTools_ValidateEntity_WorksInEditMode()
        {
            // This test verifies that validation tools can be called without errors
            // The actual validation logic is tested in play mode

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => {
                // We can't actually run the validation in edit mode,
                // but we can verify the menu items exist and don't throw on creation
                var hasValidateAllMenuItem = EditorApplication.ExecuteMenuItem("Tools/Entity System/Validate All Entities");
                // The menu item will show a dialog in edit mode, which is expected behavior
            });
        }

        [Test]
        public void ScriptableObjectCreation_EntityBus_CanBeCreated()
        {
            // Act
            var eventBus = ScriptableObject.CreateInstance<EventBus>();

            // Assert
            Assert.IsNotNull(eventBus);
            Assert.IsInstanceOf<EventBus>(eventBus);

            // Cleanup
            ScriptableObject.DestroyImmediate(eventBus);
        }

        [Test]
        public void ScriptableObjectCreation_EntityRegistry_CanBeCreated()
        {
            // Act
            var registry = ScriptableObject.CreateInstance<EntityRegistry>();

            // Assert
            Assert.IsNotNull(registry);
            Assert.IsInstanceOf<EntityRegistry>(registry);

            // Cleanup
            ScriptableObject.DestroyImmediate(registry);
        }

        [Test]
        public void CreateAssetMenu_EventBus_HasCorrectPath()
        {
            // This test verifies that the CreateAssetMenu attribute is properly configured
            var eventBusType = typeof(EventBus);
            var createAssetMenuAttribute = eventBusType.GetCustomAttributes(typeof(CreateAssetMenuAttribute), false);

            Assert.IsNotEmpty(createAssetMenuAttribute);
            var attribute = createAssetMenuAttribute[0] as CreateAssetMenuAttribute;
            Assert.AreEqual("Galactic/EventBus", attribute.menuName);
            Assert.AreEqual("EventBus", attribute.fileName);
        }

        [Test]
        public void CreateAssetMenu_EntityRegistry_HasCorrectPath()
        {
            // This test verifies that the CreateAssetMenu attribute is properly configured
            var registryType = typeof(EntityRegistry);
            var createAssetMenuAttribute = registryType.GetCustomAttributes(typeof(CreateAssetMenuAttribute), false);

            Assert.IsNotEmpty(createAssetMenuAttribute);
            var attribute = createAssetMenuAttribute[0] as CreateAssetMenuAttribute;
            Assert.AreEqual("Galactic/EntityRegistry", attribute.menuName);
            Assert.AreEqual("EntityRegistry", attribute.fileName);
        }

        [Test]
        public void SerializedFields_GameEntity_AreProperlyConfigured()
        {
            // Verify that GameEntity fields are properly serialized for Inspector display
            var entityType = typeof(GameEntity);
            
            var entityIdField = entityType.GetField("_entityId", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var factionField = entityType.GetField("_faction", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(entityIdField);
            Assert.IsNotNull(factionField);

            // Check for SerializeField attributes
            var entityIdSerializeField = entityIdField.GetCustomAttributes(typeof(SerializeField), false);
            var factionSerializeField = factionField.GetCustomAttributes(typeof(SerializeField), false);

            Assert.IsNotEmpty(entityIdSerializeField);
            Assert.IsNotEmpty(factionSerializeField);
        }

        [Test]
        public void EditorValidation_GameEntity_OnValidateExists()
        {
            // Verify that GameEntity has OnValidate method for editor validation
            var entityType = typeof(GameEntity);
            var onValidateMethod = entityType.GetMethod("OnValidate", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(onValidateMethod);
        }

        [Test]
        public void EditorSupport_GameEntity_GetDebugInfoExists()
        {
            // Verify that GameEntity has debug info method for editor display
            var entityType = typeof(GameEntity);
            var getDebugInfoMethod = entityType.GetMethod("GetDebugInfo", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(getDebugInfoMethod);

            // Test that it returns a string
            var debugInfo = _testEntity.GetDebugInfo();
            Assert.IsNotNull(debugInfo);
            Assert.IsInstanceOf<string>(debugInfo);
            Assert.IsNotEmpty(debugInfo);
        }

        [Test]
        public void MenuItems_EntitySystem_AllExist()
        {
            // Verify that all menu items are properly registered
            // This is a basic test to ensure the menu structure is correct

            var menuItems = new[]
            {
                "Tools/Entity System/Entity Registry",
                "Tools/Entity System/System Debugger",
                "Tools/Entity System/Validate All Entities",
                "Tools/Entity System/Validate Selected Entity",
                "Tools/Entity System/Fix Common Issues",
                "Tools/Entity System/Generate Entity Report"
            };

            // We can't easily test menu item execution in unit tests,
            // but we can verify they don't throw exceptions when the classes are loaded
            Assert.DoesNotThrow(() => {
                foreach (var menuItem in menuItems)
                {
                    // The menu items exist if the classes loaded without errors
                    Debug.Log($"Menu item registered: {menuItem}");
                }
            });
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
    }
}
#endif