using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(menuName = "PartyGame/Theme")]
public class SupermarketTheme : ScriptableObject
{
    [Serializable]
    public struct SupermarketLayout
    {
        // Cant use SceneAsset outside editor, so we save the scene name in a separate field.
        // Autocompleted in OnValidate
        #if UNITY_EDITOR
        public SceneAsset scene;
        #endif
        [HideInInspector] public string sceneName;
        public Sprite thumbnail;
    }

    [SerializeField] private LocalizedString _themeName;
    [SerializeField] private Sprite _themeThumbnail;
    [SerializeField] private ItemSet _themeItemSet;
    [SerializeField] private AudioClip _musicTrack;
    [SerializeField] private AudioClip _ambienceTrack;
    [SerializeField] private ItemAsset _covidModeItem;
    [SerializeField] private SupermarketLayout[] _supermarkets;

    public LocalizedString ThemeName => _themeName;
    public Sprite ThemeThumbnail => _themeThumbnail;
    public ItemSet ThemeItemSet => _themeItemSet;
    public AudioClip MusicTrack => _musicTrack;
    public AudioClip AmbienceTrack => _ambienceTrack;
    public ItemAsset CovidModeItem => _covidModeItem;
    public SupermarketLayout[] Supermarkets => _supermarkets;

    public static SupermarketTheme[] GetAllThemes()
    {
        return Resources.LoadAll<SupermarketTheme>(@"Themes");
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        for (int i = 0; i < _supermarkets.Length; i++)
        {
            _supermarkets[i].sceneName = _supermarkets[i].scene != null ? _supermarkets[i].scene.name : "";
        }
    }
    #endif
}
