using UnityEngine;
using UnityEditor;
using Master.Scripts.SaveSystem;

namespace Master.Scripts.Editor
{
    public class SaveManagerWindow : EditorWindow
    {
        private PlayerData currentData;
        private Vector2 scrollPos;

        [MenuItem("Tools/Save Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<SaveManagerWindow>("Save Manager");
            window.Show();
        }

        private void OnEnable()
        {
            RefreshData();
        }

        private void RefreshData()
        {
            currentData = SaveManager.Load();
            Repaint();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("Player Save Data Management", EditorStyles.boldLabel);

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Data", GUILayout.Height(30)))
            {
                RefreshData();
            }

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Delete Save File (Reset Progress)", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Delete Save", "Are you sure you want to completely wipe the player's save data? This cannot be undone.", "Yes, Wipe It", "Cancel"))
                {
                    SaveManager.DeleteSave();
                    RefreshData();
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);

            if (currentData == null)
            {
                EditorGUILayout.HelpBox("No save data found or error loading. The save file might not exist yet.", MessageType.Info);
                return;
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, "box");

            GUILayout.Label($"Last Active Scene: {currentData.currentScene}", EditorStyles.boldLabel);
            
            if (currentData.HasSavedPosition())
            {
                var (pos, rot) = currentData.GetPlayerTransform();
                GUILayout.Label($"Saved Position: {pos}");
                GUILayout.Label($"Saved Rotation: {rot}");
            }
            else
            {
                GUILayout.Label("Saved Position: (None)");
            }

            GUILayout.Space(15);
            GUILayout.Label("Level Progress & Tasks", EditorStyles.boldLabel);

            if (currentData.levels == null || currentData.levels.Count == 0)
            {
                GUILayout.Label("No level progress recorded yet.");
            }
            else
            {
                foreach (var level in currentData.levels)
                {
                    EditorGUILayout.BeginVertical("helpbox");
                    
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"Scene: {level.sceneId}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (level.isCompleted)
                    {
                        GUI.contentColor = Color.green;
                        GUILayout.Label("[LEVEL COMPLETED]");
                        GUI.contentColor = Color.white;
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    GUILayout.Space(5);
                    
                    GUILayout.Label("Completed Tasks:", EditorStyles.miniBoldLabel);
                    if (level.completedTaskIds != null && level.completedTaskIds.Count > 0)
                    {
                        foreach (var task in level.completedTaskIds)
                        {
                            GUILayout.Label($" \u2713 {task}"); // Checkmark
                        }
                    }
                    else
                    {
                        GUILayout.Label("    (None)");
                    }

                    GUILayout.Space(5);

                    GUILayout.Label("Active Tasks:", EditorStyles.miniBoldLabel);
                    if (level.activeTaskIds != null && level.activeTaskIds.Count > 0)
                    {
                        foreach (var task in level.activeTaskIds)
                        {
                            GUILayout.Label($" \u2022 {task}"); // Bullet point
                        }
                    }
                    else
                    {
                        GUILayout.Label("    (None)");
                    }

                    EditorGUILayout.EndVertical();
                    GUILayout.Space(5);
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
