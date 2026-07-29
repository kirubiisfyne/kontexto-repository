using System.Collections;
using UnityEngine;

namespace Master.Scripts.UI
{
    [RequireComponent(typeof(Animator))]
    public class OptionController : MonoBehaviour
    {
        [Header("Animator Settings")]
        [SerializeField] private Animator animator;

        [Header("Panel References")]
        [Tooltip("The child panel containing the options UI elements.")]
        [SerializeField] private GameObject optionPanel;

        [Header("Button Depth References")]
        [Tooltip("The Display Tab Button RectTransform.")]
        [SerializeField] private RectTransform displayTabButton;

        [Tooltip("The Audio Tab Button RectTransform.")]
        [SerializeField] private RectTransform audioTabButton;

        [Tooltip("Delay in seconds before deactivating optionPanel after transition out.")]
        [SerializeField] private float closeDelay = 0.5f;

        [Header("Tab Defaults")]
        [Tooltip("If true, defaults to Display Tab when enabled; otherwise Audio Tab.")]
        [SerializeField] private bool defaultToDisplayTab = true;

        // Animator Parameter Names
        [Header("Animator Parameter Names")]
        [SerializeField] private string optionVisibleParam = "isOptionVisible";
        [SerializeField] private string displayVisibleParam = "isDisplayVisible";

        // Cached Parameter Hashes
        private int isOptionVisibleHash;
        private int isDisplayVisibleHash;

        private Coroutine disableCoroutine;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            // Cache parameter hashes for optimized performance
            isOptionVisibleHash = Animator.StringToHash(optionVisibleParam);
            isDisplayVisibleHash = Animator.StringToHash(displayVisibleParam);
        }

        private void OnEnable()
        {
            // Sync default tab state on enable
            SetDisplayVisible(defaultToDisplayTab);
        }

        #region Option Menu Controls

        /// <summary>
        /// Sets the isOptionVisible animator parameter.
        /// Enables optionPanel before transition in, and disables optionPanel after closeDelay when transitioning out.
        /// </summary>
        public void SetOptionVisible(bool isVisible)
        {
            if (disableCoroutine != null)
            {
                StopCoroutine(disableCoroutine);
                disableCoroutine = null;
            }

            if (isVisible)
            {
                if (optionPanel != null)
                {
                    optionPanel.SetActive(true);
                }

                if (animator != null)
                {
                    animator.SetBool(isOptionVisibleHash, true);
                }
            }
            else
            {
                if (animator != null)
                {
                    animator.SetBool(isOptionVisibleHash, false);
                }

                if (optionPanel != null && gameObject.activeInHierarchy)
                {
                    disableCoroutine = StartCoroutine(DisablePanelRoutine());
                }
                else if (optionPanel != null)
                {
                    optionPanel.SetActive(false);
                }
            }
        }

        private IEnumerator DisablePanelRoutine()
        {
            // Wait for real time seconds so animation works even if Time.timeScale = 0 (game paused)
            yield return new WaitForSecondsRealtime(closeDelay);

            if (optionPanel != null)
            {
                optionPanel.SetActive(false);
            }

            disableCoroutine = null;
        }

        /// <summary>
        /// Opens the option menu.
        /// </summary>
        public void OpenOption()
        {
            SetOptionVisible(true);
        }

        /// <summary>
        /// Closes the option menu.
        /// </summary>
        public void CloseOption()
        {
            SetOptionVisible(false);
        }

        /// <summary>
        /// Toggles the option menu visibility state.
        /// </summary>
        public void ToggleOption()
        {
            if (animator != null)
            {
                bool current = animator.GetBool(isOptionVisibleHash);
                SetOptionVisible(!current);
            }
        }

        #endregion

        #region Tab Switch Controls

        /// <summary>
        /// Sets the isDisplayVisible animator parameter (true = Display Tab, false = Audio Tab).
        /// </summary>
        public void SetDisplayVisible(bool isVisible)
        {
            if (animator != null)
            {
                animator.SetBool(isDisplayVisibleHash, isVisible);
            }
        }

        /// <summary>
        /// Switches UI tab to Display tab.
        /// </summary>
        public void ShowDisplayTab()
        {
            SetDisplayVisible(true);
        }

        /// <summary>
        /// Switches UI tab to Audio tab.
        /// </summary>
        public void ShowAudioTab()
        {
            SetDisplayVisible(false);
        }

        /// <summary>
        /// Toggles between Audio tab and Display tab.
        /// </summary>
        public void ToggleDisplayTab()
        {
            if (animator != null)
            {
                bool current = animator.GetBool(isDisplayVisibleHash);
                SetDisplayVisible(!current);
            }
        }

        #endregion

        #region Animation Event Depth Controls

        /// <summary>
        /// Animation Event: Moves Display button immediately behind Audio button in hierarchy.
        /// </summary>
        public void SendDisplayButtonToBack()
        {
            if (displayTabButton != null && audioTabButton != null)
            {
                int audioIndex = audioTabButton.GetSiblingIndex();
                displayTabButton.SetSiblingIndex(audioIndex);
            }
        }

        /// <summary>
        /// Animation Event: Brings Display button immediately in front of Audio button (without overlapping other UI elements).
        /// </summary>
        public void BringDisplayButtonToFront()
        {
            if (displayTabButton != null && audioTabButton != null)
            {
                int audioIndex = audioTabButton.GetSiblingIndex();
                displayTabButton.SetSiblingIndex(audioIndex + 1);
            }
        }

        #endregion
    }
}
