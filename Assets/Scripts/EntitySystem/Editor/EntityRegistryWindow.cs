#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using GalacticVentures.EntitySystem.Core;

namespace GalacticVentures.EntitySystem.Editor
{
    /// <summary>
    /// Editor window for visualizing and managing the EntityRegistry
    /// </summary>
    public class EntityRegistryWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private string _searchFilter = "";
        private EntityFaction _factionFilter = EntityFaction.None;
        private bool _showComponents = true;
        private bool _showStats = true;
        private bool _autoRefresh = true;
        private float _lastRefreshTime;
        private const float REFRESH_INTERVAL = 1f;

        [MenuItem("Tools/Entity System/Entity Registry")]
        public static void ShowWindow()
        {
            var window = GetWindow<EntityRegistryWindow>("Entity Registry");
            window.Show();
        }

        private void OnEnable()
        {
            _lastRefreshTime = Time.realtimeSinceStartup;
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Entity Registry is only available during Play Mode", MessageType.Info);
                return;
            }

            var registry = EntityRegistry.Instance;
            if (registry == null)
            {
                EditorGUILayout.HelpBox("EntityRegistry instance not found", MessageType.Warning);
                return;
            }

            DrawToolbar();
            DrawStats(registry);
            DrawEntityList(registry);

            // Auto-refresh
            if (_autoRefresh && Time.realtimeSinceStartup - _lastRefreshTime > REFRESH_INTERVAL)
            {
                _lastRefreshTime = Time.realtimeSinceStartup;
                Repaint();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Search filter
            GUILayout.Label("Search:", GUILayout.Width(50));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarTextField, GUILayout.Width(150));

            GUILayout.Space(10);

            // Faction filter
            GUILayout.Label("Faction:", GUILayout.Width(50));
            _factionFilter = (EntityFaction)EditorGUILayout.EnumPopup(_factionFilter, EditorStyles.toolbarPopup, GUILayout.Width(120));

            GUILayout.FlexibleSpace();

            // Options
            _showComponents = GUILayout.Toggle(_showComponents, "Components", EditorStyles.toolbarButton);
            _showStats = GUILayout.Toggle(_showStats, "Stats", EditorStyles.toolbarButton);
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton);

            // Refresh button
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                Repaint();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStats(EntityRegistry registry)
        {
            if (!_showStats) return;

            var stats = registry.GetStats();

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Registry Statistics", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Total Entities: {stats.TotalEntities}");
            EditorGUILayout.LabelField($"Component Types: {stats.ComponentTypesIndexed}");
            EditorGUILayout.LabelField($"Spatial Cells: {stats.SpatialCellsUsed}");
            EditorGUILayout.EndHorizontal();

            // Faction breakdown
            if (stats.FactionCounts != null && stats.FactionCounts.Count > 0)
            {
                EditorGUILayout.LabelField("Faction Distribution:", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                foreach (var kvp in stats.FactionCounts.Where(kvp => kvp.Value > 0))
                {
                    EditorGUILayout.LabelField($"{kvp.Key}: {kvp.Value}");
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawEntityList(EntityRegistry registry)
        {
            var query = registry.CreateQuery();

            // Apply filters
            if (_factionFilter != EntityFaction.None)
            {
                query.WithFaction(_factionFilter);
            }

            var result = registry.ExecuteQuery(query);
            var entities = result.Entities.ToList();

            // Apply search filter
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                entities = entities.Where(e => 
                    e.EntityId.Contains(_searchFilter) || 
                    e.GetType().Name.Contains(_searchFilter) ||
                    e.gameObject.name.Contains(_searchFilter)
                ).ToList();
            }

            EditorGUILayout.LabelField($"Entities ({entities.Count})", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var entity in entities)
            {
                if (entity == null) continue;

                DrawEntityItem(entity);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawEntityItem(GameEntity entity)
        {
            EditorGUILayout.BeginVertical("box");

            // Entity header
            EditorGUILayout.BeginHorizontal();

            // Entity name and selection
            var entityName = $"{entity.gameObject.name} ({entity.EntityId})";
            if (GUILayout.Button(entityName, EditorStyles.linkLabel))
            {
                Selection.activeGameObject = entity.gameObject;
                EditorGUIUtility.PingObject(entity.gameObject);
            }

            GUILayout.FlexibleSpace();

            // Faction color indicator
            var factionColor = GetFactionColor(entity.Faction);
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = factionColor;
            GUILayout.Label(entity.Faction.ToString(), EditorStyles.miniButton, GUILayout.Width(100));
            GUI.backgroundColor = oldColor;

            EditorGUILayout.EndHorizontal();

            // Entity details
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Position:", GUILayout.Width(60));
            EditorGUILayout.LabelField(entity.transform.position.ToString("F1"));
            EditorGUILayout.LabelField("Registered:", GUILayout.Width(70));
            EditorGUILayout.LabelField(entity.IsRegistered ? "✓" : "✗", GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();

            // Components
            if (_showComponents)
            {
                var components = entity.GetAllComponents().ToList();
                if (components.Count > 0)
                {
                    EditorGUILayout.LabelField($"Components ({components.Count}):", EditorStyles.miniBoldLabel);
                    EditorGUI.indentLevel++;

                    foreach (var component in components)
                    {
                        if (component == null) continue;

                        EditorGUILayout.BeginHorizontal();
                        
                        // Component type
                        EditorGUILayout.LabelField(component.GetType().Name, GUILayout.Width(150));
                        
                        // Validation status
                        if (component is IEntityComponent entityComponent)
                        {
                            var isValid = entityComponent.IsValid();
                            var statusColor = isValid ? Color.green : Color.red;
                            var status = isValid ? "✓" : "✗";
                            
                            var oldGUIColor = GUI.color;
                            GUI.color = statusColor;
                            EditorGUILayout.LabelField(status, GUILayout.Width(20));
                            GUI.color = oldGUIColor;
                        }
                        else
                        {
                            EditorGUILayout.LabelField("-", GUILayout.Width(20));
                        }

                        // Select component button
                        if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
                        {
                            Selection.activeObject = component;
                            EditorGUIUtility.PingObject(component);
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private Color GetFactionColor(EntityFaction faction)
        {
            return faction switch
            {
                EntityFaction.Player => Color.green,
                EntityFaction.TradingGuild => Color.blue,
                EntityFaction.MilitaryAlliance => Color.red,
                EntityFaction.PirateClans => Color.magenta,
                EntityFaction.ScientificConsortium => Color.cyan,
                EntityFaction.IndependentTraders => Color.yellow,
                EntityFaction.CorporateConglomerate => new Color(1f, 0.5f, 0f), // Orange
                EntityFaction.RebellionForces => new Color(0.5f, 0f, 0.5f), // Purple
                EntityFaction.AlienSpecies => new Color(0f, 1f, 0.5f), // Lime
                EntityFaction.NeutralStations => Color.gray,
                _ => Color.white
            };
        }
    }
}
#endif