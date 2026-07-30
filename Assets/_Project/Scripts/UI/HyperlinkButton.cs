using UnityEngine;

namespace Kontexto.UI
{
    /// <summary>
    /// Attach this script to a UI Button to open a URL when clicked.
    /// </summary>
    public class HyperlinkButton : MonoBehaviour
    {
        [Tooltip("The URL to open when the button is clicked.")]
        public string url = "https://www.google.com";

        /// <summary>
        /// Call this method from the Button's OnClick event.
        /// </summary>
        public void OpenLink()
        {
            if (!string.IsNullOrEmpty(url))
            {
                Application.OpenURL(url);
            }
        }
        
        /// <summary>
        /// Call this method from the Button's OnClick event to pass a dynamic URL.
        /// </summary>
        public void OpenLinkDynamic(string dynamicUrl)
        {
            if (!string.IsNullOrEmpty(dynamicUrl))
            {
                Application.OpenURL(dynamicUrl);
            }
        }
    }
}
