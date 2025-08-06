using UnityEngine;
using GalacticVentures.EntitySystem.Core;

namespace GalacticVentures.EntitySystem.Examples
{
    /// <summary>
    /// Example health component for entities
    /// </summary>
    [CreateAssetMenu(fileName = "HealthComponent", menuName = "Galactic/Components/Health")]
    public class HealthComponent : ScriptableObject, IEntityComponent
    {
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _currentHealth = 100f;
        [SerializeField] private float _armor = 0f;
        [SerializeField] private bool _canRegenerate = false;
        [SerializeField] private float _regenerationRate = 1f;

        private GameEntity _owner;

        public string ComponentId => $"health_{GetInstanceID()}";
        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public float Armor => _armor;
        public bool IsAlive => _currentHealth > 0f;
        public float HealthPercentage => _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;

        public void Initialize(GameEntity owner)
        {
            _owner = owner;
            _currentHealth = _maxHealth;
            EntitySystemLogger.LogDebug("HealthComponent", $"Health component initialized for {owner.EntityId} with {_maxHealth} HP");
        }

        public void Cleanup()
        {
            _owner = null;
        }

        public bool IsValid()
        {
            return _maxHealth > 0 && _currentHealth >= 0 && _currentHealth <= _maxHealth;
        }

        /// <summary>
        /// Take damage, accounting for armor
        /// </summary>
        public float TakeDamage(float damage)
        {
            if (damage <= 0) return 0f;

            var effectiveDamage = Mathf.Max(0, damage - _armor);
            var oldHealth = _currentHealth;
            _currentHealth = Mathf.Max(0, _currentHealth - effectiveDamage);

            var actualDamage = oldHealth - _currentHealth;
            
            EntitySystemLogger.LogDebug("HealthComponent", 
                $"{_owner?.EntityId} took {actualDamage:F1} damage ({damage:F1} - {_armor:F1} armor)");

            if (_currentHealth <= 0 && oldHealth > 0)
            {
                OnDeath();
            }

            return actualDamage;
        }

        /// <summary>
        /// Heal the entity
        /// </summary>
        public float Heal(float amount)
        {
            if (amount <= 0 || _currentHealth >= _maxHealth) return 0f;

            var oldHealth = _currentHealth;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            var actualHealing = _currentHealth - oldHealth;

            EntitySystemLogger.LogDebug("HealthComponent", 
                $"{_owner?.EntityId} healed for {actualHealing:F1} HP");

            return actualHealing;
        }

        /// <summary>
        /// Regenerate health over time
        /// </summary>
        public void RegenerateHealth(float deltaTime)
        {
            if (_canRegenerate && _currentHealth < _maxHealth && _currentHealth > 0)
            {
                Heal(_regenerationRate * deltaTime);
            }
        }

        /// <summary>
        /// Set maximum health and optionally heal to full
        /// </summary>
        public void SetMaxHealth(float maxHealth, bool healToFull = false)
        {
            _maxHealth = Mathf.Max(1f, maxHealth);
            if (healToFull)
            {
                _currentHealth = _maxHealth;
            }
            else
            {
                _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
            }
        }

        private void OnDeath()
        {
            EntitySystemLogger.LogInfo("HealthComponent", $"{_owner?.EntityId} has died");
            // Could trigger death event here
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _maxHealth = Mathf.Max(1f, _maxHealth);
            _currentHealth = Mathf.Clamp(_currentHealth, 0f, _maxHealth);
            _armor = Mathf.Max(0f, _armor);
            _regenerationRate = Mathf.Max(0f, _regenerationRate);
        }
#endif
    }

    /// <summary>
    /// Example inventory component for storing items
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryComponent", menuName = "Galactic/Components/Inventory")]
    public class InventoryComponent : ScriptableObject, IEntityComponent
    {
        [SerializeField] private int _maxSlots = 20;
        [SerializeField] private float _maxWeight = 1000f;
        [SerializeField] private InventoryItem[] _items = new InventoryItem[0];

