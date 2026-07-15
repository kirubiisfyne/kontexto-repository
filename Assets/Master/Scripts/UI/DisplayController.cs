using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Master.Scripts.UI
{
    /// <summary>
    /// Controller for the Display tab of the Options / Settings menu.
    /// Manages two dropdowns:
    ///
    ///   Resolution  — Full HD (1080p) / 2K (1440p) / 4K (2160p)
    ///   Window Mode — Fullscreen / Windowed
    ///
    /// All user preferences are persisted to PlayerPrefs so they survive
    /// between sessions.
    /// </summary>
    public class DisplayController : MonoBehaviour
    {
        // ╔══════════════════════════════════════════════════════════════════╗
        // ║  Inspector References                                           ║
        // ╚══════════════════════════════════════════════════════════════════╝

        [Header("Resolution")]
        [Tooltip("TMP_Dropdown for screen resolution (1080p / 1440p / 4K).")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;

        [Header("Window Mode")]
        [Tooltip("TMP_Dropdown for window mode (Fullscreen / Windowed).")]
        [SerializeField] private TMP_Dropdown windowModeDropdown;

        // ──────────────────────────────────────────────────────────────────
        //  PlayerPrefs keys
        // ──────────────────────────────────────────────────────────────────

        private const string PrefResolution = "Options_Resolution";
        private const string PrefWindowMode = "Options_WindowMode";

        // ──────────────────────────────────────────────────────────────────
        //  Resolution presets
        // ──────────────────────────────────────────────────────────────────

        private struct ResolutionPreset
        {
            public string Label;
            public int    Width;
            public int    Height;
        }

        /// <summary>Fixed resolution list: Full HD → 2K → 4K.</summary>
        private static readonly ResolutionPreset[] Resolutions =
        {
            new ResolutionPreset { Label = "1920 × 1080  (Full HD)", Width = 1920, Height = 1080 },
            new ResolutionPreset { Label = "2560 × 1440  (2K)",      Width = 2560, Height = 1440 },
            new ResolutionPreset { Label = "3840 × 2160  (4K)",      Width = 3840, Height = 2160 },
        };

        // ──────────────────────────────────────────────────────────────────
        //  Window mode data
        // ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Window mode entries — order matches dropdown indices.
        /// Index 0 → Fullscreen  |  Index 1 → Windowed
        /// </summary>
        private static readonly FullScreenMode[] WindowModes =
        {
            FullScreenMode.ExclusiveFullScreen,
            FullScreenMode.Windowed,
        };

        private static readonly string[] WindowModeLabels =
        {
            "Fullscreen",
            "Windowed",
        };

        // ══════════════════════════════════════════════════════════════════
        //  Lifecycle
        // ══════════════════════════════════════════════════════════════════

        private void Start()
        {
            InitResolution();
            InitWindowMode();
        }

        // ══════════════════════════════════════════════════════════════════
        //  Resolution
        // ══════════════════════════════════════════════════════════════════

        private void InitResolution()
        {
            if (resolutionDropdown == null) return;

            // Populate dropdown options
            resolutionDropdown.ClearOptions();

            List<string> options = new List<string>(Resolutions.Length);
            for (int i = 0; i < Resolutions.Length; i++)
            {
                options.Add(Resolutions[i].Label);
            }

            resolutionDropdown.AddOptions(options);

            // Restore saved index (default to 0 = 1080p)
            int saved = PlayerPrefs.GetInt(PrefResolution, 0);
            if (saved < 0 || saved >= Resolutions.Length) saved = 0;

            resolutionDropdown.SetValueWithoutNotify(saved);
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }

        private void OnResolutionChanged(int index)
        {
            if (index < 0 || index >= Resolutions.Length) return;

            ResolutionPreset preset = Resolutions[index];

            // Apply using the current fullscreen mode so we don't
            // accidentally change the window mode when switching resolution.
            Screen.SetResolution(preset.Width, preset.Height, Screen.fullScreenMode);

            PlayerPrefs.SetInt(PrefResolution, index);
            PlayerPrefs.Save();

            Debug.Log($"[DisplayController] Resolution → {preset.Width}×{preset.Height}");
        }

        // ══════════════════════════════════════════════════════════════════
        //  Window Mode
        // ══════════════════════════════════════════════════════════════════

        private void InitWindowMode()
        {
            if (windowModeDropdown == null) return;

            // Populate dropdown options
            windowModeDropdown.ClearOptions();
            windowModeDropdown.AddOptions(new List<string>(WindowModeLabels));

            // Restore saved index or detect current mode
            int defaultIndex = ModeToIndex(Screen.fullScreenMode);
            int saved = PlayerPrefs.GetInt(PrefWindowMode, defaultIndex);
            if (saved < 0 || saved >= WindowModes.Length) saved = defaultIndex;

            windowModeDropdown.SetValueWithoutNotify(saved);
            windowModeDropdown.onValueChanged.AddListener(OnWindowModeChanged);
        }

        private void OnWindowModeChanged(int index)
        {
            if (index < 0 || index >= WindowModes.Length) return;

            FullScreenMode mode = WindowModes[index];
            Screen.SetResolution(Screen.width, Screen.height, mode);

            PlayerPrefs.SetInt(PrefWindowMode, index);
            PlayerPrefs.Save();

            Debug.Log($"[DisplayController] Window mode → {WindowModeLabels[index]}");
        }

        /// <summary>
        /// Maps a FullScreenMode enum to the corresponding dropdown index.
        /// Falls back to 0 (Fullscreen) for any unexpected value.
        /// </summary>
        private static int ModeToIndex(FullScreenMode mode)
        {
            for (int i = 0; i < WindowModes.Length; i++)
            {
                if (WindowModes[i] == mode) return i;
            }
            return 0;
        }
    }
}
