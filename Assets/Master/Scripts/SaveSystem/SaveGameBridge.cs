using UnityEngine;

namespace Master.Scripts.SaveSystem
{
    /// <summary>
    /// Lightweight bridge for prefab-based UI (pause menu, HUD, etc.).
    /// Routes save/load calls to the current scene's LevelLoader via the static accessor.
    /// Attach this to any prefab that needs a save/load button but can't hold scene references.
    /// </summary>
    public class SaveGameBridge : MonoBehaviour
    {
        /// <summary>
        /// Wire to a Button's OnClick or any UnityEvent.
        /// Routes to LevelLoader.Current.SaveGame().
        /// </summary>
        public void SaveGame()
        {
            if (LevelLoader.Current != null)
                LevelLoader.Current.SaveGame();
            else
                Debug.LogWarning("SaveGameBridge: No LevelLoader in this scene.");
        }

        /// <summary>
        /// Wire to a Button's OnClick or any UnityEvent.
        /// Routes to LevelLoader.Current.LoadGame().
        /// </summary>
        public void LoadGame()
        {
            if (LevelLoader.Current != null)
                LevelLoader.Current.LoadGame();
            else
                Debug.LogWarning("SaveGameBridge: No LevelLoader in this scene.");
        }
    }
}
