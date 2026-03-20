using UnityEngine;

namespace Master.Scripts
{
    [RequireComponent(typeof(Collider))]
    public class SceneGateInstance : MonoBehaviour, IInteractable
    {
        [Header("Scene Settings")]
        public string sceneToName;

        public bool canPlayerWarp = true;
        private bool isPlayerInRange = false;
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                isPlayerInRange = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                isPlayerInRange = false;
            }
        }

        public void Interact(GameObject playerGameObject)
        {
            if (isPlayerInRange && canPlayerWarp)
            {
                if (!string.IsNullOrEmpty(sceneToName))
                {
                    SceneGateManager.Instance.StartWarp(sceneToName);
                    Debug.Log($"Warping to {sceneToName}...");
                }
                else
                {
                    Debug.LogError("SceneGateInstance: Scene To Name is empty!");
                }
            }
        }
    }
}
