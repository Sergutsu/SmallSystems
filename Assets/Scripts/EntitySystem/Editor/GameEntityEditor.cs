#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using GalacticVentures.EntitySystem.Core;

namespace GalacticVentures.EntitySystem.Editor
{
    /// <summary>
    /// Custom inspector for GameEntity to show entity information and components
    /// </summary>
    [CustomEditor(typeof(GameEntity), true)]
    public class GameEntityEditor : UnityEditor.Editor
    {
        private bool _showComponents = true;
        private bool _showDebugInfo = false;

        public override void OnInspectorGUI()
        {
            var entity = (GameEntity)target;

            // Draw default inspector first
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Entity System Info", EditorStyles.boldLabel);

            // Entity basic info
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Entity ID", entity.EntityId ?? "Not Generated");
            EditorGUILayout.EnumPopup("Faction", entity.Faction);
            EditorGUILayout.Toggle("Is Registered", entity.IsRegistered);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            // Components section
            _showComponents = EditorGUILayout.Foldout(_showComponents, "Components", true);
            if (_showComponents)
            {
                EditorGUI.indentLevel++;
                
                var components = entity.GetAllComponents();
                if (components.Any())
                {
                    foreach (var component in components)
                    {
                        if (component != null)
                        {
                            EditorGUILayout.BeginHorizontal();
                            
                            // Component name
                            EditorGUILayout.LabelField(component.GetType().Name);
                            
                            // Validation status
                            if (component is IEntityComponent entityComponent)
                            {
                                bool isValid = entityComponent.IsValid();
                                var color = isValid ? Color.green : Color.red;
                                var status = isValid ? "✓" : "✗";
                                
                                var oldColor = GUI.color;
                                GUI.color = color;
                                EditorGUILayout.LabelField(status, GUILayout.Width(20));
                                GUI.color = oldColor;
                            }
                            
                            // Remove button
                            if (GUILayout.Button("Remove", GUILayout.Width(60)))
                            {
                                entity.RemoveComponent(component.GetType());
                            }
                            
                            EditorGUILayout.EndHorizontal();
                            
                            // Show component details
                            EditorGUI.indentLevel++;
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.ObjectField("Reference", component, component.GetType(), false);
                            EditorGUI.EndDisabledGroup();
                            EditorGUI.indentLevel--;
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No components attached");
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Debug info section
            if (Application.isPlaying)
            {
                _showDebugInfo = EditorGUILayout.Foldout(_showDebugInfo, "Debug Info", true);
                if (_showDebugInfo)
                {
                    EditorGUI.indentLevel++;
                    
                    var debugInfo = entity.GetDebugInfo();
                    EditorGUILayout.TextArea(debugInfo, GUILayout.Height(100));
                    
                    EditorGUI.indentLevel--;
                }
            }

            // Buttons
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Refresh Registry"))
            {
                if (Application.isPlaying)
                {
                    EntityRegistry.Instance.UnregisterEntity(entity.EntityId);
                    EntityRegistry.Instance.RegisterEntity(entity);
                }
            }
            
            if (GUILayout.Button("Generate New ID"))
            {
                // This would require making GenerateEntityId public or adding a method
                EditorUtility.DisplayDialog("Info", "Entity ID generation is handled automatically", "OK");
            }
            
            EditorGUILayout.EndHorizontal();

            // Auto-refresh in play mode
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
    }
}
#endif