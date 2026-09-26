using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Master.Scripts
{
    public class VolumeController : MonoBehaviour
    {
        public const string PrefMaster = "kontexto.audio.master";
        public const string PrefBgm = "kontexto.audio.bgm";
        public const string PrefSfx = "kontexto.audio.sfx";

        [Header("Audio Mixer")]
        [Tooltip("Drag your MainMixer asset here")]
        public AudioMixer mainMixer;

        [Header("UI Sliders")]
        public Slider masterSlider;
        public Slider bgmSlider;
        public Slider sfxSlider;

        private void Start()
        {
            if (mainMixer == null) return;

            // 1. Force the sliders to go from 0.0001 to 1 instead of 0 to 1
            // We do this because log10(0) is a math error (negative infinity)
            if (masterSlider) { masterSlider.minValue = 0.0001f; masterSlider.maxValue = 1f; }
            if (bgmSlider) { bgmSlider.minValue = 0.0001f; bgmSlider.maxValue = 1f; }
            if (sfxSlider) { sfxSlider.minValue = 0.0001f; sfxSlider.maxValue = 1f; }

            // 2. Be faithful to the AudioMixer defaults:
            // If the player has saved a custom preference, apply it.
            // Otherwise, read the mixer's default snapshot levels directly so the UI matches the actual sounds.
            float masterVal = GetOrLoadSliderValue("MasterVolume", PrefMaster);
            float bgmVal = GetOrLoadSliderValue("BGMVolume", PrefBgm);
            float sfxVal = GetOrLoadSliderValue("SFXVolume", PrefSfx);

            // Sync sliders visually to the resolved values
            if (masterSlider) masterSlider.SetValueWithoutNotify(masterVal);
            if (bgmSlider) bgmSlider.SetValueWithoutNotify(bgmVal);
            if (sfxSlider) sfxSlider.SetValueWithoutNotify(sfxVal);

            // 3. Add listeners to the sliders so they update the mixer and save to PlayerPrefs when dragged
            if (masterSlider) masterSlider.onValueChanged.AddListener(SetMasterVolume);
            if (bgmSlider) bgmSlider.onValueChanged.AddListener(SetBGMVolume);
            if (sfxSlider) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        private float GetOrLoadSliderValue(string paramName, string prefKey)
        {
            if (PlayerPrefs.HasKey(prefKey))
            {
                float savedVal = Mathf.Clamp(PlayerPrefs.GetFloat(prefKey), 0.0001f, 1f);
                mainMixer.SetFloat(paramName, Mathf.Log10(savedVal) * 20f);
                return savedVal;
            }

            // Read the mixer snapshot's default dB and convert to linear 0..1
            if (mainMixer.GetFloat(paramName, out float currentDB))
            {
                return Mathf.Clamp(Mathf.Pow(10f, currentDB / 20f), 0.0001f, 1f);
            }

            return 0.8f;
        }

        public void SetMasterVolume(float sliderValue)
        {
            SetVolumeInternal("MasterVolume", PrefMaster, sliderValue);
        }

        public void SetBGMVolume(float sliderValue)
        {
            SetVolumeInternal("BGMVolume", PrefBgm, sliderValue);
        }

        public void SetSFXVolume(float sliderValue)
        {
            SetVolumeInternal("SFXVolume", PrefSfx, sliderValue);
        }

        private void SetVolumeInternal(string paramName, string prefKey, float sliderValue)
        {
            if (mainMixer == null) return;
            sliderValue = Mathf.Clamp(sliderValue, 0.0001f, 1f);
            mainMixer.SetFloat(paramName, Mathf.Log10(sliderValue) * 20f);
            PlayerPrefs.SetFloat(prefKey, sliderValue);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Applies saved player volume preferences to the mixer on boot if they exist.
        /// If no saved preference exists, preserves the mixer's designed snapshot levels.
        /// </summary>
        public static void ApplySavedVolumes(AudioMixer mixer)
        {
            if (mixer == null) return;

            if (PlayerPrefs.HasKey(PrefMaster))
            {
                float master = Mathf.Clamp(PlayerPrefs.GetFloat(PrefMaster), 0.0001f, 1f);
                mixer.SetFloat("MasterVolume", Mathf.Log10(master) * 20f);
            }

            if (PlayerPrefs.HasKey(PrefBgm))
            {
                float bgm = Mathf.Clamp(PlayerPrefs.GetFloat(PrefBgm), 0.0001f, 1f);
                mixer.SetFloat("BGMVolume", Mathf.Log10(bgm) * 20f);
            }

            if (PlayerPrefs.HasKey(PrefSfx))
            {
                float sfx = Mathf.Clamp(PlayerPrefs.GetFloat(PrefSfx), 0.0001f, 1f);
                mixer.SetFloat("SFXVolume", Mathf.Log10(sfx) * 20f);
            }
        }
    }
}
