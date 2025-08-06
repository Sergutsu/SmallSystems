# Entity System Core

A comprehensive entity-component system for Unity designed for the Galactic Ventures space trading game. This system provides a flexible, performant, and Unity-integrated approach to managing game entities with components, events, and persistence.

## Features

- **Hybrid Entity-Component System**: Combines Unity's MonoBehaviour architecture with flexible ScriptableObject components
- **Centralized Entity Registry**: Fast entity lookup and querying with spatial indexing
- **Event-Driven Architecture**: Decoupled communication through a robust event bus
- **Advanced Query System**: Multi-criteria entity queries with caching and optimization
- **Serialization & Persistence**: Save/load entity states with full component support
- **Performance Optimized**: Object pooling, memory management, and profiling tools
- **Unity Editor Integration**: Custom inspectors, debugging tools, and validation
- **Comprehensive Testing**: Unit tests, integration tests, and stress tests

## Quick Start

### 1. Basic Entity Creation

```csharp
// Create a GameObject and add an entity component
var gameObject = new GameObject("MyShip");
var ship = gameObject.AddComponent<Ship>();
ship.ConfigureShip("Starfire", ShipClass.Frigate, EntityFaction.Player);

// The entity is automatically registered with the EntityRegistry
```

### 2. Adding Components

```csharp
// Create and add a health component
var healthComponent = ScriptableObject.CreateInstance<HealthComponent>();
healthComponent.SetMaxHealth(150f, true);
ship.AddComponent(healthComponent);

// Add an inventory component
var inventoryComponent = ScriptableObject.CreateInstance<InventoryComponent>();
ship.AddComponent(inventoryComponent);

// Components are automatically initialized and managed
```

### 3. Querying Entities

```csharp
var registry = EntityRegistry.Instance;

// Find all player entities
var playerQuery = registry.CreateQuery().WithFaction(EntityFaction.Player);
var playerEntities = registry.ExecuteQuery(playerQuery);

// Find entities with health components near a position
var complexQuery = registry.CreateQuery()
    .WithComponent<HealthComponent>()
    .WithinRadius(Vector3.zero, 100f);
var results = registry.ExecuteQuery(complexQuery);
```

### 4. Event System

```csharp
var eventBus = EventBus.Instance;

// Subscribe to entity events
eventBus.Subscribe<EntityCreatedEvent>((evt) => {
    Debug.Log($"Entity created: {evt.EntityId}");
});

eventBus.Subscribe<ComponentAddedEvent>((evt) => {
    Debug.Log($"Component {evt.ComponentType.Name} added to {evt.Source.EntityId}");
});

// Events are automatically triggered by entity operations
```

### 5. Serialization

```csharp
var persistenceManager = EntityPersistenceManager.Instance;

// Save entities to file
var entitiesToSave = new[] { ship, station, planet };
persistenceManager.SaveEntities(entitiesToSave, "game_save.json");

// Load entities from file
persistenceManager.LoadEntities("game_save.json", clearExisting: true);
```

## Architecture Overview

### Core Components

1. **GameEntity** (MonoBehaviour): Base class for all game entities
2. **EntityRegistry** (ScriptableObject): Centralized entity storage and querying
3. **EventBus** (ScriptableObject): Event system for decoupled communication
4. **ComponentManager** (MonoBehaviour): Component lifecycle management

### Component System

Components are ScriptableObjects that implement `IEntityComponent`:

```csharp
public interface IEntityComponent
{
    string ComponentId { get; }
    void Initialize(GameEntity owner);
    void Cleanup();
    bool IsValid();
}
```

### Event System

Events inherit from `GameEvent` and are processed through the EventBus:

```csharp
public abstract class GameEvent : IGameEvent
{
    public string EventId { get; }
    public DateTime Timestamp { get; }
    public GameEntity Source { get; }
    public EventPriority Priority { get; }
}
```

## Example Entities

The system includes example implementations for common game entities:

### Ship Entity
```csharp
var ship = gameObject.AddComponent<Ship>();
ship.ConfigureShip("Starfire", ShipClass.Frigate, EntityFaction.Player);
```

### Space Station Entity
```csharp
var station = gameObject.AddComponent<SpaceStation>();
station.ConfigureStation("Haven Station", StationType.TradingPost, 6, EntityFaction.TradingGuild);
```

### Planet Entity
```csharp
var planet = gameObject.AddComponent<Planet>();
planet.ConfigurePlanet("New Earth", PlanetType.Terrestrial, 5000000, 0.98f, EntityFaction.Player);
```

