using UnityEngine;
using UnityEditor;
using Master.Scripts;
using Master.Scripts.SaveSystem;
using System.IO;

namespace Master.Scripts.Editor
{
    /// <summary>
    /// Custom Inspector for LevelLoader that visualizes the save JSON
    /// for the current scene in real-time and provides a restart button.
    /// </summary>
    [CustomEditor(typeof(LevelLoader))]
    public class LevelLoaderEditor : UnityEditor.Editor
    {
        private PlayerData cachedData;
        private bool showAllLevels = false;

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
                    Debug.Log("LevelLoaderEditor: Save data wiped.");
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
            string currentScene = loader.levelData != null ? loader.levelData.sceneId : "(no LevelData assigned)";
            EditorGUILayout.LabelField("  Scene ID:", currentScene);
            EditorGUILayout.LabelField("  Save → Continue Scene:", cachedData.currentScene ?? "(not set)");

            EditorGUILayout.Space(8);

            // ── Current Scene Progress ──
            if (loader.levelData != null)
            {
                LevelProgress currentLevel = null;
                foreach (var lp in cachedData.levels)
                {
                    if (lp.sceneId == loader.levelData.sceneId)
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
                    EditorGUILayout.HelpBox($"No save record for '{loader.levelData.sceneId}' yet.", MessageType.None);
                }
            }

            EditorGUILayout.Space(8);

            // ── All Levels (collapsible) ──
            showAllLevels = EditorGUILayout.Foldout(showAllLevels, $"All Levels ({cachedData.levels.Count})", true);
            if (showAllLevels)
            {
                EditorGUI.indentLevel++;
                foreach (var level in cachedData.levels)
                {
                    bool isCurrent = loader.levelData != null && level.sceneId == loader.levelData.sceneId;
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
