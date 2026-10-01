#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Master.Scripts.NPC.Editor
{
    [CustomEditor(typeof(NPCRoamingAgent))]
    public class NPCRoamingAgentEditor : UnityEditor.Editor
    {
        private GUIStyle stateHeaderStyle;
        private GUIStyle stateLabelStyle;
        private bool stylesInitialized;

        private void InitStyles()
        {
            if (stylesInitialized) return;

            stateHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };

            stateLabelStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            stylesInitialized = true;
        }

        public override void OnInspectorGUI()
        {
            InitStyles();

            NPCRoamingAgent agent = (NPCRoamingAgent)target;

            // Runtime Status Box
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Live Runtime Diagnostics", stateHeaderStyle);

                Color originalBg = GUI.backgroundColor;
                switch (agent.CurrentState)
                {
                    case NPCRoamingAgent.RoamingState.Idle:
                        GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
                        break;
                    case NPCRoamingAgent.RoamingState.Wander:
                        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
                        break;
                    case NPCRoamingAgent.RoamingState.Interacting:
                        GUI.backgroundColor = new Color(1f, 0.8f, 0.4f);
                        break;
                }

                EditorGUILayout.LabelField($"State: {agent.CurrentState.ToString().ToUpper()}", stateLabelStyle, GUILayout.Height(26));
                GUI.backgroundColor = originalBg;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Anchor Position:", agent.AnchorPosition.ToString("F2"));
                EditorGUILayout.LabelField("Leash Radius:", $"{agent.LeashRadius:F1} m");

                if (agent.Agent != null && agent.Agent.isOnNavMesh)
                {
                    EditorGUILayout.LabelField("Agent Speed:", $"{agent.Agent.velocity.magnitude:F2} m/s");
                    EditorGUILayout.LabelField("Has Path:", agent.Agent.hasPath ? "Yes" : "No");
                    if (agent.Agent.hasPath)
                    {
                        EditorGUILayout.LabelField("Destination:", agent.Agent.destination.ToString("F2"));
                        EditorGUILayout.LabelField("Remaining Distance:", $"{agent.Agent.remainingDistance:F2} m");
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("NavMeshAgent is not currently placed on an active NavMesh.", MessageType.Warning);
                }
                EditorGUILayout.EndVertical();

                // Interactive Test Controls
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Runtime Test Controls", stateHeaderStyle);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Start Focus (Player)", GUILayout.Height(24)))
                {
                    agent.StartFocus();
                }

                if (GUILayout.Button("End Focus", GUILayout.Height(24)))
                {
                    agent.EndFocus();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Force Wander"))
                {
                    agent.ForceWander();
                }

                if (GUILayout.Button("Force Idle"))
                {
                    agent.ForceIdle();
                }

                if (GUILayout.Button("Reset to Anchor"))
                {
                    agent.ResetToAnchor();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(6);
            }

            // Draw default serialized fields
            DrawDefaultInspector();

            // Edit-mode helper buttons
            if (!Application.isPlaying)
            {
                EditorGUILayout.Space(6);
                if (GUILayout.Button("Snap Anchor to Current Position", GUILayout.Height(24)))
                {
                    Undo.RecordObject(agent, "Set NPC Anchor Position");
                    agent.SetAnchor(agent.transform.position);
                    EditorUtility.SetDirty(agent);
                }
            }
        }
    }
}
#endif
