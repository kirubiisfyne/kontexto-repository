using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Master.Scripts
{
    public class GateManager : MonoBehaviour
    {
        public static GateManager Instance { get; private set; }
        
        public GameObject player;
        public string lastSceneString;
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

        public void StartWarp(string sceneTo)
        {
            StartCoroutine(Warp(sceneTo));
        }
        
        private IEnumerator Warp(string sceneTo)
        {
            // Wait for the animation to finish.
            yield return (TransitionManager.Instance.PlayTransitionAndWait("anim_TransitionOut"));
            
            lastSceneString = SceneManager.GetActiveScene().name;
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneTo);
            
            while (operation is { isDone: false })
            {
                yield return null; 
            }
            
            yield return new WaitForEndOfFrame();

            if (GameObject.FindGameObjectWithTag("Player") != null)
            {
                SpawnPlayerToGate();
                Debug.Log("Player Warped!");
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            
            // Look for the Transition GO again.
            TransitionManager.Instance.FindTransitionObject();
        }

        private void SpawnPlayerToGate()
        {
            Vector3 spawnPosition = Vector3.zero;
            Vector3 spawnRotation = Vector3.zero;
            // Look for the gate with the corresponding gate.
            GameObject[] gates = GameObject.FindGameObjectsWithTag("Gate");

            foreach (GameObject gate in gates)
            {
                if (gate == enabled)
                {
                    if (gate.GetComponent<SceneGate>().sceneFrom.name == lastSceneString)
                    {
                        spawnPosition = gate.transform.position;
                        spawnRotation = gate.transform.rotation.eulerAngles;
                    }
                }
            }
            // Disable cc.
            player = GameObject.FindGameObjectWithTag("Player");
            player.GetComponent<CharacterController>().enabled = false;
            // Move Player.
            player.transform.position = spawnPosition;
            player.transform.rotation = Quaternion.Euler(spawnRotation);
            // Enable cc.
            player.GetComponent<CharacterController>().enabled = true;
        }
    }
}
