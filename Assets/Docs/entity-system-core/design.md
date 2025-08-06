# Entity System Core Design Document

## Overview

The Entity System Core implements a hybrid entity-component system that bridges Unity's traditional MonoBehaviour architecture with a flexible component-based design. The system uses ScriptableObjects for component data storage, enabling easy serialization and Inspector editing, while maintaining runtime performance through efficient lookup mechanisms.

The core architecture consists of four main components:
- **GameEntity**: MonoBehaviour base class for all game objects
- **EntityRegistry**: ScriptableObject singleton for centralized entity tracking
- **ComponentManager**: MonoBehaviour for component lifecycle management
- **EventBus**: ScriptableObject singleton for decoupled event communication

## Architecture

### Core Class Hierarchy

```
GameEntity (MonoBehaviour)
├── Component Dictionary<string, ScriptableObject>
├── Entity ID (string)
├── Faction (EntityFaction enum)
└── Component Management Methods

EntityRegistry (ScriptableObject)
├── Entity Dictionary<string, GameEntity>
├── Faction Groupings Dictionary<EntityFaction, HashSet<string>>
├── Component Index Dictionary<Type, HashSet<string>>
└── Query Methods

EventBus (ScriptableObject)
├── Event Handlers Dictionary<Type, List<Action>>
├── Event Queue List<IGameEvent>
└── Event Processing Methods

ComponentManager (MonoBehaviour)
├── Lifecycle Handlers Dictionary<Type, IComponentLifecycle>
├── Initialization Queue List<(GameEntity, ScriptableObject)>
└── Cleanup Methods
```

### Entity Identification System

Each entity receives a unique identifier using Unity's GUID system combined with a timestamp for collision avoidance:

```csharp
private string GenerateEntityId()
{
    return $"{System.Guid.NewGuid().ToString("N")[..8]}_{System.DateTimeOffset.UtcNow.Ticks}";
}
```

### Component Storage Strategy

Components are stored as ScriptableObject instances within each entity, providing several advantages:
- Unity Inspector integration for easy debugging
- Automatic serialization support
- Asset-based component definitions
- Runtime modification capabilities

## Components and Interfaces

### GameEntity (MonoBehaviour)

**Primary Responsibilities:**
- Entity lifecycle management
- Component storage and retrieval
- Event triggering for component changes
- Unity Inspector integration

**Key Methods:**
- `GetComponent<T>()`: Type-safe component retrieval
- `AddComponent<T>(T component)`: Component attachment with event triggering
- `RemoveComponent<T>()`: Component removal with cleanup
- `HasComponent<T>()`: Component existence checking

**Unity Integration:**
- Implements `ISerializationCallbackReceiver` for custom serialization
- Uses `[SerializeField]` for Inspector visibility
- Provides custom PropertyDrawer for entity information display

### EntityRegistry (ScriptableObject)

**Primary Responsibilities:**
- Centralized entity storage and lookup
- Faction-based entity grouping
- Component-based entity indexing
- Query processing and optimization

**Key Data Structures:**
- `_entities`: Primary entity lookup dictionary
- `_factionGroups`: Faction-based entity groupings for efficient queries
- `_componentIndex`: Component type indexing for fast component-based queries
- `_spatialIndex`: Optional spatial partitioning for location-based queries

**Query Optimization:**
- Maintains multiple indices for different query types
- Uses HashSet collections for O(1) membership testing
- Implements lazy evaluation for complex queries

### EventBus (ScriptableObject)

**Primary Responsibilities:**
- Event subscription management
- Event broadcasting and delivery
- Error handling and logging
- Performance monitoring

**Event Processing Strategy:**
- Immediate processing for critical events
- Queued processing for non-critical events
- Exception isolation to prevent cascade failures
- Event priority system for ordered processing

**Supported Event Types:**
- `ComponentAddedEvent`: Triggered when components are attached
- `ComponentRemovedEvent`: Triggered when components are removed
- `EntityCreatedEvent`: Triggered when entities are registered
- `EntityDestroyedEvent`: Triggered when entities are unregistered
- `FactionChangedEvent`: Triggered when entity factions change

