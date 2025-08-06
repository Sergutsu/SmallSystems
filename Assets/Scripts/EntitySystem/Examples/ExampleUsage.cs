using UnityEngine;
using GalacticVentures.EntitySystem.Core;

namespace GalacticVentures.EntitySystem.Examples
{
    /// <summary>
    /// Example demonstrating how to use the Entity System
    /// </summary>
    public class ExampleUsage : MonoBehaviour
    {
        [Header("Example Configuration")]
        [SerializeField] private bool _runExampleOnStart = true;
        [SerializeField] private bool _enableLogging = true;

        private void Start()
        {
            if (_runExampleOnStart)
            {
                RunEntitySystemExample();
            }
        }

        /// <summary>
        /// Comprehensive example of Entity System usage
        /// </summary>
        public void RunEntitySystemExample()
        {
            // Configure logging
            if (_enableLogging)
            {
                EntitySystemLogger.Configure(EntitySystemLogger.LogLevel.Info, false, 100);
            }

            EntitySystemLogger.LogInfo("ExampleUsage", "Starting Entity System example");

            // Example 1: Creating and configuring entities
            CreateExampleEntities();

            // Example 2: Working with components
            DemonstrateComponentUsage();

            // Example 3: Using the query system
            DemonstrateQuerySystem();

            // Example 4: Event system usage
            DemonstrateEventSystem();

            // Example 5: Serialization example
            DemonstrateSerialization();

            EntitySystemLogger.LogInfo("ExampleUsage", "Entity System example completed");
        }

        /// <summary>
        /// Example 1: Creating different types of entities
        /// </summary>
        private void CreateExampleEntities()
        {
            EntitySystemLogger.LogInfo("ExampleUsage", "=== Creating Example Entities ===");

            // Create a ship
            var shipGO = new GameObject("PlayerShip");
            var ship = shipGO.AddComponent<Ship>();
            ship.ConfigureShip("Starfire", ShipClass.Frigate, EntityFaction.Player);

            // Create a space station
            var stationGO = new GameObject("TradingStation");
            stationGO.transform.position = new Vector3(100, 0, 0);
            var station = stationGO.AddComponent<SpaceStation>();
            station.ConfigureStation("Haven Station", StationType.TradingPost, 6, EntityFaction.TradingGuild);

            // Create a planet
            var planetGO = new GameObject("NewEarth");
            planetGO.transform.position = new Vector3(0, 0, 200);
            var planet = planetGO.AddComponent<Planet>();
            planet.ConfigurePlanet("New Earth", PlanetType.Terrestrial, 5000000, 0.98f, EntityFaction.Player);

            EntitySystemLogger.LogInfo("ExampleUsage", 
                $"Created ship: {ship.GetDisplayName()}, station: {station.StationName}, planet: {planet.PlanetName}");
        }

        /// <summary>
        /// Example 2: Adding and using components
        /// </summary>
        private void DemonstrateComponentUsage()
        {
            EntitySystemLogger.LogInfo("ExampleUsage", "=== Demonstrating Component Usage ===");

            // Find the ship we created
            var registry = EntityRegistry.Instance;
            var shipQuery = registry.CreateQuery().WithFaction(EntityFaction.Player);
            var playerEntities = registry.ExecuteQuery(shipQuery);

            foreach (var entity in playerEntities.Entities)
            {
                if (entity is Ship ship)
                {
                    // Add health component
                    var healthComponent = ScriptableObject.CreateInstance<HealthComponent>();
                    healthComponent.SetMaxHealth(150f, true);
                    entity.AddComponent(healthComponent);

                    // Add inventory component
                    var inventoryComponent = ScriptableObject.CreateInstance<InventoryComponent>();
                    entity.AddComponent(inventoryComponent);

                    // Add position component
                    var positionComponent = ScriptableObject.CreateInstance<PositionComponent>();
                    entity.AddComponent(positionComponent);

                    // Demonstrate component usage
                    var health = entity.GetComponent<HealthComponent>();
                    var inventory = entity.GetComponent<InventoryComponent>();
                    var position = entity.GetComponent<PositionComponent>();

                    // Take some damage
                    health.TakeDamage(25f);
                    EntitySystemLogger.LogInfo("ExampleUsage", 
                        $"{ship.ShipName} health: {health.CurrentHealth}/{health.MaxHealth}");

                    // Add some items to inventory
                    inventory.AddItem("fuel_cells", 10, 5f);
                    inventory.AddItem("repair_kits", 3, 2f);
                    inventory.AddItem("trade_goods", 50, 100f);

                    EntitySystemLogger.LogInfo("ExampleUsage", 
                        $"{ship.ShipName} inventory: {inventory.UsedSlots}/{inventory.MaxSlots} slots, {inventory.CurrentWeight:F1}/{inventory.MaxWeight} weight");

                    // Move the ship
                    position.SetVelocity(new Vector3(5, 0, 0));
                    position.UpdatePosition(1f); // Simulate 1 second

                    EntitySystemLogger.LogInfo("ExampleUsage", 
                        $"{ship.ShipName} moved to position: {position.Position}");

                    break; // Just demonstrate with first ship
                }
            }
        }

