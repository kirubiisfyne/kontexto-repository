using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

namespace Master.Scripts
{
    /// <summary>
    /// Handles camera reframing to an NPC's virtual camera during dialogue,
    /// and fades the player's material in/out to avoid obstruction.
    /// </summary>
    public class CameraReframer : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The player's body material used for opacity fading during NPC focus.")]
        public Material playerMaterial;

        [Tooltip("The CinemachineBrain on the main camera. Auto-assigned in Awake if left empty.")]
        public CinemachineBrain cinemachineBrain;

        [Header("Camera")] [Tooltip("The NPC's virtual camera to activate during dialogue.")]
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
            playerMaterial = playerBody.GetComponent<SkinnedMeshRenderer>().material;
            playerController = playerBody.GetComponentInParent<PlayerController>();
            cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
        }

        public void StartNPCFocus()
        {
            NPCCamera.Priority = 20;
            playerController?.SetInputActive(false);

            float blendDuration = GetBlendDuration(NPCCamera);

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadePlayerMaterial(0.2f, blendDuration));

            if (rotateCoroutine != null) StopCoroutine(rotateCoroutine);
            if (playerController != null  && NPCLookAt)
                rotateCoroutine = StartCoroutine(SmoothLookAtPlayer(playerController.transform, blendDuration));
        }

        public void EndNPCFocus()
        {
            NPCCamera.Priority = 0;
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

            if (cinemachineBrain != null)
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
            float startValue = playerMaterial.GetFloat("_Opacity");
            float timeElapsed = 0f;

            while (timeElapsed < duration)
            {
                float currentValue = Mathf.Lerp(startValue, targetValue, timeElapsed / duration);
                playerMaterial.SetFloat("_Opacity", currentValue);

                timeElapsed += Time.deltaTime;
                yield return null;
            }

            playerMaterial.SetFloat("_Opacity", targetValue);
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
