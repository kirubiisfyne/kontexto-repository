using Master.Scripts.DialogueSystem;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Master.Scripts
{
    [RequireComponent(typeof(Collider))]
    public class SceneGate : MonoBehaviour, IInteractable
    {
        public UnityEditor.SceneAsset sceneTo;
        public UnityEditor.SceneAsset sceneFrom;

        public bool canPlayerWarp = true;
        private bool isPlayerInRange = false;
        
        private void OnTriggerEnter(Collider other)
        {
            isPlayerInRange = true;
        }

        private void OnTriggerExit(Collider other)
        {
            isPlayerInRange = false;
        }

        public void Interact(GameObject player)
        {
            if (isPlayerInRange && canPlayerWarp)
            {
                GateManager.Instance.StartWarp(sceneTo.name);
                Debug.Log("Warp complete!");
            }
        }
    }
}