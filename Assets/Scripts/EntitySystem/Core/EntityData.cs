using System;
using System.Collections.Generic;
using UnityEngine;

namespace GalacticVentures.EntitySystem.Core
{
    /// <summary>
    /// Serializable data structure for entity persistence
    /// </summary>
    [Serializable]
    public class EntityData
    {
        [SerializeField] public string Id;
        [SerializeField] public EntityFaction Faction;
        [SerializeField] public Vector3 Position;
        [SerializeField] public Quaternion Rotation;
        [SerializeField] public Vector3 Scale;
        [SerializeField] public long CreationTimestamp;
        [SerializeField] public string CreatedBy;
        [SerializeField] public string EntityTypeName;
        [SerializeField] public List<ComponentData> Components = new();

        public EntityData()
        {
            CreationTimestamp = DateTimeOffset.UtcNow.Ticks;
            Scale = Vector3.one;
        }

        public EntityData(GameEntity entity) : this()
        {
            if (entity == null) return;

            Id = entity.EntityId;
            Faction = entity.Faction;
            Position = entity.transform.position;
            Rotation = entity.transform.rotation;
            Scale = entity.transform.localScale;
            EntityTypeName = entity.GetType().AssemblyQualifiedName;

            // Serialize components
            foreach (var component in entity.GetAllComponents())
            {
                try
                {
                    var componentData = new ComponentData(component);
                    Components.Add(componentData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"EntityData: Failed to serialize component {component.GetType().Name}: {ex}");
                }
            }
        }
    }

    /// <summary>
    /// Serializable data structure for component persistence
    /// </summary>
    [Serializable]
    public class ComponentData
    {
        [SerializeField] public string TypeName;
        [SerializeField] public string JsonData;
        [SerializeField] public bool IsScriptableObject;

        public ComponentData()
        {
        }

        public ComponentData(ScriptableObject component)
        {
            if (component == null) return;

            TypeName = component.GetType().AssemblyQualifiedName;
            IsScriptableObject = true;

            try
            {
                JsonData = JsonUtility.ToJson(component, true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"ComponentData: Failed to serialize component {component.GetType().Name}: {ex}");
                JsonData = "{}";
            }
        }

        public T CreateComponent<T>() where T : ScriptableObject
        {
            return CreateComponent() as T;
        }

        public ScriptableObject CreateComponent()
        {
            if (string.IsNullOrEmpty(TypeName) || string.IsNullOrEmpty(JsonData))
                return null;

            try
            {
                var type = Type.GetType(TypeName);
                if (type == null || !typeof(ScriptableObject).IsAssignableFrom(type))
                {
                    Debug.LogError($"ComponentData: Invalid component type {TypeName}");
                    return null;
                }

                var component = ScriptableObject.CreateInstance(type);
                JsonUtility.FromJsonOverwrite(JsonData, component);
                return component;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ComponentData: Failed to deserialize component {TypeName}: {ex}");
                return null;
            }
        }
    }
}