        private GameEntity _owner;

        public string ComponentId => $"inventory_{GetInstanceID()}";
        public int MaxSlots => _maxSlots;
        public float MaxWeight => _maxWeight;
        public int UsedSlots => _items?.Length ?? 0;
        public float CurrentWeight => CalculateCurrentWeight();
        public bool IsFull => UsedSlots >= _maxSlots;

        public void Initialize(GameEntity owner)
        {
            _owner = owner;
            if (_items == null)
            {
                _items = new InventoryItem[0];
            }
            EntitySystemLogger.LogDebug("InventoryComponent", $"Inventory initialized for {owner.EntityId} with {_maxSlots} slots");
        }

        public void Cleanup()
        {
            _owner = null;
        }

        public bool IsValid()
        {
            return _maxSlots > 0 && _maxWeight > 0 && _items != null;
        }

        /// <summary>
        /// Add an item to the inventory
        /// </summary>
        public bool AddItem(string itemId, int quantity = 1, float weight = 0f)
        {
            if (IsFull || CurrentWeight + weight > _maxWeight)
            {
                return false;
            }

            // Check if item already exists and can be stacked
            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i].ItemId == itemId)
                {
                    _items[i].Quantity += quantity;
                    EntitySystemLogger.LogDebug("InventoryComponent", 
                        $"Added {quantity} {itemId} to existing stack in {_owner?.EntityId}");
                    return true;
                }
            }

            // Add new item
            var newItems = new InventoryItem[_items.Length + 1];
            System.Array.Copy(_items, newItems, _items.Length);
            newItems[_items.Length] = new InventoryItem
            {
                ItemId = itemId,
                Quantity = quantity,
                Weight = weight
            };
            _items = newItems;

            EntitySystemLogger.LogDebug("InventoryComponent", 
                $"Added new item {itemId} (x{quantity}) to {_owner?.EntityId}");
            return true;
        }

        /// <summary>
        /// Remove an item from the inventory
        /// </summary>
        public bool RemoveItem(string itemId, int quantity = 1)
        {
            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i].ItemId == itemId)
                {
                    if (_items[i].Quantity >= quantity)
                    {
                        _items[i].Quantity -= quantity;
                        
                        // Remove item if quantity reaches zero
                        if (_items[i].Quantity <= 0)
                        {
                            var newItems = new InventoryItem[_items.Length - 1];
                            System.Array.Copy(_items, 0, newItems, 0, i);
                            System.Array.Copy(_items, i + 1, newItems, i, _items.Length - i - 1);
                            _items = newItems;
                        }

                        EntitySystemLogger.LogDebug("InventoryComponent", 
                            $"Removed {quantity} {itemId} from {_owner?.EntityId}");
                        return true;
                    }
                    break;
                }
            }
            return false;
        }

        /// <summary>
        /// Get quantity of a specific item
        /// </summary>
        public int GetItemQuantity(string itemId)
        {
            foreach (var item in _items)
            {
                if (item.ItemId == itemId)
                {
                    return item.Quantity;
                }
            }
            return 0;
        }

        /// <summary>
        /// Get all items in the inventory
        /// </summary>
        public InventoryItem[] GetAllItems()
        {
            var result = new InventoryItem[_items.Length];
            System.Array.Copy(_items, result, _items.Length);
            return result;
        }

        /// <summary>
        /// Clear all items from inventory
        /// </summary>
        public void Clear()
        {
            _items = new InventoryItem[0];
            EntitySystemLogger.LogDebug("InventoryComponent", $"Cleared inventory for {_owner?.EntityId}");
        }

        private float CalculateCurrentWeight()
        {
            float totalWeight = 0f;
            foreach (var item in _items)
            {
                totalWeight += item.Weight * item.Quantity;
            }
            return totalWeight;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _maxSlots = Mathf.Max(1, _maxSlots);
            _maxWeight = Mathf.Max(1f, _maxWeight);
        }
