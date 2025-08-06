using System;

namespace GalacticVentures.EntitySystem.Core
{
    /// <summary>
    /// Base exception class for Entity System errors
    /// </summary>
    public class EntitySystemException : Exception
    {
        public string Context { get; }
        public string EntityId { get; }

        public EntitySystemException(string message, string context = null, string entityId = null) 
            : base(message)
        {
            Context = context ?? "Unknown";
            EntityId = entityId;
        }

        public EntitySystemException(string message, Exception innerException, string context = null, string entityId = null) 
            : base(message, innerException)
        {
            Context = context ?? "Unknown";
            EntityId = entityId;
        }

        public override string ToString()
        {
            var result = $"[{Context}] {Message}";
            if (!string.IsNullOrEmpty(EntityId))
            {
                result += $" (Entity: {EntityId})";
            }
            if (InnerException != null)
            {
                result += $"\nInner Exception: {InnerException}";
            }
            return result;
        }
    }

    /// <summary>
    /// Exception thrown when entity operations fail
    /// </summary>
    public class EntityOperationException : EntitySystemException
    {
        public EntityOperationException(string message, string entityId = null) 
            : base(message, "EntityOperation", entityId)
        {
        }

        public EntityOperationException(string message, Exception innerException, string entityId = null) 
            : base(message, innerException, "EntityOperation", entityId)
        {
        }
    }

    /// <summary>
    /// Exception thrown when component operations fail
    /// </summary>
    public class ComponentException : EntitySystemException
    {
        public Type ComponentType { get; }

        public ComponentException(string message, Type componentType = null, string entityId = null) 
            : base(message, "Component", entityId)
        {
            ComponentType = componentType;
        }

        public ComponentException(string message, Exception innerException, Type componentType = null, string entityId = null) 
            : base(message, innerException, "Component", entityId)
        {
        }

        public override string ToString()
        {
            var result = base.ToString();
            if (ComponentType != null)
            {
                result += $" (Component: {ComponentType.Name})";
            }
            return result;
        }
    }

    /// <summary>
    /// Exception thrown when registry operations fail
    /// </summary>
    public class RegistryException : EntitySystemException
    {
        public RegistryException(string message) 
            : base(message, "Registry")
        {
        }

        public RegistryException(string message, Exception innerException) 
            : base(message, innerException, "Registry")
        {
        }
    }

    /// <summary>
    /// Exception thrown when event system operations fail
    /// </summary>
    public class EventSystemException : EntitySystemException
    {
        public Type EventType { get; }

        public EventSystemException(string message, Type eventType = null) 
            : base(message, "EventSystem")
        {
            EventType = eventType;
        }

        public EventSystemException(string message, Exception innerException, Type eventType = null) 
            : base(message, innerException, "EventSystem")
        {
            EventType = eventType;
        }

        public override string ToString()
        {
            var result = base.ToString();
            if (EventType != null)
            {
                result += $" (Event: {EventType.Name})";
            }
            return result;
        }
    }

    /// <summary>
    /// Exception thrown when serialization operations fail
    /// </summary>
    public class SerializationException : EntitySystemException
    {
        public SerializationException(string message, string entityId = null) 
            : base(message, "Serialization", entityId)
        {
        }

        public SerializationException(string message, Exception innerException, string entityId = null) 
            : base(message, innerException, "Serialization", entityId)
        {
        }
    }
}