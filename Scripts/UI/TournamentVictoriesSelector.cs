using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TournamentVictoriesSelector : Selectable
{
    [Header("Options and values")]
    [Tooltip("The graphics must be ordered from left to right as they appear on the screen")]
    [SerializeField] Graphic[] _optionsGraphics;
    [SerializeField] int[] _optionsValues;

    [SerializeField] Graphic[] _supplementaryGraphics;

    [Header("Audio")]
    [SerializeField] AudioClip _deselectAudioClip;
    [SerializeField] AudioClip _changeOptionAudioClip;

    int _currentIndex = 0;

    protected override void Start()
    {
        base.Start();

        // Exit here when in Edit Mode
        if (!Application.isPlaying) return;
    }

    protected override void OnEnable()
    {
        // Set graphic color
        OnDeselect(null);

        // Deselect all options
        foreach (var graphic in _optionsGraphics)
        {
            targetGraphic = graphic;
            base.OnDeselect(null);
        }

        // Select the corresponding option, or default to the fist one
        _currentIndex = Array.IndexOf(_optionsValues, GameSettings.Current.tournamentWinsTarget);

        if (_currentIndex < 0) _currentIndex = 0;
        targetGraphic = _optionsGraphics[_currentIndex];
        base.OnSelect(null);

        // Update game settings
        GameSettings.Current.tournamentWinsTarget = _optionsValues[_currentIndex];
    }

    public override void OnMove(AxisEventData eventData)
    {
        // On lateral movement change selected setting instead of moving
        if (eventData.moveDir == MoveDirection.Left || eventData.moveDir == MoveDirection.Right)
        {
            int offset = eventData.moveDir == MoveDirection.Left ? -1 : 1;

            // Check index out of bounds
            if (_currentIndex + offset >= _optionsGraphics.Length || _currentIndex + offset < 0)
            {
                return;
            }

            // Deselect previous option
            targetGraphic = _optionsGraphics[_currentIndex];
            base.OnDeselect(eventData);

            // Select the new one
            _currentIndex += offset;
            targetGraphic = _optionsGraphics[_currentIndex];
            base.OnSelect(eventData);

            // Play audio
            AudioManager.Instance?.UiSource.PlayOneShot(_changeOptionAudioClip);

            // Update game settings
            GameSettings.Current.tournamentWinsTarget = _optionsValues[_currentIndex];
        }
        else
        {
            base.OnMove(eventData);
        }
    }

    public override void OnDeselect(BaseEventData eventData)
    {
        foreach(var graphic in _supplementaryGraphics)
        {
            targetGraphic = graphic;
            base.OnDeselect(eventData);
        }

        // Only play sound when deselecting through navigation, not when deselecting programatically
        if (eventData is AxisEventData)
            AudioManager.Instance?.UiSource.PlayOneShot(_deselectAudioClip);
    }

    public override void OnSelect(BaseEventData eventData)
    {
        foreach (var graphic in _supplementaryGraphics)
        {
            targetGraphic = graphic;
            base.OnSelect(eventData);
        }
    }
}