#endif
    }

    /// <summary>
    /// Example position component for tracking entity location
    /// </summary>
    [CreateAssetMenu(fileName = "PositionComponent", menuName = "Galactic/Components/Position")]
    public class PositionComponent : ScriptableObject, IEntityComponent
    {
        [SerializeField] private Vector3 _position = Vector3.zero;
        [SerializeField] private Vector3 _velocity = Vector3.zero;
        [SerializeField] private float _maxSpeed = 10f;
        [SerializeField] private string _systemId = "unknown";

        private GameEntity _owner;
        private Vector3 _lastPosition;

        public string ComponentId => $"position_{GetInstanceID()}";
        public Vector3 Position => _position;
        public Vector3 Velocity => _velocity;
        public float MaxSpeed => _maxSpeed;
        public string SystemId => _systemId;
        public float CurrentSpeed => _velocity.magnitude;

        public void Initialize(GameEntity owner)
        {
            _owner = owner;
            _lastPosition = _position;
            
            // Sync with Transform if available
            if (owner.transform != null)
            {
                _position = owner.transform.position;
            }

            EntitySystemLogger.LogDebug("PositionComponent", 
                $"Position component initialized for {owner.EntityId} at {_position}");
        }

        public void Cleanup()
        {
            _owner = null;
        }

        public bool IsValid()
        {
            return !float.IsNaN(_position.x) && !float.IsNaN(_position.y) && !float.IsNaN(_position.z) &&
                   !float.IsNaN(_velocity.x) && !float.IsNaN(_velocity.y) && !float.IsNaN(_velocity.z);
        }

        /// <summary>
        /// Update position based on velocity
        /// </summary>
        public void UpdatePosition(float deltaTime)
        {
            if (deltaTime <= 0) return;

            _lastPosition = _position;
            _position += _velocity * deltaTime;

            // Sync with Transform
            if (_owner?.transform != null)
            {
                _owner.transform.position = _position;
            }

            // Update spatial index in registry
            EntityRegistry.Instance?.UpdateSpatialIndex(_owner?.EntityId, _position);
        }

        /// <summary>
        /// Set velocity with speed limiting
        /// </summary>
        public void SetVelocity(Vector3 velocity)
        {
            _velocity = velocity;
            if (_velocity.magnitude > _maxSpeed)
            {
                _velocity = _velocity.normalized * _maxSpeed;
            }
        }

        /// <summary>
        /// Move towards a target position
        /// </summary>
        public void MoveTowards(Vector3 target, float speed)
        {
            var direction = (target - _position).normalized;
            SetVelocity(direction * Mathf.Min(speed, _maxSpeed));
        }

        /// <summary>
        /// Set position directly
        /// </summary>
        public void SetPosition(Vector3 position, string systemId = null)
        {
            _lastPosition = _position;
            _position = position;
            
            if (!string.IsNullOrEmpty(systemId))
            {
                _systemId = systemId;
            }

            // Sync with Transform
            if (_owner?.transform != null)
            {
                _owner.transform.position = _position;
            }

            // Update spatial index
            EntityRegistry.Instance?.UpdateSpatialIndex(_owner?.EntityId, _position);
        }

        /// <summary>
        /// Get distance to another position
        /// </summary>
        public float GetDistanceTo(Vector3 otherPosition)
        {
            return Vector3.Distance(_position, otherPosition);
        }

        /// <summary>
        /// Get distance to another entity
        /// </summary>
        public float GetDistanceTo(GameEntity otherEntity)
        {
            var otherPosition = otherEntity.GetComponent<PositionComponent>();
            return otherPosition != null ? GetDistanceTo(otherPosition.Position) : float.MaxValue;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _maxSpeed = Mathf.Max(0f, _maxSpeed);
        }
#endif
    }

    /// <summary>
    /// Represents an item in an inventory
    /// </summary>
    [System.Serializable]
    public struct InventoryItem
    {
        public string ItemId;
        public int Quantity;
        public float Weight;
    }
}