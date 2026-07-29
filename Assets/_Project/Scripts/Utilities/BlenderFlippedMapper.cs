using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BlenderFlipMapper : EditorWindow
{
    private class MeshGroup
    {
        public Mesh OriginalMesh;
        public Mesh BlenderFlippedMesh;
        public List<MeshFilter> AffectedFilters = new List<MeshFilter>();
    }

    private Dictionary<Mesh, MeshGroup> groupedObjects = new Dictionary<Mesh, MeshGroup>();
    private Vector2 scrollPos;

    [MenuItem("Tools/Kontexto/Blender Flip Mapper")]
    public static void ShowWindow()
    {
        GetWindow<BlenderFlipMapper>("Blender Flip Mapper");
    }

    private void OnGUI()
    {
        GUILayout.Label("Map Blender Flipped Meshes to Negative Scales", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Scan Scene for Negative Scales", GUILayout.Height(30)))
        {
            ScanScene();
        }

        EditorGUILayout.Space();

        if (groupedObjects.Count == 0)
        {
            EditorGUILayout.HelpBox("No negative scales found or scene hasn't been scanned.", MessageType.Info);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (var kvp in groupedObjects)
        {
            MeshGroup group = kvp.Value;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Display Original Mesh info
            EditorGUILayout.LabelField($"Original Mesh: {group.OriginalMesh.name}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Found on {group.AffectedFilters.Count} objects in the scene.");

            // Drag and drop slot for your Blender asset
            group.BlenderFlippedMesh = (Mesh)EditorGUILayout.ObjectField(
                "Blender Flipped Mesh:", 
                group.BlenderFlippedMesh, 
                typeof(Mesh), 
                false
            );

            EditorGUILayout.Space();

            // Action button for this specific mesh type
            if (group.BlenderFlippedMesh != null)
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button($"Replace & Normalize {group.AffectedFilters.Count} Objects"))
                {
                    ProcessGroupSubstitution(group);
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUILayout.HelpBox("Drag the flipped mesh exported from Blender here to fix.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();
    }

    private void ScanScene()
    {
        groupedObjects.Clear();
        MeshFilter[] filters = FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);

        foreach (var filter in filters)
        {
            if (filter.sharedMesh == null) continue;

            Vector3 scale = filter.transform.localScale;
            // Check for negative scale components
            if (scale.x < 0 || scale.y < 0 || scale.z < 0)
            {
                Mesh meshKey = filter.sharedMesh;

                if (!groupedObjects.ContainsKey(meshKey))
                {
                    groupedObjects[meshKey] = new MeshGroup { OriginalMesh = meshKey };
                }

                groupedObjects[meshKey].AffectedFilters.Add(filter);
            }
        }
    }

    private void ProcessGroupSubstitution(MeshGroup group)
    {
        int count = 0;
        foreach (var filter in group.AffectedFilters)
        {
            if (filter == null) continue;

            Undo.RecordObject(filter.transform, "Normalize Scale via Blender Asset");
            Undo.RecordObject(filter, "Swap to Blender Flipped Mesh");

            // 1. Assign your pristine Blender mesh asset
            filter.sharedMesh = group.BlenderFlippedMesh;

            // 2. Absolute value the scale component to make it positive (1, 1, 1)
            Vector3 localScale = filter.transform.localScale;
            filter.transform.localScale = new Vector3(
                Mathf.Abs(localScale.x), 
                Mathf.Abs(localScale.y), 
                Mathf.Abs(localScale.z)
            );

            count++;
        }

        //Debug.Log($"Successfully swapped and normalized {count} instances of {group.OriginalMesh.name}!");
        ScanScene(); // Rescan to update UI
    }
}
