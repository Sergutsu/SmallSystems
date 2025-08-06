using System;
using NUnit.Framework;
using GalacticVentures.EntitySystem.Core;

namespace GalacticVentures.EntitySystem.Tests
{
    /// <summary>
    /// Unit tests for Entity System exception classes
    /// </summary>
    public class EntitySystemExceptionTests
    {
        [Test]
        public void EntitySystemException_BasicConstructor_SetsProperties()
        {
            // Arrange
            var message = "Test exception message";
            var context = "TestContext";
            var entityId = "test-entity-1";

            // Act
            var exception = new EntitySystemException(message, context, entityId);

            // Assert
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual(context, exception.Context);
            Assert.AreEqual(entityId, exception.EntityId);
        }

        [Test]
        public void EntitySystemException_WithInnerException_SetsProperties()
        {
            // Arrange
            var message = "Test exception message";
            var innerException = new ArgumentException("Inner exception");
            var context = "TestContext";
            var entityId = "test-entity-1";

            // Act
            var exception = new EntitySystemException(message, innerException, context, entityId);

            // Assert
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual(innerException, exception.InnerException);
            Assert.AreEqual(context, exception.Context);
            Assert.AreEqual(entityId, exception.EntityId);
        }

        [Test]
        public void EntitySystemException_NullContext_UsesDefault()
        {
            // Act
            var exception = new EntitySystemException("Test message", null, "entity-1");

            // Assert
            Assert.AreEqual("Unknown", exception.Context);
        }

        [Test]
        public void EntitySystemException_ToString_FormatsCorrectly()
        {
            // Arrange
            var exception = new EntitySystemException("Test message", "TestContext", "entity-1");

            // Act
            var result = exception.ToString();

            // Assert
            Assert.IsTrue(result.Contains("[TestContext]"));
            Assert.IsTrue(result.Contains("Test message"));
            Assert.IsTrue(result.Contains("(Entity: entity-1)"));
        }

        [Test]
        public void EntityOperationException_BasicConstructor_SetsContext()
        {
            // Arrange
            var message = "Entity operation failed";
            var entityId = "test-entity-1";

            // Act
            var exception = new EntityOperationException(message, entityId);

            // Assert
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual("EntityOperation", exception.Context);
            Assert.AreEqual(entityId, exception.EntityId);
        }

        [Test]
        public void EntityOperationException_WithInnerException_SetsProperties()
        {
            // Arrange
            var message = "Entity operation failed";
            var innerException = new InvalidOperationException("Inner exception");
            var entityId = "test-entity-1";

            // Act
            var exception = new EntityOperationException(message, innerException, entityId);

            // Assert
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual(innerException, exception.InnerException);
            Assert.AreEqual("EntityOperation", exception.Context);
            Assert.AreEqual(entityId, exception.EntityId);
        }

        [Test]
        public void ComponentException_BasicConstructor_SetsProperties()
        {
            // Arrange
            var message = "Component operation failed";
            var componentType = typeof(TestComponent);
            var entityId = "test-entity-1";

            // Act
            var exception = new ComponentException(message, componentType, entityId);

            // Assert
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual("Component", exception.Context);
            Assert.AreEqual(componentType, exception.ComponentType);
            Assert.AreEqual(entityId, exception.EntityId);
        }

        [Test]
        public void ComponentException_ToString_IncludesComponentType()
        {
            // Arrange
            var exception = new ComponentException("Component failed", typeof(TestComponent), "entity-1");

            // Act
            var result = exception.ToString();

            // Assert
            Assert.IsTrue(result.Contains("[Component]"));
            Assert.IsTrue(result.Contains("Component failed"));
            Assert.IsTrue(result.Contains("(Entity: entity-1)"));
            Assert.IsTrue(result.Contains("(Component: TestComponent)"));
        }

        [Test]
        public void RegistryException_BasicConstructor_SetsContext()
        {
            // Arrange
            var message = "Registry operation failed";

            // Act
            var exception = new RegistryException(message);

            // Assert
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual("Registry", exception.Context);
        }

        [Test]
        public void RegistryException_WithInnerException_SetsProperties()
        {
            // Arrange
            var message = "Registry operation failed";
            var innerException = new InvalidOperationException("Inner exception");

            // Act
            var exception = new RegistryException(message, innerException);

            // Assert
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual(innerException, exception.InnerException);
            Assert.AreEqual("Registry", exception.Context);
        }

        [Test]
        public void EventSystemException_BasicConstructor_SetsProperties()
        {
            // Arrange
            var message = "Event system operation failed";
            var eventType = typeof(TestEvent);

            // Act
            var exception = new EventSystemException(message, eventType);

            // Assert
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual("EventSystem", exception.Context);
            Assert.AreEqual(eventType, exception.EventType);
        }

        [Test]
        public void EventSystemException_ToString_IncludesEventType()
        {
            // Arrange
            var exception = new EventSystemException("Event failed", typeof(TestEvent));

            // Act
            var result = exception.ToString();

            // Assert
            Assert.IsTrue(result.Contains("[EventSystem]"));
            Assert.IsTrue(result.Contains("Event failed"));
            Assert.IsTrue(result.Contains("(Event: TestEvent)"));
        }

        [Test]
        public void SerializationException_BasicConstructor_SetsContext()
        {
            // Arrange
            var message = "Serialization failed";
            var entityId = "test-entity-1";

            // Act
            var exception = new SerializationException(message, entityId);

            // Assert
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual("Serialization", exception.Context);
            Assert.AreEqual(entityId, exception.EntityId);
        }

        [Test]
        public void SerializationException_WithInnerException_SetsProperties()
        {
            // Arrange
            var message = "Serialization failed";
            var innerException = new ArgumentException("Inner exception");
            var entityId = "test-entity-1";

            // Act
            var exception = new SerializationException(message, innerException, entityId);

            // Assert
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual(innerException, exception.InnerException);
            Assert.AreEqual("Serialization", exception.Context);
            Assert.AreEqual(entityId, exception.EntityId);
        }

        [Test]
        public void EntitySystemException_WithInnerException_ToStringIncludesInner()
        {
            // Arrange
            var innerException = new ArgumentException("Inner exception message");
            var exception = new EntitySystemException("Outer message", innerException, "TestContext", "entity-1");

            // Act
            var result = exception.ToString();

            // Assert
            Assert.IsTrue(result.Contains("Outer message"));
            Assert.IsTrue(result.Contains("Inner Exception:"));
            Assert.IsTrue(result.Contains("Inner exception message"));
        }

        // Test classes
        private class TestComponent
        {
            // Empty test component
        }

        private class TestEvent
        {
            // Empty test event
        }
    }
}