        /// <summary>
        /// Example 3: Using the query system to find entities
        /// </summary>
        private void DemonstrateQuerySystem()
        {
            EntitySystemLogger.LogInfo("ExampleUsage", "=== Demonstrating Query System ===");

            var registry = EntityRegistry.Instance;

            // Query 1: Find all player entities
            var playerQuery = registry.CreateQuery().WithFaction(EntityFaction.Player);
            var playerEntities = registry.ExecuteQuery(playerQuery);
            EntitySystemLogger.LogInfo("ExampleUsage", $"Found {playerEntities.Count} player entities");

            // Query 2: Find entities with health components
            var healthQuery = registry.CreateQuery().WithComponent<HealthComponent>();
            var entitiesWithHealth = registry.ExecuteQuery(healthQuery);
            EntitySystemLogger.LogInfo("ExampleUsage", $"Found {entitiesWithHealth.Count} entities with health");

            // Query 3: Find entities near origin
            var spatialQuery = registry.CreateQuery().WithinRadius(Vector3.zero, 150f);
            var nearbyEntities = registry.ExecuteQuery(spatialQuery);
            EntitySystemLogger.LogInfo("ExampleUsage", $"Found {nearbyEntities.Count} entities within 150 units of origin");

            // Query 4: Complex query - Player entities with health components
            var complexQuery = registry.CreateQuery()
                .WithFaction(EntityFaction.Player)
                .WithComponent<HealthComponent>();
            var playerEntitiesWithHealth = registry.ExecuteQuery(complexQuery);
            EntitySystemLogger.LogInfo("ExampleUsage", $"Found {playerEntitiesWithHealth.Count} player entities with health");

            // Demonstrate query result operations
            var orderedByDistance = nearbyEntities.OrderByDistanceFrom(Vector3.zero);
            EntitySystemLogger.LogInfo("ExampleUsage", $"Ordered {orderedByDistance.Count()} entities by distance from origin");

            var groupedByFaction = nearbyEntities.GroupByFaction();
            EntitySystemLogger.LogInfo("ExampleUsage", $"Grouped entities into {groupedByFaction.Count()} faction groups");
        }

        /// <summary>
        /// Example 4: Using the event system
        /// </summary>
        private void DemonstrateEventSystem()
        {
            EntitySystemLogger.LogInfo("ExampleUsage", "=== Demonstrating Event System ===");

            var eventBus = EventBus.Instance;
            var eventsReceived = 0;

            // Subscribe to entity events
            eventBus.Subscribe<EntityCreatedEvent>((evt) => {
                eventsReceived++;
                EntitySystemLogger.LogInfo("ExampleUsage", $"Entity created: {evt.EntityId} ({evt.Faction})");
            });

            eventBus.Subscribe<ComponentAddedEvent>((evt) => {
                eventsReceived++;
                EntitySystemLogger.LogInfo("ExampleUsage", $"Component added: {evt.ComponentType?.Name} to {evt.Source?.EntityId}");
            });

            eventBus.Subscribe<FactionChangedEvent>((evt) => {
                eventsReceived++;
                EntitySystemLogger.LogInfo("ExampleUsage", $"Faction changed: {evt.EntityId} from {evt.OldFaction} to {evt.NewFaction}");
            });

            // Create a new entity to trigger events
            var testGO = new GameObject("EventTestShip");
            var testShip = testGO.AddComponent<Ship>();
            testShip.ConfigureShip("Event Test Ship", ShipClass.Fighter, EntityFaction.IndependentTraders);

            // Add a component to trigger more events
            var testHealth = ScriptableObject.CreateInstance<HealthComponent>();
            testShip.AddComponent(testHealth);

            // Change faction to trigger faction change event
            testShip.SetFaction(EntityFaction.MilitaryAlliance);

            // Process events
            eventBus.ProcessQueuedEvents();

            EntitySystemLogger.LogInfo("ExampleUsage", $"Received {eventsReceived} events from entity operations");

            // Cleanup
            DestroyImmediate(testGO);
            ScriptableObject.DestroyImmediate(testHealth);
        }

