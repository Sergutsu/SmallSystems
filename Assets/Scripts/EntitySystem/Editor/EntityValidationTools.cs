#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using GalacticVentures.EntitySystem.Core;

namespace GalacticVentures.EntitySystem.Editor
{
    /// <summary>
    /// Tools for validating entity system integrity
    /// </summary>
    public static class EntityValidationTools
    {
        [MenuItem("Tools/Entity System/Validate All Entities")]
        public static void ValidateAllEntities()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Validation Error", "Entity validation is only available during Play Mode", "OK");
                return;
            }

            var registry = EntityRegistry.Instance;
            if (registry == null)
            {
                EditorUtility.DisplayDialog("Validation Error", "EntityRegistry not found", "OK");
                return;
            }

            var results = new ValidationResults();
            var allEntities = registry.CreateQuery().ExecuteQuery(registry.CreateQuery()).Entities;

            foreach (var entity in allEntities)
            {
                ValidateEntity(entity, results);
            }

            DisplayValidationResults(results);
        }

        [MenuItem("Tools/Entity System/Validate Selected Entity")]
        public static void ValidateSelectedEntity()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Validation Error", "Entity validation is only available during Play Mode", "OK");
                return;
            }

            var selectedObject = Selection.activeGameObject;
            if (selectedObject == null)
            {
                EditorUtility.DisplayDialog("Validation Error", "No GameObject selected", "OK");
                return;
            }

            var entity = selectedObject.GetComponent<GameEntity>();
            if (entity == null)
            {
                EditorUtility.DisplayDialog("Validation Error", "Selected GameObject is not a GameEntity", "OK");
                return;
            }

            var results = new ValidationResults();
            ValidateEntity(entity, results);
            DisplayValidationResults(results, $"Validation Results for {entity.EntityId}");
        }

        [MenuItem("Tools/Entity System/Fix Common Issues")]
        public static void FixCommonIssues()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Fix Error", "Entity fixing is only available during Play Mode", "OK");
                return;
            }

            var registry = EntityRegistry.Instance;
            if (registry == null)
            {
                EditorUtility.DisplayDialog("Fix Error", "EntityRegistry not found", "OK");
                return;
            }

            int fixedIssues = 0;
            var allEntities = registry.CreateQuery().ExecuteQuery(registry.CreateQuery()).Entities.ToList();

            foreach (var entity in allEntities)
            {
                if (entity == null) continue;

                // Fix missing entity IDs
                if (string.IsNullOrEmpty(entity.EntityId))
                {
                    // This would require reflection to access protected field
                    Debug.LogWarning($"Entity {entity.gameObject.name} has missing ID - manual fix required");
                }

                // Fix unregistered entities
                if (!entity.IsRegistered)
                {
                    registry.RegisterEntity(entity);
                    fixedIssues++;
                    Debug.Log($"Re-registered entity {entity.EntityId}");
                }

                // Validate and fix components
                var components = entity.GetAllComponents().ToList();
                foreach (var component in components)
                {
                    if (component == null)
                    {
                        // Remove null components (this would need to be implemented in GameEntity)
                        Debug.LogWarning($"Entity {entity.EntityId} has null component - manual cleanup required");
                    }
                    else if (component is IEntityComponent entityComponent && !entityComponent.IsValid())
                    {
                        Debug.LogWarning($"Entity {entity.EntityId} has invalid component {component.GetType().Name}");
                    }
                }
            }

            EditorUtility.DisplayDialog("Fix Complete", $"Fixed {fixedIssues} issues", "OK");
        }

        [MenuItem("Tools/Entity System/Generate Entity Report")]
        public static void GenerateEntityReport()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Report Error", "Entity reporting is only available during Play Mode", "OK");
                return;
            }

            var registry = EntityRegistry.Instance;
            if (registry == null)
            {
                EditorUtility.DisplayDialog("Report Error", "EntityRegistry not found", "OK");
                return;
            }

            var report = GenerateDetailedReport(registry);
            var reportPath = System.IO.Path.Combine(Application.dataPath, "EntitySystemReport.txt");
            
            try
            {
                System.IO.File.WriteAllText(reportPath, report);
                EditorUtility.DisplayDialog("Report Generated", $"Entity system report saved to:\n{reportPath}", "OK");
                EditorUtility.RevealInFinder(reportPath);
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Report Error", $"Failed to save report:\n{ex.Message}", "OK");
            }
        }

        private static void ValidateEntity(GameEntity entity, ValidationResults results)
        {
            results.TotalEntities++;

            if (entity == null)
            {
                results.Issues.Add("Null entity found in registry");
                results.ErrorCount++;
                return;
            }

            // Validate entity ID
            if (string.IsNullOrEmpty(entity.EntityId))
            {
                results.Issues.Add($"Entity {entity.gameObject.name} has missing or empty ID");
                results.ErrorCount++;
            }

            // Validate registration
            if (!entity.IsRegistered)
            {
                results.Issues.Add($"Entity {entity.EntityId} is not registered with EntityRegistry");
                results.WarningCount++;
            }

            // Validate GameObject
            if (entity.gameObject == null)
            {
                results.Issues.Add($"Entity {entity.EntityId} has null GameObject");
                results.ErrorCount++;
                return;
            }

            // Validate components
            var components = entity.GetAllComponents().ToList();
            results.TotalComponents += components.Count;

            foreach (var component in components)
            {
                if (component == null)
                {
                    results.Issues.Add($"Entity {entity.EntityId} has null component");
                    results.ErrorCount++;
                    continue;
                }

                // Validate IEntityComponent implementations
                if (component is IEntityComponent entityComponent)
                {
                    if (!entityComponent.IsValid())
                    {
                        results.Issues.Add($"Entity {entity.EntityId} has invalid component {component.GetType().Name}");
                        results.WarningCount++;
                    }

                    if (string.IsNullOrEmpty(entityComponent.ComponentId))
                    {
                        results.Issues.Add($"Entity {entity.EntityId} component {component.GetType().Name} has missing ComponentId");
                        results.WarningCount++;
                    }
                }
            }

            // Check for duplicate component types
            var componentTypes = components.Select(c => c.GetType()).ToList();
            var duplicateTypes = componentTypes.GroupBy(t => t).Where(g => g.Count() > 1).Select(g => g.Key);
            
            foreach (var duplicateType in duplicateTypes)
            {
                results.Issues.Add($"Entity {entity.EntityId} has multiple components of type {duplicateType.Name}");
                results.WarningCount++;
            }
        }

        private static void DisplayValidationResults(ValidationResults results, string title = "Entity System Validation Results")
        {
            var message = $"Validation Complete:\n\n" +
                         $"Entities Checked: {results.TotalEntities}\n" +
                         $"Components Checked: {results.TotalComponents}\n" +
                         $"Errors: {results.ErrorCount}\n" +
                         $"Warnings: {results.WarningCount}\n\n";

            if (results.Issues.Count > 0)
            {
                message += "Issues Found:\n";
                foreach (var issue in results.Issues.Take(10)) // Show first 10 issues
                {
                    message += $"• {issue}\n";
                }

                if (results.Issues.Count > 10)
                {
                    message += $"... and {results.Issues.Count - 10} more issues\n";
                }

                // Log all issues to console
                foreach (var issue in results.Issues)
                {
                    if (issue.Contains("Error") || issue.Contains("null"))
                    {
                        Debug.LogError($"Entity Validation: {issue}");
                    }
                    else
                    {
                        Debug.LogWarning($"Entity Validation: {issue}");
                    }
                }
            }
            else
            {
                message += "No issues found!";
            }

            EditorUtility.DisplayDialog(title, message, "OK");
        }

        private static string GenerateDetailedReport(EntityRegistry registry)
        {
            var report = new System.Text.StringBuilder();
            var stats = registry.GetStats();

            report.AppendLine("ENTITY SYSTEM REPORT");
            report.AppendLine("===================");
            report.AppendLine($"Generated: {System.DateTime.Now}");
            report.AppendLine();

            // Registry statistics
            report.AppendLine("REGISTRY STATISTICS");
            report.AppendLine("------------------");
            report.AppendLine($"Total Entities: {stats.TotalEntities}");
            report.AppendLine($"Component Types Indexed: {stats.ComponentTypesIndexed}");
            report.AppendLine($"Spatial Cells Used: {stats.SpatialCellsUsed}");
            report.AppendLine();

            // Faction distribution
            report.AppendLine("FACTION DISTRIBUTION");
            report.AppendLine("-------------------");
            if (stats.FactionCounts != null)
            {
                foreach (var kvp in stats.FactionCounts.OrderByDescending(kvp => kvp.Value))
                {
                    report.AppendLine($"{kvp.Key}: {kvp.Value}");
                }
            }
            report.AppendLine();

            // Component analysis
            var allEntities = registry.CreateQuery().ExecuteQuery(registry.CreateQuery()).Entities;
            var componentTypes = new System.Collections.Generic.Dictionary<System.Type, int>();

            foreach (var entity in allEntities)
            {
                foreach (var componentType in entity.GetComponentTypes())
                {
                    componentTypes[componentType] = componentTypes.GetValueOrDefault(componentType, 0) + 1;
                }
            }

            report.AppendLine("COMPONENT DISTRIBUTION");
            report.AppendLine("---------------------");
            foreach (var kvp in componentTypes.OrderByDescending(kvp => kvp.Value))
            {
                report.AppendLine($"{kvp.Key.Name}: {kvp.Value} entities");
            }
            report.AppendLine();

            // System performance
            var eventBus = EventBus.Instance;
            var componentManager = ComponentManager.Instance;

            report.AppendLine("SYSTEM PERFORMANCE");
            report.AppendLine("------------------");
            
            if (eventBus != null)
            {
                var eventStats = eventBus.GetStats();
                report.AppendLine($"EventBus Handlers: {eventStats.HandlerCount}");
                report.AppendLine($"EventBus Queued Events: {eventStats.QueuedEventCount}");
                report.AppendLine($"EventBus Event Types: {eventStats.RegisteredEventTypes}");
            }

            if (componentManager != null)
            {
                var componentStats = componentManager.GetStats();
                report.AppendLine($"ComponentManager Active Components: {componentStats.ActiveComponents}");
                report.AppendLine($"ComponentManager Initialization Queue: {componentStats.InitializationQueueSize}");
                report.AppendLine($"ComponentManager Cleanup Queue: {componentStats.CleanupQueueSize}");
            }

            return report.ToString();
        }

        private class ValidationResults
        {
            public int TotalEntities = 0;
            public int TotalComponents = 0;
            public int ErrorCount = 0;
            public int WarningCount = 0;
            public System.Collections.Generic.List<string> Issues = new();
        }
    }
}
#endif