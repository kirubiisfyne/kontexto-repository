using UnityEngine;

namespace Master.Scripts.UI
{
    /// <summary>
    /// Minimal controller to switch notebook pages via Unity Button OnClick events.
    /// </summary>
    public class NotebookTabController : MonoBehaviour
    {
        [Tooltip("The Animator on PagesRoot driving anim_pageTransition.")]
        [SerializeField] private Animator pagesAnimator;

        [Tooltip("The bool parameter name in PagesRoot.controller.")]
        [SerializeField] private string isMapTabBool;

        private void Awake()
        {
            if (pagesAnimator == null)
            {
                pagesAnimator = GetComponent<Animator>();
            }
        }

        /// <summary>
        /// Call this from the To-Do bookmark button OnClick event.
        /// </summary>
        public void SwitchToTodo()
        {
            SetMapTab(true);
        }

        /// <summary>
        /// Call this from the Campus Map bookmark button OnClick event.
        /// </summary>
        public void SwitchToMap()
        {
            SetMapTab(false);
        }

        /// <summary>
        /// Generic function usable directly with dynamic/static bool in UnityEvents.
        /// </summary>
        public void SetMapTab(bool isMap)
        {
            if (pagesAnimator != null)
            {
                pagesAnimator.SetBool(isMapTabBool, isMap);
            }
        }
    }
}