### ComponentManager (MonoBehaviour)

**Primary Responsibilities:**
- Component lifecycle coordination
- Initialization sequencing
- Cleanup orchestration
- Dependency resolution

**Lifecycle Phases:**
1. **Registration**: Component added to entity
2. **Initialization**: Component-specific setup called
3. **Activation**: Component becomes active in game systems
4. **Deactivation**: Component removed from active processing
5. **Cleanup**: Component-specific teardown called
6. **Unregistration**: Component removed from entity

## Data Models

### Entity Data Structure

```csharp
[System.Serializable]
public class EntityData
{
    public string Id;
    public EntityFaction Faction;
    public Vector3 Position;
    public Quaternion Rotation;
    public Dictionary<string, ScriptableObject> Components;
    public long CreationTimestamp;
    public string CreatedBy;
}
```

### Component Interface

```csharp
public interface IEntityComponent
{
    string ComponentId { get; }
    void Initialize(GameEntity owner);
    void Cleanup();
    bool IsValid();
}
```

### Event Base Class

```csharp
public abstract class GameEvent : IGameEvent
{
    public string EventId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public GameEntity Source { get; private set; }
    public EventPriority Priority { get; protected set; }
}
```

## Error Handling

### Entity Management Errors

**Missing Entity Handling:**
- All entity lookups return null for missing entities
- Logging occurs at Debug level to avoid spam
- Graceful degradation for dependent systems

**Component Errors:**
- Component addition failures logged but don't prevent entity creation
- Component removal always succeeds (idempotent operation)
- Invalid component types handled through type checking

### Event System Errors

**Handler Exception Isolation:**
- Individual handler exceptions don't affect other handlers
- Failed handlers logged with full stack trace
- Event processing continues after handler failures

**Event Queue Management:**
- Queue overflow protection with oldest event removal
- Memory pressure monitoring and cleanup
- Performance metrics for event processing times

## Testing Strategy

### Unit Testing Approach

**Entity Registry Tests:**
- Entity registration and lookup functionality
- Faction grouping accuracy
- Component indexing correctness
- Query result validation

**Component System Tests:**
- Component addition and removal
- Lifecycle method invocation
- Event triggering verification
- Serialization round-trip testing

**Event Bus Tests:**
- Subscription and unsubscription
- Event delivery verification
- Error handling and isolation
- Performance benchmarking

### Integration Testing

**Unity Integration Tests:**
- MonoBehaviour lifecycle integration
- Inspector display functionality
- Serialization compatibility
- Scene loading and unloading

**System Integration Tests:**
- Multi-entity scenarios
- Cross-system event propagation
- Performance under load
- Memory usage validation

### Performance Testing

**Benchmarking Targets:**
- Entity lookup: < 1ms for 10,000 entities
- Component queries: < 5ms for complex multi-criteria queries
- Event processing: < 0.1ms per event for simple events
- Memory usage: < 100MB for 10,000 entities with average components

**Load Testing Scenarios:**
- 10,000+ entities with multiple components
- High-frequency component additions/removals
- Burst event processing (1000+ events/frame)
- Long-running sessions (24+ hours)

## Unity-Specific Considerations

### Editor Integration

**Custom Inspectors:**
- Entity information display with component list
- Real-time component monitoring
- Event history visualization
- Performance metrics dashboard

**Scene Management:**
- Automatic entity registration on scene load
- Proper cleanup on scene unload
- Cross-scene entity reference handling
- Prefab instantiation support

### Serialization Strategy

**ScriptableObject Benefits:**
- Automatic Unity serialization
- Asset-based component definitions
- Inspector editing capabilities
- Version control friendly

**Custom Serialization:**
- Entity ID preservation across sessions
- Component reference integrity
- Faction relationship maintenance
- Event subscription persistence

### Performance Optimizations

**Memory Management:**
- Object pooling for frequently created/destroyed entities
- Component instance reuse where possible
- Event object pooling for high-frequency events
- Garbage collection pressure minimization

**Update Loop Integration:**
- Component updates batched by type
- Spatial queries cached between frames
- Event processing spread across multiple frames
- Profiler integration for performance monitoring