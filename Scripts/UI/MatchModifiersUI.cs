using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.Localization;

public class MatchModifiersUI : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] SlidingTextOption _matchLenghtSelector;
    [SerializeField] SlidingTextOption _listLenghtSelector;
    [SerializeField] SlidingTextOption _npcAmountSelector;
    [SerializeField] Toggle _sameListToggle;
    [SerializeField] Toggle _randomizeStoreToggle;

    [Header("Strings and values")]
    [SerializeField] List<LocalizedString> _matchLenghtOptions;
    [SerializeField] List<EMatchDuration> _matchLengthValues;
    [Space(20f)]
    [SerializeField] List<LocalizedString> _listLenghtOptions;
    [SerializeField] List<EListItemAmount> _listLengthValues;
    [Space(20f)]
    [SerializeField] List<LocalizedString> _npcAmountOptions;
    [SerializeField] List<ENpcAmount> _npcAmountValues;

    private void UpdateMatchDuration(int i) => GameSettings.Current.matchDuration = _matchLengthValues[i];
    private void UpdateItemAmount(int i) => GameSettings.Current.listItemAmount = _listLengthValues[i];
    private void UpdateNpcAmount(int i) => GameSettings.Current.npcAmount = _npcAmountValues[i];
    private void UpdateSameList(bool value) => GameSettings.Current.allPlayersSameList = value;
    private void UpdateShuffleItems(bool value) => GameSettings.Current.shuffleSectionItems = value;

    private void Awake()
    {
        // Set callbacks and options
        _matchLenghtSelector.options = _matchLenghtOptions;
        _matchLenghtSelector.optionChangedCallback = UpdateMatchDuration;

        _listLenghtSelector.options = _listLenghtOptions;
        _listLenghtSelector.optionChangedCallback = UpdateItemAmount;

        _npcAmountSelector.options = _npcAmountOptions;
        _npcAmountSelector.optionChangedCallback = UpdateNpcAmount;

        _sameListToggle.onValueChanged.AddListener(UpdateSameList);

        _randomizeStoreToggle.onValueChanged.AddListener(UpdateShuffleItems);
    }

    private void OnEnable()
    {
        // Set current settings values
        _matchLenghtSelector.ChangeOption(_matchLengthValues.IndexOf(GameSettings.Current.matchDuration), true);
        _listLenghtSelector.ChangeOption(_listLengthValues.IndexOf(GameSettings.Current.listItemAmount), true);
        _npcAmountSelector.ChangeOption(_npcAmountValues.IndexOf(GameSettings.Current.npcAmount), true);
        _randomizeStoreToggle.isOn = GameSettings.Current.shuffleSectionItems;
        _sameListToggle.isOn = GameSettings.Current.allPlayersSameList;
    }
}
