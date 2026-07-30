using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

namespace Master.Scripts
{
    /// <summary>
    /// Handles camera reframing to an NPC's virtual camera during dialogue,
    /// and fades the player's materials in/out to avoid obstruction.
    /// </summary>
    public class CameraReframer : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The player's body materials used for opacity fading during NPC focus.")]
        public Material[] playerMaterials;

        [Tooltip("The CinemachineBrain on the main camera. Auto-assigned in Awake if left empty.")]
        public CinemachineBrain cinemachineBrain;

        [Header("Camera")] 
        [Tooltip("The NPC's virtual camera to activate during dialogue.")]
        public bool NPCLookAt = true;
        public CinemachineCamera NPCCamera;

        [Header("Player")]
        [Tooltip("The PlayerController to lock during dialogue. Auto-assigned in Awake if left empty.")]
        public PlayerController playerController;

        // Runtime coroutine handles — not exposed to the Inspector.
        private Coroutine fadeCoroutine;
        private Coroutine rotateCoroutine;

        private void Awake()
        {
            GameObject playerBody = GameObject.FindGameObjectWithTag("PlayerBody");
            if (playerBody != null)
            {
                Renderer[] renderers = playerBody.GetComponentsInChildren<Renderer>();
                List<Material> matList = new List<Material>();
                foreach (Renderer rend in renderers)
                {
                    if (rend != null && rend.materials != null)
                    {
                        matList.AddRange(rend.materials);
                    }
                }
                playerMaterials = matList.ToArray();

                playerController = playerBody.GetComponentInParent<PlayerController>();
            }

            if (Camera.main != null)
            {
                cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
            }
        }

        public void StartNPCFocus()
        {
            if (NPCCamera != null)
            {
                NPCCamera.Priority = 20;
            }
            playerController?.SetInputActive(false);

            float blendDuration = GetBlendDuration(NPCCamera);

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadePlayerMaterial(0.1f, blendDuration));

            if (rotateCoroutine != null) StopCoroutine(rotateCoroutine);
            if (playerController != null && NPCLookAt)
                rotateCoroutine = StartCoroutine(SmoothLookAtPlayer(playerController.transform, blendDuration));
        }

        public void EndNPCFocus()
        {
            if (NPCCamera != null)
            {
                NPCCamera.Priority = 0;
            }
            playerController?.SetInputActive(true);

            if (rotateCoroutine != null && NPCLookAt)
            {
                StopCoroutine(rotateCoroutine);
                rotateCoroutine = null;
            }

            float blendDuration = GetBlendDuration(NPCCamera);
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadePlayerMaterial(1f, blendDuration));
        }

        public float GetBlendDuration(CinemachineCamera targetCam)
        {
            float duration = 2f;

            if (cinemachineBrain != null && targetCam != null)
            {
                CinemachineBlendDefinition blendDef = cinemachineBrain.CustomBlends != null ?
                    cinemachineBrain.CustomBlends.GetBlendForVirtualCameras(cinemachineBrain.ActiveVirtualCamera?.Name, targetCam.Name, cinemachineBrain.DefaultBlend) :
                    cinemachineBrain.DefaultBlend;

                duration = blendDef.Time;
            }
            return duration;
        }

        public IEnumerator FadePlayerMaterial(float targetValue, float duration)
        {
            if (playerMaterials == null || playerMaterials.Length == 0) yield break;

            float[] startValues = new float[playerMaterials.Length];
            for (int i = 0; i < playerMaterials.Length; i++)
            {
                if (playerMaterials[i] != null && playerMaterials[i].HasProperty("_Opacity"))
                {
                    startValues[i] = playerMaterials[i].GetFloat("_Opacity");
                }
                else
                {
                    startValues[i] = 1f;
                }
            }

            float timeElapsed = 0f;

            while (timeElapsed < duration)
            {
                float t = timeElapsed / duration;
                for (int i = 0; i < playerMaterials.Length; i++)
                {
                    if (playerMaterials[i] != null && playerMaterials[i].HasProperty("_Opacity"))
                    {
                        float currentValue = Mathf.Lerp(startValues[i], targetValue, t);
                        playerMaterials[i].SetFloat("_Opacity", currentValue);
                    }
                }

                timeElapsed += Time.deltaTime;
                yield return null;
            }

            for (int i = 0; i < playerMaterials.Length; i++)
            {
                if (playerMaterials[i] != null && playerMaterials[i].HasProperty("_Opacity"))
                {
                    playerMaterials[i].SetFloat("_Opacity", targetValue);
                }
            }
        }

        /// <summary>
        /// Smoothly rotates the NPC to face the target (player) over the given duration.
        /// Only the Y-axis is affected so the NPC never tilts.
        /// </summary>
        private IEnumerator SmoothLookAtPlayer(Transform target, float duration)
        {
            Quaternion startRotation = transform.rotation;

            Vector3 direction = target.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f) yield break;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float timeElapsed = 0f;

            while (timeElapsed < duration)
            {
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, timeElapsed / duration);
                timeElapsed += Time.deltaTime;
                yield return null;
            }

            transform.rotation = targetRotation;
        }
    }
}
