using UnityEngine;
using UnityEditor;
using Master.Scripts;
using Master.Scripts.SaveSystem;
using System.IO;

namespace Master.Scripts.Editor
{
    /// <summary>
    /// Custom Inspector for LevelLoader that visualizes the save JSON,
    /// allows quick day/level switching in Editor, and tracks active progression.
    /// </summary>
    [CustomEditor(typeof(LevelLoader))]
    public class LevelLoaderEditor : UnityEditor.Editor
    {
        private PlayerData cachedData;
        private bool showAllLevels = false;
        private bool showDatabaseLevels = true;

        private GUIStyle headerStyle;
        private GUIStyle completedStyle;
        private GUIStyle incompleteStyle;
        private GUIStyle readyStyle;
        private GUIStyle taskIdStyle;
        private bool stylesInitialized = false;

        private void InitStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };

            completedStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.3f, 0.85f, 0.4f) },
                fontStyle = FontStyle.Bold
            };

            incompleteStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.95f, 0.6f, 0.2f) },
                fontStyle = FontStyle.Bold
            };

            readyStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.3f, 0.8f, 0.95f) },
                fontStyle = FontStyle.Bold
            };

            taskIdStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                padding = new RectOffset(16, 0, 0, 0)
            };

            stylesInitialized = true;
        }

        public override void OnInspectorGUI()
        {
            // Draw the default Inspector fields first
            DrawDefaultInspector();

            InitStyles();

            var loader = (LevelLoader)target;

            EditorGUILayout.Space(16);

            // ── Level Database & Quick Testing ──
            if (loader.levelDatabase != null && loader.levelDatabase.Count > 0)
            {
                EditorGUILayout.LabelField("Level Database & Day Controls", headerStyle);
                DrawSeparator();

                showDatabaseLevels = EditorGUILayout.Foldout(showDatabaseLevels, $"Database Days ({loader.levelDatabase.Count})", true);
                if (showDatabaseLevels)
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < loader.levelDatabase.Count; i++)
                    {
                        var sequence = loader.levelDatabase.GetLevelByIndex(i);
                        if (sequence == null) continue;

                        var level = sequence.levelData;
                        bool isCurrent = level != null && loader.ActiveLevelData == level;
                        string sceneId = level != null ? level.sceneId : "no LevelData";
                        string cutsceneInfo = $" [In: {sequence.introCutscenePrefabs.Count}, Out: {sequence.outroCutscenePrefabs.Count}]";

                        EditorGUILayout.BeginHorizontal();
                        string label = isCurrent ? $"► [{i}] {sequence.name} ({sceneId}){cutsceneInfo}" : $"  [{i}] {sequence.name} ({sceneId}){cutsceneInfo}";
                        EditorGUILayout.LabelField(label, isCurrent ? EditorStyles.boldLabel : EditorStyles.label);

                        if (Application.isPlaying)
                        {
                            if (GUILayout.Button("Load This Day", GUILayout.Width(110)))
                            {
                                if (level != null)
                                {
                                    loader.LoadLevel(level);
                                }
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("Set As Override", GUILayout.Width(110)))
                            {
                                Undo.RecordObject(loader, "Set Editor Override Level");
                                loader.editorOverrideSequence = sequence;
                                loader.editorOverrideLevel = level;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }

                if (Application.isPlaying)
                {
                    EditorGUILayout.Space(6);
                    if (GUILayout.Button("Advance To Next Level", GUILayout.Height(26)))
                    {
                        loader.AdvanceToNextLevel();
                    }
                }

                EditorGUILayout.Space(12);
            }

            // ── Save Data Visualizer ──
            EditorGUILayout.LabelField("Save Data Visualizer", headerStyle);
            DrawSeparator();

            // Refresh / Load button
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Height(24)))
            {
                cachedData = null; // Force re-read
            }

            // Wipe save button (red)
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.35f, 0.35f);
            if (GUILayout.Button("Wipe Save Data", GUILayout.Height(24)))
            {
                if (EditorUtility.DisplayDialog(
                    "Wipe Save Data",
                    "This will DELETE the entire save file (player_save.json).\n\nAll level and task progress will be lost.\n\nAre you sure?",
                    "Wipe It",
                    "Cancel"))
                {
                    SaveManager.DeleteSave();
                    cachedData = null;
                }
            }
            GUI.backgroundColor = originalColor;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // Load save data
            if (cachedData == null)
            {
                cachedData = SaveManager.Load();
            }

            if (cachedData == null || cachedData.levels.Count == 0)
            {
                EditorGUILayout.HelpBox("No save data found. Play the game to generate data.", MessageType.Info);
                return;
            }

            // Show current scene info
            EditorGUILayout.LabelField("Current Scene", EditorStyles.miniBoldLabel);
            string currentScene = loader.ActiveLevelData != null ? loader.ActiveLevelData.sceneId : "(no LevelData active)";
            EditorGUILayout.LabelField("  Active Level ID:", currentScene);
            EditorGUILayout.LabelField("  Save → Continue Scene:", cachedData.currentScene ?? "(not set)");

            EditorGUILayout.Space(8);

            // ── Current Scene Progress ──
            if (loader.ActiveLevelData != null)
            {
                LevelProgress currentLevel = null;
                foreach (var lp in cachedData.levels)
                {
                    if (lp.sceneId == loader.ActiveLevelData.sceneId)
                    {
                        currentLevel = lp;
                        break;
                    }
                }

                if (currentLevel != null)
                {
                    DrawLevelProgress(currentLevel, true, loader);
                }
                else
                {
                    EditorGUILayout.HelpBox($"No save record for '{loader.ActiveLevelData.sceneId}' yet.", MessageType.None);
                }
            }

            EditorGUILayout.Space(8);

            // ── All Levels (collapsible) ──
            showAllLevels = EditorGUILayout.Foldout(showAllLevels, $"All Saved Levels ({cachedData.levels.Count})", true);
            if (showAllLevels)
            {
                EditorGUI.indentLevel++;
                foreach (var level in cachedData.levels)
                {
                    bool isCurrent = loader.ActiveLevelData != null && level.sceneId == loader.ActiveLevelData.sceneId;
                    DrawLevelProgress(level, isCurrent, loader);
                    EditorGUILayout.Space(4);
                }
                EditorGUI.indentLevel--;
            }

            // Auto-refresh during play mode
            if (Application.isPlaying)
            {
                cachedData = null; // Re-read next frame
                Repaint();
            }
        }

        private void DrawLevelProgress(LevelProgress level, bool highlight, LevelLoader loader)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header row
            EditorGUILayout.BeginHorizontal();
            string label = highlight ? $"► {level.sceneId}" : level.sceneId;
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            // Three-way status: Completed > Ready for Completion > In Progress
            string statusText;
            GUIStyle statusStyle;

            if (level.isCompleted)
            {
                statusText = "✓ COMPLETED";
                statusStyle = completedStyle;
            }
            else if (highlight && Application.isPlaying && loader != null && loader.AreAllTasksCompleted())
            {
                statusText = "★ READY FOR COMPLETION";
                statusStyle = readyStyle;
            }
            else
            {
                statusText = "○ IN PROGRESS";
                statusStyle = incompleteStyle;
            }

            EditorGUILayout.LabelField(statusText, statusStyle, GUILayout.Width(180));
            EditorGUILayout.EndHorizontal();

            // Completed tasks
            if (level.completedTaskIds.Count > 0)
            {
                EditorGUILayout.LabelField($"  Completed Tasks ({level.completedTaskIds.Count}):", EditorStyles.miniLabel);
                foreach (var taskId in level.completedTaskIds)
                {
                    EditorGUILayout.LabelField($"    ✓  {taskId}", taskIdStyle);
                }
            }
            else
            {
                EditorGUILayout.LabelField("  No tasks completed yet.", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }
    }
}
