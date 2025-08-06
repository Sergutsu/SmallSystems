using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GalacticVentures.EntitySystem.Core
{
    /// <summary>
    /// Manages saving and loading of entity data
    /// </summary>
    public class EntityPersistenceManager : MonoBehaviour
    {
        private static EntityPersistenceManager _instance;
        public static EntityPersistenceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("EntityPersistenceManager");
                    _instance = go.AddComponent<EntityPersistenceManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private string _saveDirectory = "EntitySaves";
        [SerializeField] private bool _useCompression = false;
        [SerializeField] private bool _createBackups = true;
        [SerializeField] private int _maxBackups = 5;

        private string SavePath => Path.Combine(Application.persistentDataPath, _saveDirectory);

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                EnsureSaveDirectoryExists();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Save all entities to a file
        /// </summary>
        public bool SaveAllEntities(string fileName = null)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = $"entities_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            }

            var registry = EntityRegistry.Instance;
            var allEntities = registry.CreateQuery().ExecuteQuery(registry.CreateQuery()).Entities;

            return SaveEntities(allEntities, fileName);
        }

        /// <summary>
        /// Save specific entities to a file
        /// </summary>
        public bool SaveEntities(IEnumerable<GameEntity> entities, string fileName)
        {
            if (entities == null || string.IsNullOrEmpty(fileName))
            {
                if (_enableLogging)
                    Debug.LogError("EntityPersistenceManager: Invalid parameters for SaveEntities");
                return false;
            }

            try
            {
                var saveData = new EntitySaveData();
                saveData.Timestamp = DateTime.UtcNow;
                saveData.Version = "1.0";

                foreach (var entity in entities.Where(e => e != null))
                {
                    try
                    {
                        var entityData = new EntityData(entity);
                        saveData.Entities.Add(entityData);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"EntityPersistenceManager: Failed to serialize entity {entity.EntityId}: {ex}");
                        // Continue with other entities
                    }
                }

                var json = JsonUtility.ToJson(saveData, true);
                var filePath = Path.Combine(SavePath, fileName);

                // Create backup if enabled
                if (_createBackups && File.Exists(filePath))
                {
                    CreateBackup(filePath);
                }

                File.WriteAllText(filePath, json);

                if (_enableLogging)
                    Debug.Log($"EntityPersistenceManager: Saved {saveData.Entities.Count} entities to {fileName}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"EntityPersistenceManager: Failed to save entities: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Load entities from a file
        /// </summary>
        public bool LoadEntities(string fileName, bool clearExisting = false)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                if (_enableLogging)
                    Debug.LogError("EntityPersistenceManager: Invalid file name for LoadEntities");
                return false;
            }

            var filePath = Path.Combine(SavePath, fileName);
            if (!File.Exists(filePath))
            {
                if (_enableLogging)
                    Debug.LogError($"EntityPersistenceManager: Save file not found: {filePath}");
                return false;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var saveData = JsonUtility.FromJson<EntitySaveData>(json);

                if (saveData == null || saveData.Entities == null)
                {
                    if (_enableLogging)
                        Debug.LogError("EntityPersistenceManager: Invalid save data format");
                    return false;
                }

                // Clear existing entities if requested
                if (clearExisting)
                {
                    ClearAllEntities();
                }

                var loadedCount = 0;
                var failedCount = 0;

                foreach (var entityData in saveData.Entities)
                {
                    try
                    {
                        if (RestoreEntity(entityData))
                        {
                            loadedCount++;
                        }
                        else
                        {
                            failedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"EntityPersistenceManager: Failed to restore entity {entityData.Id}: {ex}");
                        failedCount++;
                    }
                }

                if (_enableLogging)
                    Debug.Log($"EntityPersistenceManager: Loaded {loadedCount} entities, {failedCount} failed from {fileName}");

                return failedCount == 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"EntityPersistenceManager: Failed to load entities: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Restore a single entity from data
        /// </summary>
        private bool RestoreEntity(EntityData entityData)
        {
            if (entityData == null || string.IsNullOrEmpty(entityData.EntityTypeName))
                return false;

            try
            {
                // Get the entity type
                var entityType = Type.GetType(entityData.EntityTypeName);
                if (entityType == null || !typeof(GameEntity).IsAssignableFrom(entityType))
                {
                    Debug.LogError($"EntityPersistenceManager: Invalid entity type {entityData.EntityTypeName}");
                    return false;
                }

                // Create GameObject and add entity component
                var gameObject = new GameObject($"Restored_{entityData.Id}");
                var entity = gameObject.AddComponent(entityType) as GameEntity;
                
                if (entity == null)
                {
                    DestroyImmediate(gameObject);
                    return false;
                }

                // Restore transform
                gameObject.transform.position = entityData.Position;
                gameObject.transform.rotation = entityData.Rotation;
                gameObject.transform.localScale = entityData.Scale;

                // Set entity properties using reflection (since fields are protected)
                var entityIdField = typeof(GameEntity).GetField("_entityId", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var factionField = typeof(GameEntity).GetField("_faction", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                entityIdField?.SetValue(entity, entityData.Id);
                factionField?.SetValue(entity, entityData.Faction);

                // Restore components
                foreach (var componentData in entityData.Components)
                {
                    try
                    {
                        var component = componentData.CreateComponent();
                        if (component != null)
                        {
                            entity.AddComponent(component);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"EntityPersistenceManager: Failed to restore component {componentData.TypeName}: {ex}");
                        // Continue with other components
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"EntityPersistenceManager: Failed to restore entity {entityData.Id}: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Get list of available save files
        /// </summary>
        public List<string> GetSaveFiles()
        {
            try
            {
                EnsureSaveDirectoryExists();
                return Directory.GetFiles(SavePath, "*.json")
                    .Select(Path.GetFileName)
                    .OrderByDescending(f => File.GetLastWriteTime(Path.Combine(SavePath, f)))
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.LogError($"EntityPersistenceManager: Failed to get save files: {ex}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Delete a save file
        /// </summary>
        public bool DeleteSaveFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            try
            {
                var filePath = Path.Combine(SavePath, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    if (_enableLogging)
                        Debug.Log($"EntityPersistenceManager: Deleted save file {fileName}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"EntityPersistenceManager: Failed to delete save file {fileName}: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Clear all entities from the scene
        /// </summary>
        private void ClearAllEntities()
        {
            var registry = EntityRegistry.Instance;
            var allEntities = registry.CreateQuery().ExecuteQuery(registry.CreateQuery()).Entities.ToList();

            foreach (var entity in allEntities)
            {
                if (entity != null && entity.gameObject != null)
                {
                    DestroyImmediate(entity.gameObject);
                }
            }

            registry.Clear();
        }

        /// <summary>
        /// Create a backup of an existing save file
        /// </summary>
        private void CreateBackup(string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var extension = Path.GetExtension(filePath);
                
                var backupFileName = $"{fileName}_backup_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
                var backupPath = Path.Combine(directory, backupFileName);
                
                File.Copy(filePath, backupPath);

                // Clean up old backups
                CleanupOldBackups(directory, fileName, extension);
            }
            catch (Exception ex)
            {
                Debug.LogError($"EntityPersistenceManager: Failed to create backup: {ex}");
            }
        }

        /// <summary>
        /// Remove old backup files beyond the maximum count
        /// </summary>
        private void CleanupOldBackups(string directory, string baseFileName, string extension)
        {
            try
            {
                var backupPattern = $"{baseFileName}_backup_*{extension}";
                var backupFiles = Directory.GetFiles(directory, backupPattern)
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .Skip(_maxBackups)
                    .ToList();

                foreach (var backupFile in backupFiles)
                {
                    File.Delete(backupFile);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"EntityPersistenceManager: Failed to cleanup old backups: {ex}");
            }
        }

        /// <summary>
        /// Ensure the save directory exists
        /// </summary>
        private void EnsureSaveDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(SavePath))
                {
                    Directory.CreateDirectory(SavePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"EntityPersistenceManager: Failed to create save directory: {ex}");
            }
        }

        /// <summary>
        /// Auto-save entities at regular intervals
        /// </summary>
        public void StartAutoSave(float intervalSeconds = 300f) // 5 minutes default
        {
            InvokeRepeating(nameof(AutoSave), intervalSeconds, intervalSeconds);
        }

        /// <summary>
        /// Stop auto-save
        /// </summary>
        public void StopAutoSave()
        {
            CancelInvoke(nameof(AutoSave));
        }

        private void AutoSave()
        {
            SaveAllEntities($"autosave_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        }
    }

    /// <summary>
    /// Container for entity save data
    /// </summary>
    [Serializable]
    public class EntitySaveData
    {
        [SerializeField] public string Version;
        [SerializeField] public DateTime Timestamp;
        [SerializeField] public List<EntityData> Entities = new();
    }
}