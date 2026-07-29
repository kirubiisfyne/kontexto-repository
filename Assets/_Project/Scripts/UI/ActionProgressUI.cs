using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Master.Scripts.UI
{
    public class ActionProgressUI : MonoBehaviour
    {
        [Tooltip("Assign the child Panel here. You can safely disable it in the Editor!")]
        [SerializeField] private GameObject visualPanel;
        [SerializeField] private Animator animator;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TMP_Text actionText;
        
        private static readonly int IsVisibleBool = Animator.StringToHash("IsVisible");

        private void OnEnable()
        {
            Master.Scripts.TaskSystem.KeyItemInstance.OnActionWaitStartedGlobal += StartProgress;
        }

        private void OnDisable()
        {
            Master.Scripts.TaskSystem.KeyItemInstance.OnActionWaitStartedGlobal -= StartProgress;
        }

        private void StartProgress(float duration, string text, Transform sourceTransform)
        {
            // We ignore the sourceTransform for the 2D HUD, but it's there if you ever want to make it 3D!
            if (visualPanel != null) visualPanel.SetActive(true);
            if (animator != null) animator.SetBool(IsVisibleBool, true);
            
            if (actionText != null)
            {
                if (string.IsNullOrEmpty(text))
                {
                    actionText.gameObject.SetActive(false);
                }
                else
                {
                    actionText.gameObject.SetActive(true);
                    actionText.text = text;
                }
            }
            
            // Stop any existing coroutine if they trigger rapidly
            StopAllCoroutines();
            StartCoroutine(FillSliderCoroutine(duration));
        }

        private IEnumerator FillSliderCoroutine(float duration)
        {
            float time = 0f;
            
            if (progressBar != null)
            {
                progressBar.value = 0f;
            }
            
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                if (progressBar != null)
                {
                    progressBar.value = Mathf.Clamp01(time / duration);
                }
                yield return null;
            }

            if (animator != null) animator.SetBool(IsVisibleBool, false);
        }
    }
}
