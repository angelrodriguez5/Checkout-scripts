using System.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public class MatchSettingSelector : Selectable
{
    [System.Serializable]
    private struct GameModeData
    {
        public EGamemode gamemode;
        public LocalizedString name;
        public Sprite thumbnail;
    }

    private enum EMatchSetting
    {
        Theme,
        Gamemode
    }

    [Space(20f)]
    [SerializeField] EMatchSetting _matchSetting;
    [SerializeField] ScrollRect _scrollRect;
    [SerializeField] Transform _scrollRectContentLayout;
    [SerializeField] Image _leftarrowImage;
    [SerializeField] Image _rightarrowImage;
    [SerializeField] List<Graphic> _suplementaryHighlightedGraphics;
    [SerializeField] GameObject _imagePrefab;
    [SerializeField] GameModeData[] _gameModes;

    [Header("Audio")]
    [SerializeField] AudioClip _deselectAudioClip;
    [SerializeField] AudioClip _changeOptionAudioClip;

    bool _isMoving = false;
    int _currentIndex = 0;
    SupermarketTheme[] _supermarketThemes;

    protected override void OnEnable()
    {
        base.OnEnable();

        // Set graphic color
        OnDeselect(null);

        // Set scroller to the right position
        float targetPosition = 0f;
        if (_matchSetting == EMatchSetting.Theme)
        {
            _currentIndex = GameSettings.Current.supermarketTheme == null ? 0 : SupermarketTheme.GetAllThemes().ToList().IndexOf(GameSettings.Current.supermarketTheme) + 1;
            targetPosition = _currentIndex * (1f / _supermarketThemes.Length);
        }
        else if (_matchSetting == EMatchSetting.Gamemode)
        {
            _currentIndex = _gameModes.ToList().FindIndex(x => x.gamemode == GameSettings.Current.gamemode);
            targetPosition = _currentIndex / (_gameModes.Length - 1f);
        }

        _scrollRect.horizontalNormalizedPosition = targetPosition;
        StartCoroutine(InstantScroll(targetPosition));
    }

    protected override void Awake()
    {
        base.Awake();

        // Exit here when in Edit Mode
        if (!Application.isPlaying) return;

        LoadImages();

        // Set view to the first element
        _currentIndex = 0;
        _scrollRect.normalizedPosition = new Vector2(0f, 0f);
    }

    public void LoadImages()
    {
        // Remove placeholders
        foreach (Transform child in _scrollRectContentLayout.transform)
        {
            Destroy(child.gameObject);
        }

        if (_matchSetting == EMatchSetting.Theme)
        {
            // Add "Random" as first element. Should be the default image in _imagePrefab
            Instantiate(_imagePrefab, _scrollRectContentLayout);

            _supermarketThemes = SupermarketTheme.GetAllThemes();
            foreach (var theme in _supermarketThemes)
            {
                var thumbnailObject = Instantiate(_imagePrefab, _scrollRectContentLayout);
                var img = thumbnailObject.GetComponent<Image>();
                img.sprite = theme.ThemeThumbnail;

                var text = thumbnailObject.transform.GetChild(0).GetComponent<LocalizeStringEvent>();
                text.StringReference = theme.ThemeName;
            }

            Canvas.ForceUpdateCanvases();
        }
        else if (_matchSetting == EMatchSetting.Gamemode)
        {
            // In this case Random is configured as an element in the Game Modes array
            foreach (var gamemode in _gameModes)
            {
                var thumbnailObject = Instantiate(_imagePrefab, _scrollRectContentLayout);
                var img = thumbnailObject.GetComponent<Image>();
                img.sprite = gamemode.thumbnail;

                var localizedString = thumbnailObject.transform.GetChild(0).GetComponent<LocalizeStringEvent>();
                localizedString.StringReference = gamemode.name;

                _suplementaryHighlightedGraphics.Add(thumbnailObject.transform.GetChild(0).GetComponent<Graphic>());
            }

            Canvas.ForceUpdateCanvases();
            OnDeselect(null);
        }
    }

    private void SetSupermarketTheme()
    {
        SupermarketTheme chosenTheme;

        // First item in scroll view signifies a random theme
        if (_currentIndex == 0) chosenTheme = null;
        else
        {
            chosenTheme = _supermarketThemes[_currentIndex - 1];
        }

        GameSettings.Current.supermarketTheme = chosenTheme;
    }

    private void SetGameMode()
    {
        GameSettings.Current.gamemode = _gameModes[_currentIndex].gamemode;
    }

    public override void OnMove(AxisEventData eventData)
    {
        // On lateral movement change selected setting instead of moving
        if (eventData.moveDir == MoveDirection.Left || eventData.moveDir == MoveDirection.Right)
        {

            if (!_isMoving)
            {
                // Move scroll
                int offset = eventData.moveDir == MoveDirection.Left ? -1 : 1;
                _isMoving = true;
                StartCoroutine(ScrollRoutine(offset));

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
        foreach(var graphic in _suplementaryHighlightedGraphics)
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

    private IEnumerator ScrollRoutine(int offset)
    {
        // Check index out of bounds
        if (_matchSetting == EMatchSetting.Theme)
        {
            if (_currentIndex + offset >= _supermarketThemes.Length + 1 || _currentIndex + offset < 0)
            {
                _isMoving = false;
                yield break;
            }
        }
        else if (_matchSetting == EMatchSetting.Gamemode)
        {
            if (_currentIndex + offset >= _gameModes.Length || _currentIndex + offset < 0)
            {
                _isMoving = false;
                yield break;
            }
        }

        // Change selected item and save it to GameSettings.Current
        _currentIndex += offset;
        if (_matchSetting == EMatchSetting.Theme)
            SetSupermarketTheme();
        else if (_matchSetting == EMatchSetting.Gamemode)
            SetGameMode();

        // Play audio
        AudioManager.Instance?.UiSource.PlayOneShot(_changeOptionAudioClip);

        // Animate moving to the next position
        float targetPosition = 0f;
        if (_matchSetting == EMatchSetting.Theme)
            targetPosition = _currentIndex * (1f / _supermarketThemes.Length);
        else if (_matchSetting == EMatchSetting.Gamemode)
            targetPosition = _currentIndex * (1f / (_gameModes.Length - 1));

        float t = 0f;
        float speed = 4f;
        while (_scrollRect.horizontalNormalizedPosition != targetPosition)
        {
            _scrollRect.horizontalNormalizedPosition = Mathf.Lerp(_scrollRect.horizontalNormalizedPosition, targetPosition, t);
            Canvas.ForceUpdateCanvases();
            t += speed * Time.deltaTime;
            if (t > 1f)
            {
                _scrollRect.horizontalNormalizedPosition = targetPosition;
                break;
            }
            yield return null;
        }

        Canvas.ForceUpdateCanvases();
        _isMoving = false;
    }

    private IEnumerator InstantScroll(float targetPosition)
    {
        yield return null;
        _scrollRect.horizontalNormalizedPosition = targetPosition;
    }
}