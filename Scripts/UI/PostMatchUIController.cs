using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

public class PostMatchUIController : MonoBehaviour
{
    [System.Serializable]
    private struct PlayerPanelData
    {
        public PlayerColor color;
        public Sprite narrowBanner;
        public Sprite panel;
    }

    [Header("Between matches")]
    [SerializeField] private GameObject _betweenMatchesPanel;
    [SerializeField] private BetweenMatchesCardUI _betweenMatchesCardPrefab;
    [SerializeField] private Transform _betweenMatchesCardLayout;
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private Image _timerImage;

    [Header("Match results")]
    [SerializeField] private CanvasGroup _gameResultsPanel;
    [SerializeField] private GameResultCardUI _gameResultsCardPrefab;
    [SerializeField] private Transform _gameResultCardLayout;

    [Header("Other")]
    [SerializeField] private GameObject _firstSelectedTournament;
    [SerializeField] private GameObject _firstSelectedQuickplay;
    [SerializeField] private GameObject _rematchButton;
    [SerializeField] private PlayerPanelData[] _playerPanelData;

    private List<BetweenMatchesCardUI> _betweenMatchesCards = new List<BetweenMatchesCardUI>();
    private bool _allCardsAnimated = false;

    BetweenMatchesCardUI betweenMatchesWinnerCard;

    private void Awake()
    {
        _betweenMatchesPanel.SetActive(false);
        _gameResultsPanel.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (GameSettings.Current.isTournament)
            _rematchButton.SetActive(false);
    }

    private void Start()
    {
        if (GameSettings.Current.isTournament)
            EventSystem.current.SetSelectedGameObject(_firstSelectedTournament);
        else
            EventSystem.current.SetSelectedGameObject(_firstSelectedQuickplay);
    }

    public void ShowBetweenMatchesUI(Action onTimerEnd = null)
    {
        // Clear the list of previously created cards
        _betweenMatchesCards.Clear();

        foreach (Transform child in _betweenMatchesCardLayout)
            Destroy(child.gameObject);

        // Variable to save the final scale of the card
        Vector3 finalScale = Vector3.zero;

        foreach (var player in GameManager.Instance.Players)
        {
            var card = Instantiate(_betweenMatchesCardPrefab, _betweenMatchesCardLayout);
            var banner = _playerPanelData.Where(x => x.color == player.ColorAsset).First().narrowBanner;

            bool isWinner = player == GameManager.Instance.Winner;
            card.Initialize(player, banner, isWinner);
            if (isWinner)
                betweenMatchesWinnerCard = card;

            // Save the final scale
            finalScale = card.transform.localScale;
            // Set the initial scale of the card to zero before activating it
            card.transform.localScale = Vector3.zero;

            // Add the created card to the list
            _betweenMatchesCards.Add(card);
        }

        _betweenMatchesPanel.SetActive(true);

        // Start the staggered appearance animation
        StartCoroutine(AppearAnimationStaggered(finalScale));

        StartCoroutine(TimerRoutine(onTimerEnd));
    }

    private IEnumerator AppearAnimationStaggered(Vector3 finalScale)
    {
        float delayBetweenAnimations = 0.2f;
        for (int i = 0; i < _betweenMatchesCards.Count; i++)
        {
            _betweenMatchesCards[i].gameObject.SetActive(true);
            _betweenMatchesCards[i].StartAppearAnimation(finalScale);
            yield return new WaitForSeconds(delayBetweenAnimations);
        }

        // Set the flag indicating that all cards have completed their animations so we can start animating the crown
        _allCardsAnimated = true;
    }

    public void ShowGameResultsUI()
    {
        foreach (Transform child in _gameResultCardLayout)
            Destroy(child.gameObject);

        // Create a list of cards to store the created cards
        List<GameResultCardUI> resultCards = new();

        foreach (var player in GameManager.Instance.Players)
        {
            var card = Instantiate(_gameResultsCardPrefab, _gameResultCardLayout);
            var data = _playerPanelData.Where(x => x.color == player.ColorAsset).First();
            card.Initialize(player, data.narrowBanner, data.panel, player == GameManager.Instance.Winner);

            // Add the newly created card to the list
            resultCards.Add(card);
        }

        StartCoroutine(_gameResultsPanel.FadeIn());

        // Call the staggered animation
        StartCoroutine(AnimateResultCards(resultCards));

        if (GameSettings.Current.isTournament)
        {
            _rematchButton.SetActive(false);
            EventSystem.current.SetSelectedGameObject(_firstSelectedTournament);
        }
        else
            EventSystem.current.SetSelectedGameObject(_firstSelectedQuickplay);
    }

    private IEnumerator AnimateResultCards(List<GameResultCardUI> cards)
    {
        float delayBetweenAnimations = 0.2f;

        // Wait one frame so all cards are active
        yield return null;
        yield return null;

        // Activate cards in sequence
        foreach (var card in cards)
        {
            card.AnimateCard();
            yield return new WaitForSeconds(delayBetweenAnimations);
        }
    }

    public void RematchButtonPressed()
    {
        GameManager.Instance?.Rematch();
    }

    public void ExitButtonPressed()
    {
        GameManager.Instance?.ExitMatch();
    }

    private IEnumerator TimerRoutine(Action onTimerEnd)
    {
        float timer = 5f;
        bool animationPlayed = false;

        while (!_allCardsAnimated)
        {
            yield return null;
        }

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            _timerText.text = $"{Math.Ceiling(timer)}";
            _timerImage.fillAmount = 1 - (timer % 1);  // fill in 1 revolution per second
            yield return null;

            // Animate the crown that was won this round
            if (!animationPlayed && timer < 4f)
            {
                betweenMatchesWinnerCard.AnimateWinnerCrown();
                animationPlayed = true;
            }
        }

        _timerText.text = "";
        _timerImage.gameObject.SetActive(false);

        onTimerEnd?.Invoke();
    }

}
