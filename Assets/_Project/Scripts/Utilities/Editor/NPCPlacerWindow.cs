#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Master.Scripts.SaveSystem;

namespace Master.Scripts.Editor
{
    /// <summary>
    /// Editor window for placing filler NPCs on Terrain and Floor GameObjects,
    /// with automatic obstacle collision checking, direct serialization into LevelData,
    /// and interactive Scene View click-to-place support.
    /// </summary>
    public class NPCPlacerWindow : EditorWindow
    {
        [Header("Target Configuration")]
        [SerializeField] private LevelData targetLevelData;
        [SerializeField] private GameObject selectedNpcPrefab;
        [SerializeField] private List<GameObject> npcPrefabPool = new List<GameObject>();

        [Header("Placement Settings")]
        private bool clickToPlaceActive = false;
        private float npcRadius = 0.45f;
        private float npcHeight = 1.8f;
        private float randomYRotationMin = 0f;
        private float randomYRotationMax = 360f;
        private bool randomRotationEnabled = true;

        [Header("Surface & Collision Settings")]
        private LayerMask surfaceMask = ~0;
        private string floorNameFilter = "Floor";
        private float minSurfaceSlopeNormalY = 0.65f; // Ensures walkable surfaces

        [Header("Batch Scatter")]
        private Vector3 scatterCenter = Vector3.zero;
        private float scatterRadius = 15f;
        private int scatterCount = 5;
        private int maxScatterAttempts = 100;

        private Vector2 scrollPos;
        private Vector3 lastHitPoint;
        private bool lastHitValid = false;

