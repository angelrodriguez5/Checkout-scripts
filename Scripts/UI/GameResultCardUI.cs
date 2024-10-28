using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public class GameResultCardUI : MonoBehaviour
{
    [SerializeField] Image _background;
    [SerializeField] Image _playerBanner;
    [SerializeField] Image _playerSticker;
    [SerializeField] LocalizeStringEvent _playerName;
    [SerializeField] GameObject[] _winnerAddons;

    [SerializeField] float _animationDuration;
    [SerializeField] RectTransform _cardContent;
    private Vector2 _targetPosition;
    private Vector2 _initialPosition;

    public void Initialize(PlayerAsset player, Sprite narrowBanner, Sprite background, bool isWinner = false)
    {
        _background.sprite = background;
        _playerBanner.sprite = narrowBanner;
        _playerSticker.sprite = player.PlayerStickerFull;
        _playerName.StringReference = player.PlayerName;

        foreach (var obj in _winnerAddons)
        {
            obj.SetActive(isWinner);
        }
    }

    public void AnimateCard() => StartCoroutine(_AnimateCard());
    private IEnumerator _AnimateCard()
    {
        // Get target position
        _targetPosition = new Vector2(0, 0);
        // The card is outside the screen by default
        _initialPosition = _cardContent.anchoredPosition;

        float elapsedTime = 0;

        while (elapsedTime < _animationDuration)
        {
            float t = elapsedTime / _animationDuration;
            _cardContent.anchoredPosition = Vector2.Lerp(_initialPosition, _targetPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _cardContent.anchoredPosition = _targetPosition;
    }

}