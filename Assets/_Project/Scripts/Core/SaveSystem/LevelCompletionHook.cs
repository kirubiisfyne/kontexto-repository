using UnityEngine;

namespace Master.Scripts.SaveSystem
{
    /// <summary>
    /// An extension script that can be attached to Scene Gates.
    /// Hooks into the warp event to advance to the next level when all tasks are finished.
    /// </summary>
    public class LevelCompletionHook : MonoBehaviour
    {
        public void FinalizeLevelData()
        {
            if (LevelLoader.Current != null && LevelLoader.Current.AreAllTasksCompleted())
            {
                LevelLoader.Current.AdvanceToNextLevel();
            }
            else
            {
                Debug.LogWarning("[LevelCompletionHook] Player triggered exit, but not all tasks are complete.");
            }
        }
    }
}