        /// <summary>
        /// Example 5: Serialization and persistence
        /// </summary>
        private void DemonstrateSerialization()
        {
            EntitySystemLogger.LogInfo("ExampleUsage", "=== Demonstrating Serialization ===");

            var persistenceManager = FindObjectOfType<EntityPersistenceManager>();
            if (persistenceManager == null)
            {
                var persistenceGO = new GameObject("PersistenceManager");
                persistenceManager = persistenceGO.AddComponent<EntityPersistenceManager>();
            }

            var registry = EntityRegistry.Instance;

            // Create a test entity with components for serialization
            var serializationGO = new GameObject("SerializationTestShip");
            var serializationShip = serializationGO.AddComponent<Ship>();
            serializationShip.ConfigureShip("Serialization Test", ShipClass.Cruiser, EntityFaction.Player);

            // Add components
            var health = ScriptableObject.CreateInstance<HealthComponent>();
            health.SetMaxHealth(200f, true);
            health.TakeDamage(50f); // Damage it so we can verify state
            serializationShip.AddComponent(health);

            var inventory = ScriptableObject.CreateInstance<InventoryComponent>();
            inventory.AddItem("test_item", 5, 10f);
            serializationShip.AddComponent(inventory);

            var position = ScriptableObject.CreateInstance<PositionComponent>();
            position.SetPosition(new Vector3(25, 50, 75), "test_system");
            serializationShip.AddComponent(position);

            // Save the entity
            var saveFileName = "example_save.json";
            var saveResult = persistenceManager.SaveEntities(new[] { serializationShip }, saveFileName);
            
            if (saveResult)
            {
                EntitySystemLogger.LogInfo("ExampleUsage", "Entity saved successfully");

                // Destroy the original
                var originalId = serializationShip.EntityId;
                DestroyImmediate(serializationGO);

                // Load it back
                var loadResult = persistenceManager.LoadEntities(saveFileName, false);
                
                if (loadResult)
                {
                    EntitySystemLogger.LogInfo("ExampleUsage", "Entity loaded successfully");

                    // Verify the loaded entity
                    var loadedEntity = registry.GetEntity(originalId);
                    if (loadedEntity != null)
                    {
                        var loadedHealth = loadedEntity.GetComponent<HealthComponent>();
                        var loadedInventory = loadedEntity.GetComponent<InventoryComponent>();
                        var loadedPosition = loadedEntity.GetComponent<PositionComponent>();

                        EntitySystemLogger.LogInfo("ExampleUsage", 
                            $"Loaded entity verification - Health: {loadedHealth?.CurrentHealth}, " +
                            $"Inventory items: {loadedInventory?.GetItemQuantity("test_item")}, " +
                            $"Position: {loadedPosition?.Position}");
                    }
                }

                // Cleanup save file
                persistenceManager.DeleteSaveFile(saveFileName);
            }

            // Cleanup
            ScriptableObject.DestroyImmediate(health);
            ScriptableObject.DestroyImmediate(inventory);
            ScriptableObject.DestroyImmediate(position);
        }

        /// <summary>
        /// Clean up example entities (call this to reset)
        /// </summary>
        public void CleanupExample()
        {
            EntitySystemLogger.LogInfo("ExampleUsage", "Cleaning up example entities");

            var registry = EntityRegistry.Instance;
            if (registry != null)
            {
                var allEntities = registry.CreateQuery().ExecuteQuery(registry.CreateQuery()).Entities;
                
                foreach (var entity in allEntities)
                {
                    if (entity != null && entity.gameObject != null)
                    {
                        // Clean up components
                        var components = entity.GetAllComponents();
                        foreach (var component in components)
                        {
                            if (component != null)
                            {
                                ScriptableObject.DestroyImmediate(component);
                            }
                        }

                        // Destroy game object
                        DestroyImmediate(entity.gameObject);
                    }
                }

                registry.Clear();
            }

            EntitySystemLogger.LogInfo("ExampleUsage", "Example cleanup completed");
        }

        private void OnDestroy()
        {
            // Clean up when this component is destroyed
            CleanupExample();
        }
    }
}