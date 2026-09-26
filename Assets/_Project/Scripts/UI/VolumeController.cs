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

            // 2. Load saved volume preferences or initialize defaults (0.8 = ~-2dB)
            float masterVal = PlayerPrefs.GetFloat(PrefMaster, 0.8f);
            float bgmVal = PlayerPrefs.GetFloat(PrefBgm, 0.8f);
            float sfxVal = PlayerPrefs.GetFloat(PrefSfx, 0.8f);

            SetMasterVolumeInternal(masterVal, false);
            SetBGMVolumeInternal(bgmVal, false);
            SetSFXVolumeInternal(sfxVal, false);

            if (masterSlider) masterSlider.SetValueWithoutNotify(masterVal);
            if (bgmSlider) bgmSlider.SetValueWithoutNotify(bgmVal);
            if (sfxSlider) sfxSlider.SetValueWithoutNotify(sfxVal);

            // 3. Add listeners to the sliders so they automatically update when dragged
            if (masterSlider) masterSlider.onValueChanged.AddListener(SetMasterVolume);
            if (bgmSlider) bgmSlider.onValueChanged.AddListener(SetBGMVolume);
            if (sfxSlider) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        public void SetMasterVolume(float sliderValue)
        {
            SetMasterVolumeInternal(sliderValue, true);
        }

        public void SetBGMVolume(float sliderValue)
        {
            SetBGMVolumeInternal(sliderValue, true);
        }

        public void SetSFXVolume(float sliderValue)
        {
            SetSFXVolumeInternal(sliderValue, true);
        }

        private void SetMasterVolumeInternal(float sliderValue, bool save)
        {
            if (mainMixer == null) return;
            sliderValue = Mathf.Clamp(sliderValue, 0.0001f, 1f);
            mainMixer.SetFloat("MasterVolume", Mathf.Log10(sliderValue) * 20f);
            if (save)
            {
                PlayerPrefs.SetFloat(PrefMaster, sliderValue);
                PlayerPrefs.Save();
            }
        }

        private void SetBGMVolumeInternal(float sliderValue, bool save)
        {
            if (mainMixer == null) return;
            sliderValue = Mathf.Clamp(sliderValue, 0.0001f, 1f);
            mainMixer.SetFloat("BGMVolume", Mathf.Log10(sliderValue) * 20f);
            if (save)
            {
                PlayerPrefs.SetFloat(PrefBgm, sliderValue);
                PlayerPrefs.Save();
            }
        }

        private void SetSFXVolumeInternal(float sliderValue, bool save)
        {
            if (mainMixer == null) return;
            sliderValue = Mathf.Clamp(sliderValue, 0.0001f, 1f);
            mainMixer.SetFloat("SFXVolume", Mathf.Log10(sliderValue) * 20f);
            if (save)
            {
                PlayerPrefs.SetFloat(PrefSfx, sliderValue);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Utility method to apply saved volumes directly to an AudioMixer on startup.
        /// </summary>
        public static void ApplySavedVolumes(AudioMixer mixer)
        {
            if (mixer == null) return;
            float master = PlayerPrefs.GetFloat(PrefMaster, 0.8f);
            float bgm = PlayerPrefs.GetFloat(PrefBgm, 0.8f);
            float sfx = PlayerPrefs.GetFloat(PrefSfx, 0.8f);

            mixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Clamp(master, 0.0001f, 1f)) * 20f);
            mixer.SetFloat("BGMVolume", Mathf.Log10(Mathf.Clamp(bgm, 0.0001f, 1f)) * 20f);
            mixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(sfx, 0.0001f, 1f)) * 20f);
        }
    }
}
