using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor window for snapping GameObjects to integer grid positions,
/// detecting positional duplicates and mesh collisions, and bulk cleanup.
/// </summary>
public class SnapToGridWindow : EditorWindow
{
    // ──────────────────────────────────────────────
    //  Settings
    // ──────────────────────────────────────────────

    private float positionThreshold = 0.01f;
    private float boundsThreshold   = 0.0f;
    private bool  mustMatchScale    = true;

    // ──────────────────────────────────────────────
    //  State
    // ──────────────────────────────────────────────

    private Vector2 scrollPos;

    // Same-position results
    private bool hasScanPosition = false;
    private List<OverlapGroup> positionGroups = new List<OverlapGroup>();
    private Dictionary<int, bool> posFoldouts = new Dictionary<int, bool>();

    // Colliding-bounds results
    private bool hasScanBounds = false;
    private List<OverlapGroup> boundsGroups = new List<OverlapGroup>();
    private Dictionary<int, bool> bndFoldouts = new Dictionary<int, bool>();

    /// <summary>
    /// A group of GameObjects that overlap by some criterion.
    /// </summary>
    private class OverlapGroup
    {
        public string label;
        public List<GameObject> objects = new List<GameObject>();
        public int DuplicateCount => Mathf.Max(0, objects.Count - 1);
    }

    // ──────────────────────────────────────────────
    //  Window Lifecycle
    // ──────────────────────────────────────────────

    [MenuItem("Tools/Snap to Grid Window")]
    private static void OpenWindow()
    {
        SnapToGridWindow window = GetWindow<SnapToGridWindow>("Snap to Grid");
        window.minSize = new Vector2(380, 400);
        window.Show();
    }

    private void OnSelectionChange() { Repaint(); }

    // ──────────────────────────────────────────────
    //  Main GUI
    // ──────────────────────────────────────────────

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawSnapSection();
        DrawSpacer();
        DrawSettingsSection();
        DrawSpacer();
        DrawPositionSection();
        DrawSpacer();
        DrawBoundsSection();

