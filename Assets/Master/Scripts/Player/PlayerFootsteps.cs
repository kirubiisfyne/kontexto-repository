using UnityEngine;
using Master.Scripts;

namespace Master.Scripts.Player
{
    public class PlayerFootsteps : MonoBehaviour
    {
        [Header("AudioManager Mappings")]
        [Tooltip("The exact string name you gave the terrain SFX in AudioManager")]
        public string terrainStepName = "StepTerrain";
        [Tooltip("The exact string name you gave the floor SFX in AudioManager")]
        public string floorStepName = "StepFloor";
        
        [Header("Settings")]
        [Range(0f, 1f)] public float footstepVolume = 0.4f;
        public float walkStepInterval = 0.5f; 
        public float sprintStepInterval = 0.3f; 
        
        private CharacterController controller;
        private PlayerController playerController;
        private float stepTimer;

        private void Start()
        {
            controller = GetComponent<CharacterController>();
            playerController = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (controller == null) return;

            float currentInterval = (playerController != null && playerController.IsRunning) 
                                    ? sprintStepInterval 
                                    : walkStepInterval;

            // Only count as moving if the player is actively pressing WASD keys (ignores gravity & sliding)
            bool isMoving = playerController != null && playerController.inputDirection.sqrMagnitude > 0.01f;

            // Run timer if grounded and actually pressing keys
            if (controller.isGrounded && isMoving)
            {
                stepTimer -= Time.deltaTime;
                
                if (stepTimer <= 0f)
                {
                    PlayFootstepSound();
                    stepTimer = currentInterval;
                }
            }
            else
            {
                stepTimer = 0f; 
            }
        }

        private void PlayFootstepSound()
        {
            // Safety check to ensure AudioManager exists
            if (AudioManager.Instance == null) return;

            // Shoot a raycast down to detect the floor type
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.5f))
            {
                // Pass 'true' to trigger the random pitch shifting in your AudioManager!
                if (hit.collider is TerrainCollider)
                {
                    AudioManager.Instance.PlaySFX(terrainStepName, true, 0.85f, 1.15f, footstepVolume);
                }
                else
                {
                    AudioManager.Instance.PlaySFX(floorStepName, true, 0.85f, 1.15f, footstepVolume);
                }
            }
        }
    }
}