        [MenuItem("Tools/NPC Placer")]
        public static void ShowWindow()
        {
            var window = GetWindow<NPCPlacerWindow>("NPC Placer");
            window.minSize = new Vector2(360, 520);
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;

            // Auto-detect LevelData from scene LevelLoader if unassigned
            if (targetLevelData == null)
            {
                var loader = FindObjectOfType<LevelLoader>();
                if (loader != null && loader.ActiveLevelData != null)
                {
                    targetLevelData = loader.ActiveLevelData;
                }
            }
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            clickToPlaceActive = false;
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUILayout.Space(8);
            EditorGUILayout.LabelField("NPC Placer Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Place filler NPCs on Floors and Terrain without clipping Obstacles. Writes directly to LevelData.", MessageType.Info);
            EditorGUILayout.Space(6);

            // ── Target LevelData ──
            EditorGUILayout.LabelField("Target Level Data", EditorStyles.boldLabel);
            targetLevelData = (LevelData)EditorGUILayout.ObjectField("LevelData Asset", targetLevelData, typeof(LevelData), false);

            if (targetLevelData == null)
            {
                EditorGUILayout.HelpBox("Please assign a LevelData asset to store placed NPCs.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField($"Currently contains: {targetLevelData.fillerNpcEntries.Count} filler NPC(s)", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(10);

            // ── NPC Selection ──
            EditorGUILayout.LabelField("NPC Prefab Settings", EditorStyles.boldLabel);
            selectedNpcPrefab = (GameObject)EditorGUILayout.ObjectField("Active NPC Prefab", selectedNpcPrefab, typeof(GameObject), false);

            // Prefab pool foldout for randomized placement
            SerializedObject so = new SerializedObject(this);
            SerializedProperty poolProp = so.FindProperty("npcPrefabPool");
            EditorGUILayout.PropertyField(poolProp, new GUIContent("Random Pool (Optional)"), true);
            so.ApplyModifiedProperties();

            EditorGUILayout.Space(6);
            randomRotationEnabled = EditorGUILayout.Toggle("Randomize Y Rotation", randomRotationEnabled);
            if (randomRotationEnabled)
            {
                EditorGUILayout.BeginHorizontal();
                randomYRotationMin = EditorGUILayout.FloatField("Min Angle", randomYRotationMin);
                randomYRotationMax = EditorGUILayout.FloatField("Max Angle", randomYRotationMax);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);

            // ── Collision & Surface Settings ──
            EditorGUILayout.LabelField("Surface & Obstacle Clearance", EditorStyles.boldLabel);
            surfaceMask = EditorGUILayout.MaskField("Raycast LayerMask", surfaceMask, UnityEditorInternal.InternalEditorUtility.layers);
            floorNameFilter = EditorGUILayout.TextField("Floor Name Filter", floorNameFilter);
            minSurfaceSlopeNormalY = EditorGUILayout.Slider("Min Walkable Slope", minSurfaceSlopeNormalY, 0.4f, 1f);
            npcRadius = EditorGUILayout.FloatField("NPC Clearance Radius", npcRadius);
            npcHeight = EditorGUILayout.FloatField("NPC Height", npcHeight);

            EditorGUILayout.Space(12);

            // ── Mode 1: Click to Place ──
            EditorGUILayout.LabelField("Mode 1: Interactive Click-to-Place", EditorStyles.boldLabel);
            GUI.backgroundColor = clickToPlaceActive ? new Color(0.4f, 1f, 0.4f) : Color.white;
            if (GUILayout.Button(clickToPlaceActive ? "■ Click-to-Place Active (Click in Scene)" : "▶ Enable Click-to-Place in SceneView", GUILayout.Height(32)))
            {
                clickToPlaceActive = !clickToPlaceActive;
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(12);

            // ── Mode 2: Area Scatter ──
            EditorGUILayout.LabelField("Mode 2: Area Scatter (Batch)", EditorStyles.boldLabel);
            scatterCenter = EditorGUILayout.Vector3Field("Area Center", scatterCenter);
            if (GUILayout.Button("Use Scene View Camera Focus Point"))
            {
                if (SceneView.lastActiveSceneView != null)
                {
                    scatterCenter = SceneView.lastActiveSceneView.pivot;
                }
            }
            scatterRadius = EditorGUILayout.FloatField("Scatter Radius", scatterRadius);
            scatterCount = EditorGUILayout.IntField("Count to Spawn", scatterCount);

            if (GUILayout.Button("Scatter NPCs in Area", GUILayout.Height(28)))
            {
                ExecuteAreaScatter();
            }

            EditorGUILayout.Space(12);

            // ── Clear / Utilities ──
            if (targetLevelData != null && targetLevelData.fillerNpcEntries.Count > 0)
            {
                EditorGUILayout.LabelField("LevelData Entries Management", EditorStyles.boldLabel);
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button($"Clear All Filler NPCs ({targetLevelData.fillerNpcEntries.Count})", GUILayout.Height(26)))
                {
                    if (EditorUtility.DisplayDialog("Clear Filler NPCs", $"Are you sure you want to remove all {targetLevelData.fillerNpcEntries.Count} filler NPC entries from {targetLevelData.name}?", "Clear", "Cancel"))
                    {
                        Undo.RecordObject(targetLevelData, "Clear Filler NPCs");
                        targetLevelData.fillerNpcEntries.Clear();
                        EditorUtility.SetDirty(targetLevelData);
                        AssetDatabase.SaveAssets();
                    }
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndScrollView();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!clickToPlaceActive) return;

            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            Event e = Event.current;

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 500f, surfaceMask))
            {
                lastHitPoint = hit.point;
                lastHitValid = IsValidSurface(hit.collider, hit.normal) && HasObstacleClearance(hit.point, hit.collider);

                // Visual disc indicator
                Handles.color = lastHitValid ? new Color(0.2f, 1f, 0.2f, 0.6f) : new Color(1f, 0.2f, 0.2f, 0.6f);
                Handles.DrawSolidDisc(hit.point, hit.normal, npcRadius);
                Handles.DrawWireDisc(hit.point, Vector3.up, npcRadius);
                Handles.DrawWireCube(hit.point + Vector3.up * (npcHeight * 0.5f), new Vector3(npcRadius * 2f, npcHeight, npcRadius * 2f));

                SceneView.RepaintAll();

                // Prevent standard selection box when clicking to place
                if (e.type == EventType.Layout)
                {
                    HandleUtility.AddDefaultControl(controlId);
                }

                // Left Click: Place NPC
                if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
                {
                    if (lastHitValid)
                    {
                        GameObject prefabToSpawn = GetSelectedOrRandomPrefab();
                        if (prefabToSpawn != null)
                        {
                            float yRot = randomRotationEnabled ? Random.Range(randomYRotationMin, randomYRotationMax) : 0f;
                            RecordNpcToLevelData(prefabToSpawn, hit.point, new Vector3(0, yRot, 0));
                            e.Use();
                        }
                        else
                        {
                            Debug.LogWarning("[NPC Placer] No NPC prefab assigned to spawn. Please assign a prefab or pool in the NPC Placer window.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[NPC Placer] Cannot place NPC: Surface is not Floor/Terrain or location clips an Obstacle.");
                    }
                }
            }
        }

        private bool IsValidSurface(Collider col, Vector3 surfaceNormal)
        {
            if (col == null) return false;

            // Must be relatively horizontal surface (not a wall or steep incline)
            if (surfaceNormal.y < minSurfaceSlopeNormalY) return false;

            // 1. Terrain check
            if (col.GetComponent<Terrain>() != null || col is TerrainCollider) return true;

            // 2. Name check
            string colName = col.gameObject.name.ToLower();
            if (!string.IsNullOrEmpty(floorNameFilter) && colName.Contains(floorNameFilter.ToLower()))
            {
                return true;
            }

            if (colName.Contains("ground") || colName.Contains("terrain"))
            {
                return true;
            }

            // 3. Tag check
            try
            {
                if (col.CompareTag("Untagged") || col.CompareTag("Default"))
                {
                    return true;
                }
            }
            catch { }

            return false;
        }

        private bool HasObstacleClearance(Vector3 position, Collider groundCollider = null)
        {
            Vector3 pointBottom = position + Vector3.up * npcRadius;
            Vector3 pointTop = position + Vector3.up * Mathf.Max(npcRadius, npcHeight - npcRadius);

            Collider[] hits = Physics.OverlapCapsule(pointBottom, pointTop, npcRadius);
            int obstacleLayer = LayerMask.NameToLayer("Obstacle");

            foreach (var col in hits)
            {
                if (col == null || col.isTrigger) continue;
                if (groundCollider != null && col == groundCollider) continue;

                // Check "Obstacle" tag
                try
                {
                    if (col.CompareTag("Obstacle")) return false;
                }
                catch { }

                // Check "Obstacle" layer
                if (obstacleLayer != -1 && col.gameObject.layer == obstacleLayer) return false;

                // Check if hit object name indicates obstacle
                if (col.gameObject.name.ToLower().Contains("obstacle")) return false;

                // Ignore the valid floor beneath the feet
                if (col.bounds.max.y <= position.y + 0.1f) continue;

                // Any other solid body colliding with the capsule is an obstruction
                return false;
            }

            return true;
        }

        private GameObject GetSelectedOrRandomPrefab()
        {
            if (npcPrefabPool != null && npcPrefabPool.Count > 0)
            {
                var validPool = npcPrefabPool.FindAll(p => p != null);
                if (validPool.Count > 0)
                {
                    return validPool[Random.Range(0, validPool.Count)];
                }
            }
            return selectedNpcPrefab;
        }

        private void RecordNpcToLevelData(GameObject prefab, Vector3 position, Vector3 rotation)
        {
            if (targetLevelData == null)
            {
                Debug.LogError("[NPC Placer] Target LevelData is not assigned! Please assign a LevelData asset.");
                return;
            }

            Undo.RecordObject(targetLevelData, "Add Filler NPC");

            var entry = new NPCSpawnEntry
            {
                prefab = prefab,
                spawnPosition = position,
                spawnRotation = rotation,
                usePrefabTransform = false
            };

            targetLevelData.fillerNpcEntries.Add(entry);
            EditorUtility.SetDirty(targetLevelData);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=green>[NPC Placer]</color> Added '{prefab.name}' at {position} to LevelData '{targetLevelData.name}'. Total filler NPCs: {targetLevelData.fillerNpcEntries.Count}");
        }

        private void ExecuteAreaScatter()
        {
            if (targetLevelData == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a target LevelData first.", "OK");
                return;
            }

            if (selectedNpcPrefab == null && (npcPrefabPool == null || npcPrefabPool.Count == 0))
            {
                EditorUtility.DisplayDialog("Error", "Please assign at least one NPC prefab or populate the random pool.", "OK");
                return;
            }

            int placed = 0;
            for (int i = 0; i < maxScatterAttempts && placed < scatterCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * scatterRadius;
                Vector3 origin = new Vector3(scatterCenter.x + randomCircle.x, scatterCenter.y + 50f, scatterCenter.z + randomCircle.y);

                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 100f, surfaceMask))
                {
                    if (IsValidSurface(hit.collider, hit.normal) && HasObstacleClearance(hit.point, hit.collider))
                    {
                        GameObject prefabToSpawn = GetSelectedOrRandomPrefab();
                        if (prefabToSpawn != null)
                        {
                            float yRot = randomRotationEnabled ? Random.Range(randomYRotationMin, randomYRotationMax) : 0f;
                            RecordNpcToLevelData(prefabToSpawn, hit.point, new Vector3(0, yRot, 0));
                            placed++;
                        }
                    }
                }
            }

            EditorUtility.DisplayDialog("Scatter Complete", $"Successfully placed {placed}/{scatterCount} filler NPCs into '{targetLevelData.name}'.", "OK");
        }
    }
}
#endif