        EditorGUILayout.EndScrollView();
    }

    // ── Snap ─────────────────────────────────────

    private void DrawSnapSection()
    {
        EditorGUILayout.LabelField("Snap to Grid", EditorStyles.boldLabel);
        DrawSeparator();

        int count = Selection.gameObjects.Length;
        EditorGUILayout.LabelField("Selected:", $"{count} object(s)");

        EditorGUI.BeginDisabledGroup(count == 0);
        if (GUILayout.Button("Snap Selected to Grid", GUILayout.Height(26)))
            SnapSelection();
        EditorGUI.EndDisabledGroup();
    }

    // ── Settings ─────────────────────────────────

    private void DrawSettingsSection()
    {
        EditorGUILayout.LabelField("Detection Settings", EditorStyles.boldLabel);
        DrawSeparator();

        positionThreshold = EditorGUILayout.Slider("Position Threshold", positionThreshold, 0.001f, 1.0f);
        boundsThreshold   = EditorGUILayout.Slider("Bounds Threshold",   boundsThreshold,   0.0f,   1.0f);
        mustMatchScale    = EditorGUILayout.Toggle("Must Match Scale", mustMatchScale);

        EditorGUILayout.HelpBox(
            "Position: max distance between pivots to count as 'same position'.\n" +
            "Bounds: expand each renderer bound before checking intersection.\n" +
            "Match Scale: skip objects with different scales (e.g. mirrored pieces).",
            MessageType.None
        );
    }

    // ── Same Position ────────────────────────────

    private void DrawPositionSection()
    {
        EditorGUILayout.LabelField("Same Position", EditorStyles.boldLabel);
        DrawSeparator();

        int selCount = Selection.gameObjects.Length;

        // Action buttons row
        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(selCount < 2);
        if (GUILayout.Button("Scan", GUILayout.Height(26)))
            RunPositionScan();
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(!hasScanPosition || positionGroups.Count == 0);
        if (GUILayout.Button("Select All", GUILayout.Height(26)))
            SelectAllFlagged(positionGroups);
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(!hasScanPosition);
        if (GUILayout.Button("Clear", GUILayout.Height(26), GUILayout.Width(50)))
        {
            positionGroups.Clear();
            posFoldouts.Clear();
            hasScanPosition = false;
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        if (!hasScanPosition)
            return;

        PruneDestroyed(positionGroups);

        if (positionGroups.Count == 0)
        {
            EditorGUILayout.HelpBox("✓ No same-position groups found.", MessageType.Info);
            return;
        }

        int totalDupes = positionGroups.Sum(g => g.DuplicateCount);

        EditorGUILayout.HelpBox(
            $"{positionGroups.Count} group(s), {totalDupes} duplicate(s) to remove.\n" +
            "★ = kept object. Others are deleted.",
            MessageType.Warning
        );

        // Bulk delete
        EditorGUILayout.Space(2);
        GUI.backgroundColor = new Color(1f, 0.35f, 0.35f);
        if (GUILayout.Button($"DELETE ALL DUPLICATES ({totalDupes})", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                "Delete All Duplicates",
                $"Delete {totalDupes} duplicate(s) across {positionGroups.Count} group(s)?\n\nUndo with Ctrl+Z.",
                "Delete All", "Cancel"))
            {
                BulkDelete(positionGroups);
            }
        }
        GUI.backgroundColor = Color.white;

        // Group list
        EditorGUILayout.Space(4);
        DrawGroupList(positionGroups, posFoldouts, 0);
    }

    // ── Colliding Bounds ─────────────────────────

    private void DrawBoundsSection()
    {
        EditorGUILayout.LabelField("Colliding Bounds", EditorStyles.boldLabel);
        DrawSeparator();

        int selCount = Selection.gameObjects.Length;

        // Action buttons row
        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(selCount < 2);
        if (GUILayout.Button("Scan", GUILayout.Height(26)))
            RunBoundsScan();
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(!hasScanBounds || boundsGroups.Count == 0);
        if (GUILayout.Button("Select All", GUILayout.Height(26)))
            SelectAllFlagged(boundsGroups);
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(!hasScanBounds);
        if (GUILayout.Button("Clear", GUILayout.Height(26), GUILayout.Width(50)))
        {
            boundsGroups.Clear();
            bndFoldouts.Clear();
            hasScanBounds = false;
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        if (!hasScanBounds)
            return;

        PruneDestroyed(boundsGroups);

        if (boundsGroups.Count == 0)
        {
            EditorGUILayout.HelpBox("✓ No colliding bounds found.", MessageType.Info);
            return;
        }

        EditorGUILayout.HelpBox(
            $"{boundsGroups.Count} collision(s) found.\n" +
            "These objects have overlapping renderer bounds.",
            MessageType.Warning
        );

        // Group list
        EditorGUILayout.Space(4);
        DrawGroupList(boundsGroups, bndFoldouts, 50000);
    }

    // ── Shared Group List Drawing ────────────────

    private void DrawGroupList(List<OverlapGroup> groups, Dictionary<int, bool> foldouts, int idOffset)
    {
        for (int i = groups.Count - 1; i >= 0; i--)
        {
            OverlapGroup group = groups[i];
            if (group.objects.Count <= 1)
            {
                groups.RemoveAt(i);
                continue;
            }

            int foldId = i + idOffset;
            if (!foldouts.ContainsKey(foldId))
                foldouts[foldId] = false;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header
            EditorGUILayout.BeginHorizontal();

            foldouts[foldId] = EditorGUILayout.Foldout(
                foldouts[foldId],
                $"{group.label}  —  {group.objects.Count} obj(s)",
                true
            );

            if (GUILayout.Button("Select", GUILayout.Width(50)))
            {
                Selection.objects = group.objects.Where(o => o != null).Cast<Object>().ToArray();
            }

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button($"Delete ({group.DuplicateCount})", GUILayout.Width(80)))
            {
                DeleteGroupDuplicates(group);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // Expanded detail
            if (foldouts[foldId])
            {
                EditorGUI.indentLevel++;
                for (int j = group.objects.Count - 1; j >= 0; j--)
                {
                    GameObject obj = group.objects[j];
                    if (obj == null) { group.objects.RemoveAt(j); continue; }

                    EditorGUILayout.BeginHorizontal();

                    // Keeper marker
                    EditorGUILayout.LabelField(j == 0 ? "★" : " ", GUILayout.Width(16));

                    // Object field
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                    EditorGUI.EndDisabledGroup();

                    // Scale label
                    Vector3 s = obj.transform.localScale;
                    EditorGUILayout.LabelField($"S({s.x:F1},{s.y:F1},{s.z:F1})", GUILayout.Width(100));

                    // Select
                    if (GUILayout.Button("Sel", GUILayout.Width(32)))
                    {
                        Selection.activeGameObject = obj;
                        EditorGUIUtility.PingObject(obj);
                        if (SceneView.lastActiveSceneView != null)
                            SceneView.lastActiveSceneView.FrameSelected();
                    }

                    // Delete
                    EditorGUI.BeginDisabledGroup(group.objects.Count <= 1);
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                    if (GUILayout.Button("Del", GUILayout.Width(32)))
                    {
                        Undo.DestroyObjectImmediate(obj);
                        group.objects.RemoveAt(j);
                    }
                    GUI.backgroundColor = Color.white;
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
    }

    // ──────────────────────────────────────────────
    //  Scan Logic
    // ──────────────────────────────────────────────

    /// <summary>
    /// Groups selected objects by proximity of their transform positions.
    /// Optionally filters by matching scale.
    /// </summary>
    private void RunPositionScan()
    {
        positionGroups.Clear();
        posFoldouts.Clear();
        hasScanPosition = true;

        GameObject[] selected = Selection.gameObjects;
        if (selected.Length < 2) { Repaint(); return; }

        // Build groups via brute-force distance check
        bool[] assigned = new bool[selected.Length];

        for (int i = 0; i < selected.Length; i++)
        {
            if (assigned[i]) continue;

            List<GameObject> group = new List<GameObject> { selected[i] };
            assigned[i] = true;

            Vector3 posA = selected[i].transform.position;
            Vector3 scaleA = selected[i].transform.localScale;

            for (int j = i + 1; j < selected.Length; j++)
            {
                if (assigned[j]) continue;

                Vector3 posB = selected[j].transform.position;

                if (Vector3.Distance(posA, posB) > positionThreshold)
                    continue;

                if (mustMatchScale)
                {
                    Vector3 scaleB = selected[j].transform.localScale;
                    if (!ApproxEqual(scaleA, scaleB))
                        continue;
                }

                // Also require same mesh
                Mesh meshA = GetSharedMesh(selected[i]);
                Mesh meshB = GetSharedMesh(selected[j]);
                if (meshA == null || meshB == null || meshA != meshB)
                    continue;

                group.Add(selected[j]);
                assigned[j] = true;
            }

            if (group.Count >= 2)
            {
                Vector3 p = posA;
                string meshName = GetMeshName(selected[i]);
                positionGroups.Add(new OverlapGroup
                {
                    label = $"({p.x:F1}, {p.y:F1}, {p.z:F1})  \"{meshName}\"",
                    objects = group
                });
            }
        }

        // Sort by position
        positionGroups.Sort((a, b) => string.Compare(a.label, b.label, System.StringComparison.Ordinal));

        int totalDupes = positionGroups.Sum(g => g.DuplicateCount);
        //Debug.Log($"[Same Position] {positionGroups.Count} group(s), {totalDupes} duplicate(s) found.");
        Repaint();
    }

    /// <summary>
    /// Finds pairs of objects whose renderer bounds intersect,
    /// expanded by the bounds threshold.
    /// </summary>
    private void RunBoundsScan()
    {
        boundsGroups.Clear();
        bndFoldouts.Clear();
        hasScanBounds = true;

        GameObject[] selected = Selection.gameObjects;
        if (selected.Length < 2) { Repaint(); return; }

        // Collect renderer entries
        List<BoundsEntry> entries = new List<BoundsEntry>();
        foreach (GameObject go in selected)
        {
            Renderer rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                Bounds b = rend.bounds;
                b.Expand(boundsThreshold);
                entries.Add(new BoundsEntry { gameObject = go, bounds = b });
            }
        }

        // Track which objects are already grouped to avoid duplicate entries
        // Use a set of pair keys to avoid listing the same pair twice
        HashSet<string> seenPairs = new HashSet<string>();

        for (int i = 0; i < entries.Count; i++)
        {
            for (int j = i + 1; j < entries.Count; j++)
            {
                // Skip objects at the exact same position (handled by position scan)
                if (Vector3.Distance(
                    entries[i].gameObject.transform.position,
                    entries[j].gameObject.transform.position) <= positionThreshold)
                    continue;

                if (!entries[i].bounds.Intersects(entries[j].bounds))
                    continue;

                string pairKey = PairKey(entries[i].gameObject, entries[j].gameObject);
                if (seenPairs.Contains(pairKey))
                    continue;
                seenPairs.Add(pairKey);

                string nameA = entries[i].gameObject.name;
                string nameB = entries[j].gameObject.name;

                boundsGroups.Add(new OverlapGroup
                {
                    label = $"\"{nameA}\" ∩ \"{nameB}\"",
                    objects = new List<GameObject>
                    {
                        entries[i].gameObject,
                        entries[j].gameObject
                    }
                });
            }
        }

        //Debug.Log($"[Colliding Bounds] {boundsGroups.Count} collision(s) found.");
        Repaint();
    }

    // ──────────────────────────────────────────────
    //  Actions
    // ──────────────────────────────────────────────

    [MenuItem("Tools/Snap to Grid %#g")]
    private static void SnapSelection()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected.Length == 0)
        {
            //Debug.LogWarning("[Snap to Grid] No objects selected.");
            return;
        }

        int count = 0;
        foreach (GameObject go in selected)
        {
            Undo.RecordObject(go.transform, "Snap to Grid");
            Vector3 pos = go.transform.position;
            go.transform.position = new Vector3(
                Mathf.Round(pos.x),
                Mathf.Round(pos.y),
                Mathf.Round(pos.z)
            );
            count++;
        }
        //Debug.Log($"[Snap to Grid] Snapped {count} object(s).");
    }

    private void SelectAllFlagged(List<OverlapGroup> groups)
    {
        HashSet<GameObject> all = new HashSet<GameObject>();
        foreach (OverlapGroup g in groups)
            foreach (GameObject o in g.objects)
                if (o != null) all.Add(o);

        Selection.objects = all.Cast<Object>().ToArray();
        //Debug.Log($"[Select All] Selected {all.Count} object(s).");
    }

    private void BulkDelete(List<OverlapGroup> groups)
    {
        Undo.SetCurrentGroupName("Delete All Duplicates");
        int deleted = 0;

        foreach (OverlapGroup group in groups)
        {
            for (int i = group.objects.Count - 1; i >= 1; i--)
            {
                if (group.objects[i] != null)
                {
                    Undo.DestroyObjectImmediate(group.objects[i]);
                    deleted++;
                }
                group.objects.RemoveAt(i);
            }
        }

        groups.RemoveAll(g => g.objects.Count <= 1);
        //Debug.Log($"[Delete All] Deleted {deleted} object(s).");
        Repaint();
    }

    private void DeleteGroupDuplicates(OverlapGroup group)
    {
        Undo.SetCurrentGroupName("Delete Group Duplicates");
        for (int i = group.objects.Count - 1; i >= 1; i--)
        {
            if (group.objects[i] != null)
                Undo.DestroyObjectImmediate(group.objects[i]);
            group.objects.RemoveAt(i);
        }
        Repaint();
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────

    private struct BoundsEntry
    {
        public GameObject gameObject;
        public Bounds bounds;
    }

    private static Mesh GetSharedMesh(GameObject go)
    {
        MeshFilter mf = go.GetComponent<MeshFilter>();
        return mf != null ? mf.sharedMesh : null;
    }

    private static string GetMeshName(GameObject go)
    {
        Mesh m = GetSharedMesh(go);
        return m != null ? m.name : "(no mesh)";
    }

    private static bool ApproxEqual(Vector3 a, Vector3 b)
    {
        return Mathf.Approximately(a.x, b.x)
            && Mathf.Approximately(a.y, b.y)
            && Mathf.Approximately(a.z, b.z);
    }

    private static string PairKey(GameObject a, GameObject b)
    {
        int idA = a.GetInstanceID();
        int idB = b.GetInstanceID();
        return idA < idB ? $"{idA}_{idB}" : $"{idB}_{idA}";
    }

    private void PruneDestroyed(List<OverlapGroup> groups)
    {
        for (int i = groups.Count - 1; i >= 0; i--)
        {
            groups[i].objects.RemoveAll(o => o == null);
            if (groups[i].objects.Count <= 1)
                groups.RemoveAt(i);
        }
    }

    private void DrawSeparator()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        EditorGUILayout.Space(2);
    }

    private void DrawSpacer() { EditorGUILayout.Space(8); }
}
