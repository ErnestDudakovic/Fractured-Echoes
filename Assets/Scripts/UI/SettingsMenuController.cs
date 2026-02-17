// ============================================================================
// SettingsMenuController.cs — Settings panel for the main menu
// Handles audio volume, mouse sensitivity, resolution, fullscreen,
// and quality presets.  Works with PlayerPrefs for persistence.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace FracturedEchoes.UI
{
    /// <summary>
    /// Manages the Settings sub-panel.  Wire sliders, dropdowns, and toggles
    /// in the Inspector.  All values are saved to <see cref="PlayerPrefs"/>.
    /// </summary>
    public class SettingsMenuController : MonoBehaviour
    {
        // =====================================================================
        // PREFS KEYS
        // =====================================================================

        private const string KEY_MASTER_VOL  = "Settings_MasterVolume";
        private const string KEY_MUSIC_VOL   = "Settings_MusicVolume";
        private const string KEY_SFX_VOL     = "Settings_SFXVolume";
        private const string KEY_SENSITIVITY = "Settings_MouseSensitivity";
        private const string KEY_FULLSCREEN  = "Settings_Fullscreen";
        private const string KEY_RESOLUTION  = "Settings_Resolution";
        private const string KEY_QUALITY     = "Settings_Quality";
        private const string KEY_VSYNC       = "Settings_VSync";

        // =====================================================================
        // SERIALIZED FIELDS — AUDIO
        // =====================================================================

        [Header("Audio")]
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;

        [SerializeField] private TextMeshProUGUI _masterVolumeLabel;
        [SerializeField] private TextMeshProUGUI _musicVolumeLabel;
        [SerializeField] private TextMeshProUGUI _sfxVolumeLabel;

        // =====================================================================
        // SERIALIZED FIELDS — CONTROLS
        // =====================================================================

        [Header("Controls")]
        [SerializeField] private Slider _sensitivitySlider;
        [SerializeField] private TextMeshProUGUI _sensitivityLabel;

        // =====================================================================
        // SERIALIZED FIELDS — DISPLAY
        // =====================================================================

        [Header("Display")]
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        [SerializeField] private Toggle _fullscreenToggle;
        [SerializeField] private Toggle _vSyncToggle;
        [SerializeField] private TMP_Dropdown _qualityDropdown;

        // =====================================================================
        // SERIALIZED FIELDS — ACTIONS
        // =====================================================================

        [Header("Buttons")]
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _resetButton;

        // =====================================================================
        // RUNTIME
        // =====================================================================

        private Resolution[] _availableResolutions;
        private int _selectedResolutionIndex;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void OnEnable()
        {
            PopulateResolutions();
            PopulateQuality();
            LoadSettings();

            // Wire listeners
            _masterVolumeSlider?.onValueChanged.AddListener(OnMasterVolumeChanged);
            _musicVolumeSlider?.onValueChanged.AddListener(OnMusicVolumeChanged);
            _sfxVolumeSlider?.onValueChanged.AddListener(OnSFXVolumeChanged);
            _sensitivitySlider?.onValueChanged.AddListener(OnSensitivityChanged);

            _applyButton?.onClick.AddListener(ApplySettings);
            _resetButton?.onClick.AddListener(ResetToDefaults);
        }

        private void OnDisable()
        {
            _masterVolumeSlider?.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            _musicVolumeSlider?.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            _sfxVolumeSlider?.onValueChanged.RemoveListener(OnSFXVolumeChanged);
            _sensitivitySlider?.onValueChanged.RemoveListener(OnSensitivityChanged);

            _applyButton?.onClick.RemoveListener(ApplySettings);
            _resetButton?.onClick.RemoveListener(ResetToDefaults);
        }

        // =====================================================================
        // POPULATE DROPDOWNS
        // =====================================================================

        private void PopulateResolutions()
        {
            if (_resolutionDropdown == null) return;

            _availableResolutions = Screen.resolutions;
            _resolutionDropdown.ClearOptions();

            List<string> options = new List<string>();
            int currentIndex = 0;

            for (int i = 0; i < _availableResolutions.Length; i++)
            {
                Resolution res = _availableResolutions[i];
                string label = $"{res.width} x {res.height} @ {res.refreshRateRatio.value:F0}Hz";
                options.Add(label);

                if (res.width == Screen.currentResolution.width &&
                    res.height == Screen.currentResolution.height)
                {
                    currentIndex = i;
                }
            }

            _resolutionDropdown.AddOptions(options);

            int saved = PlayerPrefs.GetInt(KEY_RESOLUTION, currentIndex);
            _resolutionDropdown.value = Mathf.Clamp(saved, 0, options.Count - 1);
            _selectedResolutionIndex = _resolutionDropdown.value;
        }

        private void PopulateQuality()
        {
            if (_qualityDropdown == null) return;

            _qualityDropdown.ClearOptions();
            _qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
            _qualityDropdown.value = PlayerPrefs.GetInt(KEY_QUALITY, QualitySettings.GetQualityLevel());
        }

        // =====================================================================
        // LOAD / SAVE
        // =====================================================================

        private void LoadSettings()
        {
            // Audio
            float master = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
            float music = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 0.8f);
            float sfx = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);

            if (_masterVolumeSlider != null) _masterVolumeSlider.value = master;
            if (_musicVolumeSlider != null) _musicVolumeSlider.value = music;
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.value = sfx;

            UpdateVolumeLabel(_masterVolumeLabel, master);
            UpdateVolumeLabel(_musicVolumeLabel, music);
            UpdateVolumeLabel(_sfxVolumeLabel, sfx);

            // Sensitivity
            float sens = PlayerPrefs.GetFloat(KEY_SENSITIVITY, 2f);
            if (_sensitivitySlider != null) _sensitivitySlider.value = sens;
            UpdateSensitivityLabel(sens);

            // Display
            bool fullscreen = PlayerPrefs.GetInt(KEY_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
            if (_fullscreenToggle != null) _fullscreenToggle.isOn = fullscreen;

            bool vsync = PlayerPrefs.GetInt(KEY_VSYNC, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;
            if (_vSyncToggle != null) _vSyncToggle.isOn = vsync;
        }

        /// <summary>
        /// Applies all current UI values to the engine and saves to PlayerPrefs.
        /// </summary>
        public void ApplySettings()
        {
            // Audio — store values (AudioManager reads from prefs on init)
            float master = _masterVolumeSlider != null ? _masterVolumeSlider.value : 1f;
            float music = _musicVolumeSlider != null ? _musicVolumeSlider.value : 0.8f;
            float sfx = _sfxVolumeSlider != null ? _sfxVolumeSlider.value : 1f;

            PlayerPrefs.SetFloat(KEY_MASTER_VOL, master);
            PlayerPrefs.SetFloat(KEY_MUSIC_VOL, music);
            PlayerPrefs.SetFloat(KEY_SFX_VOL, sfx);

            // Set AudioListener volume as master
            AudioListener.volume = master;

            // Sensitivity
            float sens = _sensitivitySlider != null ? _sensitivitySlider.value : 2f;
            PlayerPrefs.SetFloat(KEY_SENSITIVITY, sens);

            // Resolution
            if (_resolutionDropdown != null && _availableResolutions != null &&
                _resolutionDropdown.value < _availableResolutions.Length)
            {
                _selectedResolutionIndex = _resolutionDropdown.value;
                Resolution res = _availableResolutions[_selectedResolutionIndex];
                bool fs = _fullscreenToggle != null && _fullscreenToggle.isOn;
                Screen.SetResolution(res.width, res.height, fs);

                PlayerPrefs.SetInt(KEY_RESOLUTION, _selectedResolutionIndex);
                PlayerPrefs.SetInt(KEY_FULLSCREEN, fs ? 1 : 0);
            }

            // Quality
            if (_qualityDropdown != null)
            {
                QualitySettings.SetQualityLevel(_qualityDropdown.value, true);
                PlayerPrefs.SetInt(KEY_QUALITY, _qualityDropdown.value);
            }

            // VSync
            bool vsync = _vSyncToggle != null && _vSyncToggle.isOn;
            QualitySettings.vSyncCount = vsync ? 1 : 0;
            PlayerPrefs.SetInt(KEY_VSYNC, vsync ? 1 : 0);

            PlayerPrefs.Save();
            Debug.Log("[Settings] Applied and saved.");
        }

        /// <summary>
        /// Reset all settings to defaults.
        /// </summary>
        public void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(KEY_MASTER_VOL);
            PlayerPrefs.DeleteKey(KEY_MUSIC_VOL);
            PlayerPrefs.DeleteKey(KEY_SFX_VOL);
            PlayerPrefs.DeleteKey(KEY_SENSITIVITY);
            PlayerPrefs.DeleteKey(KEY_FULLSCREEN);
            PlayerPrefs.DeleteKey(KEY_RESOLUTION);
            PlayerPrefs.DeleteKey(KEY_QUALITY);
            PlayerPrefs.DeleteKey(KEY_VSYNC);
            PlayerPrefs.Save();

            LoadSettings();
            Debug.Log("[Settings] Reset to defaults.");
        }

        // =====================================================================
        // SLIDER CALLBACKS
        // =====================================================================

        private void OnMasterVolumeChanged(float value)
        {
            UpdateVolumeLabel(_masterVolumeLabel, value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            UpdateVolumeLabel(_musicVolumeLabel, value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            UpdateVolumeLabel(_sfxVolumeLabel, value);
        }

        private void OnSensitivityChanged(float value)
        {
            UpdateSensitivityLabel(value);
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private void UpdateVolumeLabel(TextMeshProUGUI label, float value)
        {
            if (label != null)
            {
                label.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        private void UpdateSensitivityLabel(float value)
        {
            if (_sensitivityLabel != null)
            {
                _sensitivityLabel.text = $"{value:F1}";
            }
        }
    }
}
