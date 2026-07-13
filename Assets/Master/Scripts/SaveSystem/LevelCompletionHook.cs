using UnityEngine;

namespace Master.Scripts.SaveSystem
{
    /// <summary>
    /// An extension script that can be attached to Scene Gates.
    /// Hooks into the warp event to finalize level data if all tasks are done.
    /// </summary>
    public class LevelCompletionHook : MonoBehaviour
    {
        public void FinalizeLevelData()
        {
            if (LevelLoader.Current != null && LevelLoader.Current.AreAllTasksCompleted())
            {
                Debug.Log("LevelCompletionHook: All tasks complete. Wrapping up level data...");
                LevelLoader.Current.CompleteLevel();
                // Note: LevelLoader.Current.SaveGame() is handled by the gate itself right after this.
            }
            else
            {
                Debug.LogWarning("LevelCompletionHook: Player warped, but tasks were NOT fully complete.");
            }
        }
    }
}
