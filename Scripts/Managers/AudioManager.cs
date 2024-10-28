using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _effectSource;
    [SerializeField] private AudioSource _uiSource;
    [SerializeField] private AudioSource _ambienceSource;

    [SerializeField] private float _musicAcceleratedPitch;
    [SerializeField] private float _musicAccelerateTransitionDuration;

    public AudioSource EffectSource => _effectSource;
    public AudioSource MusicSource => _musicSource;
    public AudioSource AmbienceSource => _ambienceSource;
    public AudioSource UiSource => _uiSource;

    public static AudioManager Instance {get; private set;}

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            throw new Exception("Several audio managers in scene");

        if (_uiSource)
            _uiSource.ignoreListenerPause = true;

        // Set user volume levels or default
        SetMasterVolume(PlayerPrefs.GetFloat("Settings_MasterVolume", 1));
        SetMusicVolume(PlayerPrefs.GetFloat("Settings_MusicVolume", 1));
        SetSfxVolume(PlayerPrefs.GetFloat("Settings_SfxVolume", 1));
    }

    private void OnDisable()
    {
        PlayerPrefs.Save();
    }

    public void AccelerateMusic() => StartCoroutine(AccelerateMusicInternal());

    private IEnumerator AccelerateMusicInternal()
    {
        var previousPitch = MusicSource.pitch;
        var currentPitch = previousPitch;
        while (currentPitch < _musicAcceleratedPitch)
        {
            currentPitch += (_musicAcceleratedPitch - previousPitch) * (Time.deltaTime / _musicAccelerateTransitionDuration);
            MusicSource.pitch = currentPitch;
            yield return null;
        }

        MusicSource.pitch = _musicAcceleratedPitch;
    }

    public static float GetMasterVolume()
    {
        return PlayerPrefs.GetFloat("Settings_MasterVolume", 1);
    }

    public void SetMasterVolume(float volumeLevel)
    {
        var logVol = Mathf.Log10(volumeLevel) * 20;
        _audioMixer.SetFloat("MasterVolume", logVol);
        PlayerPrefs.SetFloat("Settings_MasterVolume", volumeLevel);
    }

    public static float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat("Settings_MusicVolume", 1);
    }

    public void SetMusicVolume(float volumeLevel)
    {
        var logVol = Mathf.Log10(volumeLevel) * 20;
        _audioMixer.SetFloat("MusicVolume", logVol);
        PlayerPrefs.SetFloat("Settings_MusicVolume", volumeLevel);
    }

    public static float GetSfxVolume()
    {
        return PlayerPrefs.GetFloat("Settings_SfxVolume", 1);
    }

    public void SetSfxVolume(float volumeLevel)
    {
        var logVol = Mathf.Log10(volumeLevel) * 20;
        _audioMixer.SetFloat("SFXVolume", logVol);
        PlayerPrefs.SetFloat("Settings_SfxVolume", volumeLevel);
    }
}
