using UnityEngine;
using UnityEditor;

namespace Master.Scripts
{
    [RequireComponent(typeof(Collider))]
    public class SceneGateInstance : MonoBehaviour, IInteractable
    {
        public SceneAsset sceneTo;
        public SceneAsset sceneFrom;

        public bool canPlayerWarp = true;
        private bool isPlayerInRange = false;
        
        private GameObject player;

        public void Interact(GameObject playerGameObject)
        {
            player = playerGameObject;
            
            if (isPlayerInRange && canPlayerWarp)
            {
                SceneGateManager.Instance.StartWarp(sceneTo.name);
                Debug.Log("Warp complete!");
            }
        }
    }
}