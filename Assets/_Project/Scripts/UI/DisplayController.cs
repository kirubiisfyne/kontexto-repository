using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Master.Scripts.UI
{
    public class DisplayController : MonoBehaviour
    {
        [Header("Dropdown References")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown windowModeDropdown;

        private const string PrefWidth = "kontexto.display.res_width";
        private const string PrefHeight = "kontexto.display.res_height";
        private const string PrefMode = "kontexto.display.windowmode";

        private List<Resolution> availableResolutions = new List<Resolution>();
        private int currentResolutionIndex = 0;

        private readonly FullScreenMode[] supportedModes = new[]
        {
            FullScreenMode.FullScreenWindow,
            FullScreenMode.ExclusiveFullScreen,
            FullScreenMode.Windowed
        };

        private readonly string[] modeLabels = new[]
        {
            "Borderless Fullscreen",
            "Exclusive Fullscreen",
            "Windowed"
        };

        private void Awake()
        {
            AutoResolveReferences();
        }

        private void Start()
        {
            InitializeWindowModes();
            InitializeResolutions();
            ApplyInitialSettings();
        }

        private void AutoResolveReferences()
        {
            if (resolutionDropdown == null)
            {
                var tr = transform.Find("dropdown_resolution");
                if (tr != null)
                {
                    resolutionDropdown = tr.GetComponent<TMP_Dropdown>();
                }
                else
                {
                    var all = GetComponentsInChildren<TMP_Dropdown>(true);
                    foreach (var d in all)
                    {
                        if (d.gameObject.name.ToLower().Contains("res"))
                        {
                            resolutionDropdown = d;
                            break;
                        }
                    }
                }
            }

            if (windowModeDropdown == null)
            {
                var tr = transform.Find("dropdown_windowmode");
                if (tr != null)
                {
                    windowModeDropdown = tr.GetComponent<TMP_Dropdown>();
                }
                else
                {
                    var all = GetComponentsInChildren<TMP_Dropdown>(true);
                    foreach (var d in all)
                    {
                        if (d.gameObject.name.ToLower().Contains("window") || d.gameObject.name.ToLower().Contains("mode"))
                        {
                            windowModeDropdown = d;
                            break;
                        }
                    }
                }
            }
        }

        private void InitializeWindowModes()
        {
            if (windowModeDropdown == null) return;

            windowModeDropdown.ClearOptions();
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            for (int i = 0; i < modeLabels.Length; i++)
            {
                options.Add(new TMP_Dropdown.OptionData(modeLabels[i]));
            }
            windowModeDropdown.AddOptions(options);

            int savedMode = PlayerPrefs.GetInt(PrefMode, (int)Screen.fullScreenMode);
            int selectedIndex = 0;
            for (int i = 0; i < supportedModes.Length; i++)
            {
                if ((int)supportedModes[i] == savedMode)
                {
                    selectedIndex = i;
                    break;
                }
            }

            windowModeDropdown.SetValueWithoutNotify(selectedIndex);
            windowModeDropdown.onValueChanged.AddListener(OnWindowModeChanged);
        }

        private void InitializeResolutions()
        {
            if (resolutionDropdown == null) return;

            resolutionDropdown.ClearOptions();
            availableResolutions.Clear();

            Resolution[] systemResolutions = Screen.resolutions;
            var uniqueMap = new HashSet<string>();

            // Collect unique resolutions
            for (int i = 0; i < systemResolutions.Length; i++)
            {
                var r = systemResolutions[i];
                string key = $"{r.width}x{r.height}";
                if (!uniqueMap.Contains(key))
                {
                    uniqueMap.Add(key);
                    availableResolutions.Add(r);
                }
            }

            // Fallback presets if system resolutions report empty (common in editor / some test envs)
            if (availableResolutions.Count == 0)
            {
                int[,] presets = new int[,] { { 1920, 1080 }, { 1600, 900 }, { 1366, 768 }, { 1280, 720 } };
                for (int i = 0; i < presets.GetLength(0); i++)
                {
                    Resolution r = new Resolution { width = presets[i, 0], height = presets[i, 1] };
                    availableResolutions.Add(r);
                }
            }

            // Sort ascending by width
            availableResolutions.Sort((a, b) =>
            {
                int cmp = a.width.CompareTo(b.width);
                return cmp != 0 ? cmp : a.height.CompareTo(b.height);
            });

            int savedWidth = PlayerPrefs.GetInt(PrefWidth, Screen.width);
            int savedHeight = PlayerPrefs.GetInt(PrefHeight, Screen.height);

            currentResolutionIndex = 0;
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            for (int i = 0; i < availableResolutions.Count; i++)
            {
                var res = availableResolutions[i];
                options.Add(new TMP_Dropdown.OptionData($"{res.width} x {res.height}"));

                if (res.width == savedWidth && res.height == savedHeight)
                {
                    currentResolutionIndex = i;
                }
                else if (res.width == Screen.width && res.height == Screen.height && currentResolutionIndex == 0)
                {
                    currentResolutionIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.SetValueWithoutNotify(currentResolutionIndex);
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }

        private void ApplyInitialSettings()
        {
            if (PlayerPrefs.HasKey(PrefWidth) && PlayerPrefs.HasKey(PrefHeight) && PlayerPrefs.HasKey(PrefMode))
            {
                int w = PlayerPrefs.GetInt(PrefWidth);
                int h = PlayerPrefs.GetInt(PrefHeight);
                FullScreenMode mode = (FullScreenMode)PlayerPrefs.GetInt(PrefMode);
                Screen.SetResolution(w, h, mode);
            }
        }

        private void OnResolutionChanged(int index)
        {
            if (index < 0 || index >= availableResolutions.Count) return;

            currentResolutionIndex = index;
            var targetRes = availableResolutions[index];
            FullScreenMode currentMode = GetCurrentSelectedMode();

            Screen.SetResolution(targetRes.width, targetRes.height, currentMode);

            PlayerPrefs.SetInt(PrefWidth, targetRes.width);
            PlayerPrefs.SetInt(PrefHeight, targetRes.height);
            PlayerPrefs.Save();
        }

        private void OnWindowModeChanged(int index)
        {
            if (index < 0 || index >= supportedModes.Length) return;

            FullScreenMode targetMode = supportedModes[index];
            int w = Screen.width;
            int h = Screen.height;

            if (currentResolutionIndex >= 0 && currentResolutionIndex < availableResolutions.Count)
            {
                w = availableResolutions[currentResolutionIndex].width;
                h = availableResolutions[currentResolutionIndex].height;
            }

            Screen.SetResolution(w, h, targetMode);

            PlayerPrefs.SetInt(PrefMode, (int)targetMode);
            PlayerPrefs.Save();
        }

        private FullScreenMode GetCurrentSelectedMode()
        {
            if (windowModeDropdown != null && windowModeDropdown.value >= 0 && windowModeDropdown.value < supportedModes.Length)
            {
                return supportedModes[windowModeDropdown.value];
            }
            return Screen.fullScreenMode;
        }
    }
}
