using UnityEngine;
using UnityEngine.Events;

namespace Master.Scripts
{
    public enum GateTriggerMode 
    { 
        Interact, 
        OnTriggerEnter 
    }

    [RequireComponent(typeof(Collider))]
    public class SceneGateInstance : MonoBehaviour, IInteractable
    {
        [Header("Scene Settings")]
        [Tooltip("The name of the scene this gate will load.")]
        public string sceneToName;

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
            if (isPlayerInRange && canPlayerWarp)
            {
                if (!string.IsNullOrEmpty(sceneToName))
                {
                    // Fire any custom hooks (like your Level Completion Hook!)
                    onWarpStart?.Invoke();

                    // Save player's current position so they return here later
                    Master.Scripts.SaveSystem.LevelLoader.Current?.SaveGame();
                    
                    SceneGateManager.Instance.StartWarp(sceneToName);
                    Debug.Log($"[SceneGate] Warping to {sceneToName}...");
                }
                else
                {
                    Debug.LogError("[SceneGate] Scene To Name is empty! Please assign a scene name in the Inspector.");
                }
            }
        }
    }
}