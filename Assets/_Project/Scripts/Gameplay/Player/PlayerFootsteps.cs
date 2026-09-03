using System;
using UnityEngine;

namespace Master.Scripts.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerFootsteps : MonoBehaviour
    {
        [Header("Audio Source")]
        [Tooltip("AudioSource used to play the footsteps. If unassigned, one will be fetched or created automatically.")]
        [SerializeField] private AudioSource audioSource;

        [Header("Terrain Surface SFX List")]
        [Tooltip("Audio clips for walking on terrain/dirt/grass. Supports 1 or multiple variations.")]
        [SerializeField] private AudioClip[] terrainSFX = Array.Empty<AudioClip>();

        [Header("Floor Surface SFX List")]
        [Tooltip("Audio clips for walking on solid floors/stone/wood. Supports 1 or multiple variations.")]
        [SerializeField] private AudioClip[] floorSFX = Array.Empty<AudioClip>();

        [Header("Audio Variation Settings")]
        [Range(0f, 1f)] [SerializeField] private float footstepVolume = 0.4f;
        [Tooltip("Volume jitter (+/-) applied per step (e.g., 0.04 = ±4%).")]
        [Range(0f, 0.2f)] [SerializeField] private float volumeJitter = 0.04f;

        [Range(0.5f, 1.5f)] [SerializeField] private float basePitch = 1.0f;
        [Tooltip("Pitch jitter (+/-) applied per step (e.g., 0.05 = ±5%).")]
        [Range(0f, 0.2f)] [SerializeField] private float pitchJitter = 0.05f;

        [Header("Step Timing")]
        public float walkStepInterval = 0.5f; 
        public float sprintStepInterval = 0.3f; 

        private CharacterController controller;
        private PlayerController playerController;
        private float stepTimer;

        // Tracks previous clip indices independently to prevent back-to-back repeats
        private int _lastTerrainIndex = -1;
        private int _lastFloorIndex = -1;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            playerController = GetComponent<PlayerController>();

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.spatialBlend = 1f; // 3D spatialized
                    audioSource.playOnAwake = false;
                }
            }
        }

        private void Update()
        {
            if (controller == null) return;

            float currentInterval = (playerController != null && playerController.IsRunning) 
                                    ? sprintStepInterval 
                                    : walkStepInterval;

            // Only count as moving if the player is actively pressing movement keys
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
            // Shoot a raycast down to detect the floor surface type
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.5f))
            {
                AudioClip clipToPlay;

                if (hit.collider is TerrainCollider)
                {
                    clipToPlay = GetRandomClip(terrainSFX, ref _lastTerrainIndex);
                }
                else
                {
                    clipToPlay = GetRandomClip(floorSFX, ref _lastFloorIndex);
                }

                if (clipToPlay == null) return;

                // Apply pitch & volume jitter
                audioSource.pitch = GetRandomPitch();
                audioSource.PlayOneShot(clipToPlay, GetRandomVolume());
            }
        }

        /// <summary>
        /// Selects a random clip from the given list, guaranteeing no immediate repeat if list > 1.
        /// </summary>
        private AudioClip GetRandomClip(AudioClip[] clips, ref int lastIndex)
        {
            if (clips == null || clips.Length == 0) return null;
            if (clips.Length == 1) return clips[0];

            int offset = UnityEngine.Random.Range(1, clips.Length);
            int nextIndex = (lastIndex + offset) % clips.Length;
            lastIndex = nextIndex;
            return clips[nextIndex];
        }

        private float GetRandomVolume()
        {
            if (volumeJitter <= 0.001f) return footstepVolume;
            return Mathf.Clamp01(footstepVolume + UnityEngine.Random.Range(-volumeJitter, volumeJitter));
        }

        private float GetRandomPitch()
        {
            if (pitchJitter <= 0.001f) return basePitch;
            return Mathf.Clamp(basePitch + UnityEngine.Random.Range(-pitchJitter, pitchJitter), 0.1f, 3f);
        }
    }
}