## Example Components

### Health Component
Manages entity health, armor, and regeneration:

```csharp
var health = ScriptableObject.CreateInstance<HealthComponent>();
health.SetMaxHealth(150f, true);
entity.AddComponent(health);

// Use the component
health.TakeDamage(25f);
health.Heal(10f);
```

### Inventory Component
Manages item storage with weight and slot limits:

```csharp
var inventory = ScriptableObject.CreateInstance<InventoryComponent>();
entity.AddComponent(inventory);

// Add items
inventory.AddItem("fuel_cells", 10, 5f);
inventory.AddItem("repair_kits", 3, 2f);

// Check contents
int fuelCount = inventory.GetItemQuantity("fuel_cells");
```

### Position Component
Tracks entity position, velocity, and movement:

```csharp
var position = ScriptableObject.CreateInstance<PositionComponent>();
entity.AddComponent(position);

// Move the entity
position.SetVelocity(new Vector3(5, 0, 0));
position.UpdatePosition(Time.deltaTime);

// Get distance to another entity
float distance = position.GetDistanceTo(otherEntity);
```

## Performance Features

### Object Pooling
```csharp
var pool = new ObjectPool<MyComponent>(maxSize: 100);
var component = pool.Get();
// Use component...
pool.Return(component);
```

### Performance Profiling
```csharp
using (PerformanceProfiler.Profile("MyOperation"))
{
    // Code to profile
}

var profileData = PerformanceProfiler.GetProfileData("MyOperation");
Debug.Log($"Average time: {profileData.AverageMs}ms");
```

### Memory Management
```csharp
MemoryManager.Initialize();
var tempList = MemoryManager.GetTempList();
// Use list...
MemoryManager.ReturnTempList(tempList);
```

## Unity Editor Integration

### Entity Registry Window
Access via `Tools > Entity System > Entity Registry` to view and manage all entities in real-time.

### System Debugger
Access via `Tools > Entity System > System Debugger` for performance monitoring and debugging.

### Validation Tools
- `Tools > Entity System > Validate All Entities`
- `Tools > Entity System > Fix Common Issues`
- `Tools > Entity System > Generate Entity Report`

## Testing

The system includes comprehensive tests:

- **Unit Tests**: Individual component testing
- **Integration Tests**: Multi-system interaction testing
- **Performance Tests**: Load and performance validation
- **Stress Tests**: High-volume operation testing

Run tests using Unity's Test Runner or the included `TestRunner` component.

## Configuration

### Logger Configuration
```csharp
EntitySystemLogger.Configure(
    minLevel: EntitySystemLogger.LogLevel.Info,
    enableFileLogging: true,
    maxHistorySize: 1000
);
```

### Performance Settings
```csharp
PerformanceProfiler.SetEnabled(true);
MemoryManager.Initialize();
MemoryManager.PreWarmPools();
```

## Best Practices

1. **Entity Design**: Keep entities lightweight, put logic in components
2. **Component Lifecycle**: Always implement `IEntityComponent` for proper lifecycle management
3. **Event Usage**: Use events for loose coupling between systems
4. **Query Optimization**: Use specific queries rather than broad searches
5. **Memory Management**: Use object pools for frequently created/destroyed objects
6. **Error Handling**: Use the built-in logging and exception system

## Integration with Other Systems

The Entity System is designed to integrate with other game systems:

```csharp
// Example: Integrate with a combat system
public class CombatSystem : MonoBehaviour
{
    private void Start()
    {
        EventBus.Instance.Subscribe<EntityCreatedEvent>(OnEntityCreated);
    }

    private void OnEntityCreated(EntityCreatedEvent evt)
    {
        if (evt.Source.HasComponent<HealthComponent>())
        {
            // Register entity with combat system
            RegisterCombatEntity(evt.Source);
        }
    }
}
```

## Troubleshooting

### Common Issues

1. **Entities not appearing in queries**: Ensure entities are registered with `EntityRegistry`
2. **Components not initializing**: Check that components implement `IEntityComponent`
3. **Events not firing**: Verify event subscriptions and call `ProcessQueuedEvents()`
4. **Performance issues**: Use the profiler and check query optimization

### Debug Information

Use the Entity Registry Window and System Debugger for real-time debugging information.

## API Reference

For detailed API documentation, see the inline code documentation and Unity's generated documentation.

## License

This Entity System is part of the Galactic Ventures project and follows the project's licensing terms.