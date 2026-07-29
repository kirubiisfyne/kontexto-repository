using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Master.Scripts
{
    public class SceneGateManager : MonoBehaviour
    {
        public static SceneGateManager Instance { get; private set; }
        
        [HideInInspector] public string lastSceneString;
        [HideInInspector] public string targetGateId;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartWarp(string sceneTo, string targetGateId = "")
        {
            if (string.IsNullOrEmpty(sceneTo))
            {
                //Debug.LogWarning("[SceneGateManager] StartWarp was called with a null or empty scene string. If you are playing the UI scene directly, this is expected. Aborting warp to prevent crash.");
                return;
            }
            this.targetGateId = targetGateId;
            StartCoroutine(WarpRoutine(sceneTo));
        }
        
        private IEnumerator WarpRoutine(string sceneTo)
        {
            // Play fade out transition and wait for it to complete
            if (TransitionManager.Instance != null)
            {
                yield return TransitionManager.Instance.PlayTransitionAndWait("transition");
            }
            
            lastSceneString = SceneManager.GetActiveScene().name;
            
            // Start the async load AFTER the screen is black to prevent stutter
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneTo);
            if (operation == null)
            {
                //Debug.LogError($"[SceneGateManager] Failed to load scene '{sceneTo}'. Please ensure it is added to the Build Settings!");
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null; 
            }
            
            // Allow one frame for objects in the new scene to fully awaken
            yield return new WaitForEndOfFrame();

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                SpawnPlayerToGate(playerObj);
                //Debug.Log($"[SceneGateManager] Player warped to {sceneTo}");
            }
            else
            {
                // If there's no player (like in the Main Menu), free the cursor
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void SpawnPlayerToGate(GameObject playerObj)
        {
            if (string.IsNullOrEmpty(targetGateId))
            {
                return; // Do not move player if no target gate was specified
            }

            Vector3 spawnPosition = Vector3.zero;
            Vector3 spawnRotation = Vector3.zero;
            bool gateFound = false;

            // Find the corresponding return gate in the new scene
            GameObject[] gates = GameObject.FindGameObjectsWithTag("Gate");
            foreach (GameObject gate in gates)
            {
                SceneGateInstance gateInstance = gate.GetComponent<SceneGateInstance>();
                if (gateInstance != null && gateInstance.enabled && gateInstance.gateId == targetGateId)
                {
                    spawnPosition = gate.transform.position;
                    spawnRotation = gate.transform.rotation.eulerAngles;
                    gateFound = true;
                    break; // Stop searching once we find the correct gate
                }
            }

            if (!gateFound) return;

            // Disable CharacterController before teleporting to prevent physics jitter/snapback
            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            
            playerObj.transform.position = spawnPosition;
            playerObj.transform.rotation = Quaternion.Euler(spawnRotation);
            
            if (cc != null) cc.enabled = true;
        }
    }
}