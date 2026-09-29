using UnityEngine;
using UnityEngine.UI;

namespace Master.Scripts
{
    /// <summary>
    /// UI controller that synchronizes volume sliders with the persistent AudioManager.
    /// </summary>
    public class VolumeController : MonoBehaviour
    {
        [Header("UI Sliders")]
        public Slider masterSlider;
        public Slider bgmSlider;
        public Slider sfxSlider;

        private bool isInitialized = false;

        private void Awake()
        {
            InitializeSliders();
        }

        private void OnEnable()
        {
            SyncSliders();
        }

        private void InitializeSliders()
        {
            if (isInitialized) return;

            SetupSlider(masterSlider, v => AudioManager.Instance?.SetMasterVolume(v));
            SetupSlider(bgmSlider, v => AudioManager.Instance?.SetBGMVolume(v));
            SetupSlider(sfxSlider, v => AudioManager.Instance?.SetSFXVolume(v));

            isInitialized = true;
        }

        private void SetupSlider(Slider slider, UnityEngine.Events.UnityAction<float> onValueChanged)
        {
            if (slider == null) return;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.onValueChanged.AddListener(onValueChanged);
        }

        public void SyncSliders()
        {
            if (AudioManager.Instance == null) return;
            InitializeSliders();

            if (masterSlider) masterSlider.SetValueWithoutNotify(AudioManager.Instance.GetMasterVolume());
            if (bgmSlider) bgmSlider.SetValueWithoutNotify(AudioManager.Instance.GetBGMVolume());
            if (sfxSlider) sfxSlider.SetValueWithoutNotify(AudioManager.Instance.GetSFXVolume());
        }
    }
}
