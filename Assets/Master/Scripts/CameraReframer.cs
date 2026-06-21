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

        [Header("Camera")]
        [Tooltip("The NPC's virtual camera to activate during dialogue.")]
        public CinemachineCamera NPCCamera;

        [Header("Player")]
        [Tooltip("The PlayerController to lock during dialogue. Auto-assigned in Awake if left empty.")]
        public PlayerController playerController;

        // Runtime coroutine handle — not exposed to the Inspector.
        private Coroutine fadeCoroutine;

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
            fadeCoroutine = StartCoroutine(FadePlayerMaterial(0.4f, blendDuration));
        }

        public void EndNPCFocus()
        {
            NPCCamera.Priority = 0;
            playerController?.SetInputActive(true);

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
    }
}
