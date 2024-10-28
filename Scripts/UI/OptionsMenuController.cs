using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class OptionsMenuController : MonoBehaviour
{
    public GameObject firstSelected;
    [SerializeField] Slider _masterVolumeSlider, _musicVolumeSlider, _sfxVolumeSlider;
    [SerializeField] SlidingTextOption _resolutionSelector, _displaySelector, _languageSelector;
    [SerializeField] Toggle _vsyncToggle;

    [Header("Options data")]
    [SerializeField] Locale[] _languageOptions;

    private void OnEnable()
    {
        // Configure callbacks
        _languageSelector.optionChangedCallback = ChangeLanguage;
        _displaySelector.optionChangedCallback = ChangeDisplay;
        _resolutionSelector.optionChangedCallback = ChangeResolution;

        // Set UI visuals to current configuration
        // Options
        var resVal = Screen.height == 1080 ? 0 : 1;
        _resolutionSelector.ChangeOption(resVal, true);
        var disVal = Screen.fullScreen ? 1 : 0;
        _displaySelector.ChangeOption(disVal, true);
        var langVal = _languageOptions.ToList().IndexOf(LocalizationSettings.SelectedLocale);
        _languageSelector.ChangeOption(langVal, true);
        _vsyncToggle.isOn = QualitySettings.vSyncCount != 0;
        // Volumes
        _masterVolumeSlider.value = AudioManager.GetMasterVolume();
        _musicVolumeSlider.value = AudioManager.GetMusicVolume();
        _sfxVolumeSlider.value = AudioManager.GetSfxVolume();
    }

    public void ChangeResolution(int value)
    {
        switch (value)
        {
            case 0:  // 1080p
                Screen.SetResolution(1920, 1080, Screen.fullScreen);
                break;
            case 1:  // 720p
                Screen.SetResolution(1280, 720, Screen.fullScreen);
                break;
            default:
                break;
        }
    }

    public void ChangeDisplay(int value)
    {
        // Windowed = 0; Fullscreen = 1
        Screen.fullScreen = value != 0;
    }

    public void ChangeLanguage(int value)
    {
        LocalizationSettings.SelectedLocale = _languageOptions[value];
    }

    public void ChangeVSync(bool value)
    {
        if (value)
            QualitySettings.vSyncCount = 1;
        else
            QualitySettings.vSyncCount = 0;
    }

    public void ChangeMasterVolume(float volumeLevel)
    {
        AudioManager.Instance?.SetMasterVolume(volumeLevel);
    }

    public void ChangeMusicVolume(float volumeLevel)
    {
        AudioManager.Instance?.SetMusicVolume(volumeLevel);
    }

    public void ChangeSfxVolume(float volumeLevel)
    {
        AudioManager.Instance?.SetSfxVolume(volumeLevel);
    }
}
