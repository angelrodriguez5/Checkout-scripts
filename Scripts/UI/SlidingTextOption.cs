using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class SlidingTextOption : Selectable
{
    [Space(20f)]
    public List<LocalizedString> options;
    [SerializeField] Image _leftarrowImage;
    [SerializeField] Image _rightarrowImage;
    [SerializeField] List<Graphic> _suplementaryHighlightedGraphics;
    [SerializeField] TMP_Text _text;

    [Header("Audio")]
    [SerializeField] AudioClip _deselectAudioClip;
    [SerializeField] AudioClip _changeOptionAudioClip;

    int _currentIdx;

    public Action<int> optionChangedCallback;

    protected override void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += LocaleChanged;

        base.OnEnable();

        // Set graphic color
        OnDeselect(null);
    }

    protected override void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= LocaleChanged;

        base.OnDisable();
    }

    private void LocaleChanged(Locale obj)
    {
        _text.text = options[_currentIdx].GetLocalizedString();
    }

    public override void OnMove(AxisEventData eventData)
    {
        // On lateral movement change selected setting instead of moving
        if (eventData.moveDir == MoveDirection.Left || eventData.moveDir == MoveDirection.Right)
        {
            // Change selected option
            // Avoid negative offset to avoid negative indexes due to modulus returning negative values
            int offset = eventData.moveDir == MoveDirection.Left ? options.Count - 1 : 1;
            var newIdx = (_currentIdx + offset) % options.Count;
            ChangeOption(newIdx);

            // Animate pressing the corresponding arrow
            if (eventData.moveDir == MoveDirection.Left)
            {
                _leftarrowImage.GetComponent<Animation>().Play();
            }
            else
            {
                _rightarrowImage.GetComponent<Animation>().Play();
            }

            // Consume event
            eventData.Use();
        }
        else
        {
            base.OnMove(eventData);
        }
    }

    public override void OnSelect(BaseEventData eventData)
    {
        targetGraphic = _rightarrowImage;
        base.OnSelect(eventData);
        targetGraphic = _leftarrowImage;
        base.OnSelect(eventData);
        foreach (var graphic in _suplementaryHighlightedGraphics)
        {
            targetGraphic = graphic;
            base.OnSelect(eventData);
        }
    }

    public override void OnDeselect(BaseEventData eventData)
    {
        targetGraphic = _rightarrowImage;
        base.OnDeselect(eventData);
        targetGraphic = _leftarrowImage;
        base.OnDeselect(eventData);
        foreach (var graphic in _suplementaryHighlightedGraphics)
        {
            targetGraphic = graphic;
            base.OnDeselect(eventData);
        }

        // Only play sound when deselecting through navigation, not when deselecting programatically
        if (eventData is AxisEventData)
            AudioManager.Instance?.UiSource.PlayOneShot(_deselectAudioClip);
    }

    public void ChangeOption(int newIdx, bool bypassEvent = false)
    {
        _currentIdx = newIdx;
        if (!bypassEvent)
        {
            optionChangedCallback?.Invoke(_currentIdx);
            AudioManager.Instance?.UiSource.PlayOneShot(_changeOptionAudioClip);
        }

        // Set text
        _text.text = options[_currentIdx].GetLocalizedString();
    }
}
