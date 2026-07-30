using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Master.Scripts
{
    public class VolumeController : MonoBehaviour
    {
        [Header("Audio Mixer")]
        [Tooltip("Drag your MainMixer asset here")]
        public AudioMixer mainMixer;

        [Header("UI Sliders")]
        public Slider masterSlider;
        public Slider bgmSlider;
        public Slider sfxSlider;

        private void Start()
        {
            // 1. Force the sliders to go from 0.0001 to 1 instead of 0 to 1
            // We do this because log10(0) is a math error (negative infinity)
            if (masterSlider) { masterSlider.minValue = 0.0001f; masterSlider.maxValue = 1f; }
            if (bgmSlider) { bgmSlider.minValue = 0.0001f; bgmSlider.maxValue = 1f; }
            if (sfxSlider) { sfxSlider.minValue = 0.0001f; sfxSlider.maxValue = 1f; }

            // 2. Sync the sliders visually to match the current AudioMixer volumes!
            // We have to reverse the math: convert Decibels back into a 0-1 linear slider float
            if (masterSlider && mainMixer.GetFloat("MasterVolume", out float masterDB))
            {
                masterSlider.SetValueWithoutNotify(Mathf.Pow(10f, masterDB / 20f));
            }
            if (bgmSlider && mainMixer.GetFloat("BGMVolume", out float bgmDB))
            {
                bgmSlider.SetValueWithoutNotify(Mathf.Pow(10f, bgmDB / 20f));
            }
            if (sfxSlider && mainMixer.GetFloat("SFXVolume", out float sfxDB))
            {
                sfxSlider.SetValueWithoutNotify(Mathf.Pow(10f, sfxDB / 20f));
            }

            // 3. Add listeners to the sliders so they automatically update when dragged
            if (masterSlider) masterSlider.onValueChanged.AddListener(SetMasterVolume);
            if (bgmSlider) bgmSlider.onValueChanged.AddListener(SetBGMVolume);
            if (sfxSlider) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        public void SetMasterVolume(float sliderValue)
        {
            // Convert the slider value (linear) to Audio Decibels (logarithmic)
            mainMixer.SetFloat("MasterVolume", Mathf.Log10(sliderValue) * 20f);
        }

        public void SetBGMVolume(float sliderValue)
        {
            mainMixer.SetFloat("BGMVolume", Mathf.Log10(sliderValue) * 20f);
        }

        public void SetSFXVolume(float sliderValue)
        {
            mainMixer.SetFloat("SFXVolume", Mathf.Log10(sliderValue) * 20f);
        }
    }
}
