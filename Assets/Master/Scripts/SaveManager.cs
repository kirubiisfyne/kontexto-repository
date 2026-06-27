using UnityEngine;
using System.IO;

namespace Master.Scripts
{
    /// <summary>
    /// Static utility for reading/writing PlayerData to a local JSON file.
    /// </summary>
    public static class SaveManager
    {
        private const string SAVE_FILENAME = "player_save.json";

        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, SAVE_FILENAME);

        /// <summary>
        /// Loads PlayerData from disk. Returns a fresh instance if no save exists.
        /// </summary>
        public static PlayerData Load()
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log("SaveManager: No save file found. Starting fresh.");
                return new PlayerData();
            }

            string json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<PlayerData>(json);

            Debug.Log($"SaveManager: Loaded save from {SavePath}");
            return data ?? new PlayerData();
        }

        /// <summary>
        /// Writes PlayerData to disk immediately.
        /// </summary>
        public static void Save(PlayerData data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);

            Debug.Log($"SaveManager: Saved to {SavePath}");
        }

        /// <summary>
        /// Deletes the save file. Used for "New Game".
        /// </summary>
        public static void DeleteSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log("SaveManager: Save file deleted.");
            }
        }
    }
}
