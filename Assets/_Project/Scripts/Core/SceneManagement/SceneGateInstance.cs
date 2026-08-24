using UnityEngine;
using UnityEngine.Events;

namespace Master.Scripts
{
    public enum GateTriggerMode 
    { 
        Interact, 
        OnTriggerEnter 
    }

    public enum GateFunction
    {
        SceneWarp,
        LevelAdvance
    }

    [RequireComponent(typeof(Collider))]
    public class SceneGateInstance : MonoBehaviour, IInteractable
    {
        [Header("Gate Functionality")]
        [Tooltip("SceneWarp loads another scene (e.g., scn_editor). LevelAdvance transitions to the next day via LevelLoader without needing a scene name.")]
        public GateFunction gateFunction = GateFunction.SceneWarp;

        [Header("Scene Settings")]
        [Tooltip("The name of the scene this gate will load (required for SceneWarp mode).")]
        public string sceneToName;

        [Tooltip("The unique ID of this gate (e.g. 'CampusFront').")]
        public string gateId;

        [Tooltip("The ID of the gate to spawn at in the target scene (optional).")]
        public string targetGateId;

        [Tooltip("Check this if the player can warp right now.")]
        public bool canPlayerWarp = true;
        
        [Header("Trigger Settings")]
        [Tooltip("How does the player trigger this gate? By interacting, or just walking into it?")]
        public GateTriggerMode triggerMode = GateTriggerMode.Interact;

        [Header("Events")]
        [Tooltip("Fired right before the warp happens. Useful for hooking into LevelCompletionHook.")]
        public UnityEvent onWarpStart;

        private bool isPlayerInRange = false;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerInRange = true;
                
                if (triggerMode == GateTriggerMode.OnTriggerEnter)
                {
                    TryWarp();
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerInRange = false;
            }
        }

        public void Interact()
        {
            if (triggerMode == GateTriggerMode.Interact)
            {
                TryWarp();
            }
        }

        private void TryWarp()
        {
            if (!isPlayerInRange || !canPlayerWarp) return;

            // 1. Level Advance Mode (in-place progression without external scene requirement)
            if (gateFunction == GateFunction.LevelAdvance)
            {
                onWarpStart?.Invoke();

                // Only call AdvanceToNextLevel if LevelCompletionHook hasn't already handled it via onWarpStart
                var completionHook = GetComponent<SaveSystem.LevelCompletionHook>();
                if (completionHook == null)
                {
                    if (SaveSystem.LevelLoader.Current != null && SaveSystem.LevelLoader.Current.AreAllTasksCompleted())
                    {
                        SaveSystem.LevelLoader.Current.AdvanceToNextLevel();
                    }
                    else
                    {
                        Debug.LogWarning("[SceneGate] Cannot advance: Tasks for this day are not yet completed.");
                    }
                }
                return;
            }

            // 2. Standard Cross-Scene Warp Mode (e.g., Computer -> scn_editor)
            if (!string.IsNullOrEmpty(sceneToName))
            {
                // Dynamically resolve active task's document data if transitioning to a document editor context
                var activeTasks = FindObjectsByType<Master.Scripts.TaskSystem.HostTaskManager>(FindObjectsSortMode.None);
                foreach (var manager in activeTasks)
                {
                    if (manager.status == Master.Scripts.TaskSystem.TaskStatus.Active && manager.task != null && manager.task.documentData != null)
                    {
                        if (Master.Scripts.GameManager.Instance != null)
                        {
                            Master.Scripts.GameManager.Instance.activeDocumentData = manager.task.documentData;
                        }
                        break;
                    }
                }

                // Fire any custom hooks (like your Level Completion Hook!)
                onWarpStart?.Invoke();

                // Save player's current position so they return here later
                Master.Scripts.SaveSystem.LevelLoader.Current?.SaveGame();
                
                SceneGateManager.Instance.StartWarp(sceneToName, targetGateId);
            }
            else
            {
                Debug.LogError("[SceneGate] Scene To Name is empty! Please assign a scene name in the Inspector for SceneWarp mode.");
            }
        }
    }
}