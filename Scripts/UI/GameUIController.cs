using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class GameUIController : MonoBehaviour
{
    [Serializable]
    private struct GamemodeTutorialData
    {
        public EGamemode gamemode;
        public CanvasGroup canvasGroup;
        public GameObject prompt;
    }

    [Header("Prefabs")]
    [SerializeField] private GameObject _playerItemListPrefab;
    [SerializeField] private GameObject _playerItemListCovid;
    [SerializeField] private GameObject _playerItemListSequential;
    [SerializeField] private GameObject _tiebreakerStickerPrefab;

    [Header("Config")]
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private Transform _tiebreakerLayout;
    [SerializeField] private Transform _playerListLayout;
    [SerializeField] private PostMatchUIController _postMatchUIController;
    [SerializeField] private AnimatedTimer _animatedTimer;
    [SerializeField] private GamemodeTutorialData[] _gamemodeTutorials;

    [Header("Stickers")]
    [SerializeField] public GameObject readySticker;
    [SerializeField] public GameObject setSticker;
    [SerializeField] public GameObject goSticker;
    [SerializeField] public GameObject timesUpSticker;
    [SerializeField] public GameObject memoryModeSticker;
    [SerializeField] public GameObject tiebreakerSticker;

    [Header("Covid mode")]
    [SerializeField] private GameObject _toiletPaperCounterPanel;
    [SerializeField] private TMP_Text _toiletPaperCounterText;

    private Dictionary<PlayerAsset, GameObject> _playerListObjects = new Dictionary<PlayerAsset, GameObject>();

    int _toiletPaperAmount;

    private void Awake()
    {
        foreach (var tutorial in _gamemodeTutorials)
        {
            tutorial.canvasGroup.gameObject.SetActive(false);
        }

        _toiletPaperCounterPanel.SetActive(false);
    }

    private void Start()
    {
        GameManager.onGamePaused += HideUIOnPause;
        GameManager.onItemDelivered += UpdateToiletPaperCounter;
    }

    private void OnDestroy()
    {
        GameManager.onGamePaused -= HideUIOnPause;
        GameManager.onItemDelivered += UpdateToiletPaperCounter;
    }

    private void HideUIOnPause(bool value)
    {
        gameObject.SetActive(!value);
    }

    private void UpdateToiletPaperCounter(PlayerAsset player, ItemAsset item)
    {
        if (GameSettings.Current.matchGamemode == EGamemode.Pandemic)
        {
            _toiletPaperAmount--;
            _toiletPaperCounterText.text = $"{_toiletPaperAmount}";
        }
    }

    public void AddPlayerShoppingList(PlayerAsset player, List<ItemAsset> items)
    {
        var instance = Instantiate(_playerItemListPrefab, _playerListLayout);
        instance.GetComponent<IPlayerItemList>().Initialise(player, items);

        _playerListObjects.Add(player, instance);
    }

    public void AddPlayerShoppingListPandemic(PlayerAsset player)
    {
        var instance = Instantiate(_playerItemListCovid, _playerListLayout);
        instance.GetComponent<IPlayerItemList>().Initialise(player, null);

        _playerListObjects.Add(player, instance);
    }

    public void AddPlayerShoppingListSequential(PlayerAsset player, List<ItemAsset> items)
    {
        var instance = Instantiate(_playerItemListSequential, _playerListLayout);
        instance.GetComponent<IPlayerItemList>().Initialise(player, items);

        _playerListObjects.Add(player, instance);
    }

    /// <summary>
    /// Hides player's shopping lists and timer, then displays winner text and rematch/exit buttons
    /// </summary>
    public void ShowPostMatchUI()
    {
        // Hide lists and timer
        _playerListLayout.gameObject.SetActive(false);

        // If it is a tournament
        if (GameSettings.Current.isTournament)
        {
            // Show between matches ui
            // If it was the last match transition to post game UI, else play next match
            Action action;
            if (GameSettings.Current.tournamentPlayerWins.Any(kv => kv.Value == GameSettings.Current.tournamentWinsTarget))
                action = _postMatchUIController.ShowGameResultsUI;
            else
                action = GameManager.Instance.NextMatch;

            _postMatchUIController.ShowBetweenMatchesUI(action);
        }
        else
        {
            // Not a tournament
            _postMatchUIController.ShowGameResultsUI();
        }
    }

    public void ConfigureTimer(int duration, Action onTimerFinished)
    {
        _animatedTimer.TargetSeconds = duration;
        _animatedTimer.DoOnTimerFinish = onTimerFinished;
        _animatedTimer.TextDisplay = _timerText;
        _animatedTimer.Regressive = true;
        _animatedTimer.UpdateUI();
        _animatedTimer.Active = false;

        if (duration <= 0)
        {
            _timerText.text = "\u221E";  // Infinity symbol
        }
    }

    public void HideTimer()
    {
        _timerText.transform.parent.gameObject.SetActive(false);
    }

    public void ShowTimer()
    {
        _timerText.transform.parent.gameObject.SetActive(true);
    }

    public void StartTimer()
    {
        if (_animatedTimer.TargetSeconds > 0)
            _animatedTimer.Active = true;
    }

    public void StopTimer()
    {
        _animatedTimer.Active = false;
    }

    public void HidePlayerShoppingList(PlayerAsset player)
    {
        var list = _playerListObjects[player];
        if (list)
            list.SetActive(false);
    }

    public void HideAllPlayersShoppingLists()
    {
        _playerListObjects.Values.Map(x => x.SetActive(false));
    }

    public void ShowAllPlayersShoppingLists()
    {
        _playerListObjects.Values.Map(x => x.SetActive(true));

        var sequence = DOTween.Sequence();

        float seqPosition = 0;
        foreach(var list in _playerListObjects.Values)
        {
            // Punch each transform into existence
            RectTransform rectTransform = list.transform as RectTransform;
            Vector3 punchDir = new Vector3(0,0,0);
            var tween = rectTransform.DOScale(Vector3.zero, 0.2f).From();

            // Start moving next list after the previous is half way done
            sequence.Insert(seqPosition, tween);
            seqPosition += 0.3f;
        }

        sequence.Play();
    }

    public void ShowPlayerShoppingList(PlayerAsset player)
    {
        var list = _playerListObjects[player];
        if (list)
            list.SetActive(true);
    }

    public void ShowPlayerShoppingList(PlayerAsset player, float time) => StartCoroutine(_ShowPlayerShoppingList(player, time));
    private IEnumerator _ShowPlayerShoppingList(PlayerAsset player, float time)
    {
        var list = _playerListObjects[player];
        if (list)
        {
            list.SetActive(true);
            yield return new WaitForSeconds(time);
            list.SetActive(false);
        }
    }

    public void ForceReloadItemLists()
    {
        foreach (var entry in _playerListObjects)
        {
            var list = entry.Value.GetComponent<IPlayerItemList>();
            list.UpdateUI();
        }
    }


    #region Stickers
    /// <summary>
    /// Shows a tiebreaker sticker with the faces of these players
    /// </summary>
    /// <param name="players"></param>
    public void ShowTiebreakerSticker(IEnumerable<PlayerAsset> players)
    {
        foreach (var player in players)
        {
            var instance = Instantiate(_tiebreakerStickerPrefab, _tiebreakerLayout);
            instance.GetComponent<Image>().sprite = player.PlayerStickerHead;
        }

        tiebreakerSticker.SetActive(true);
    }

    public void HideTiebreakerSticker()
    {
        tiebreakerSticker.SetActive(false);
    }

    public void ShowReadySticker()
    {
        readySticker.SetActive(true);
        readySticker.transform.DOScale(Vector3.zero, 0.5f).From();
    }

    public void HideReadySticker()
    {
        readySticker.SetActive(false);
    }

    public void ShowSetSticker()
    {
        setSticker.SetActive(true);
        setSticker.transform.DOScale(Vector3.zero, 0.5f).From();
    }

    public void HideSetSticker()
    {
        setSticker.SetActive(false);
    }

    public void ShowGoSticker()
    {
        goSticker.SetActive(true);
        goSticker.transform.DOScale(Vector3.zero, 0.5f).From();
    }

    public void HideGoSticker()
    {
        goSticker.SetActive(false);
    }
    #endregion

    public void ShowGamemodeTutorial()
    {
        var tutorial = _gamemodeTutorials.First(x => x.gamemode == GameSettings.Current.matchGamemode);
        tutorial.canvasGroup.gameObject.SetActive(true);
        tutorial.canvasGroup.alpha = 1;
        tutorial.prompt.SetActive(false);
    }

    public void HideGamemodeTutorial()
    {
        var tutorial = _gamemodeTutorials.First(x => x.gamemode == GameSettings.Current.matchGamemode);

        StartCoroutine(tutorial.canvasGroup.FadeOut());
    }

    public void ShowGamemodeTutorialPrompt()
    {
        var tutorial = _gamemodeTutorials.First(x => x.gamemode == GameSettings.Current.matchGamemode);
        tutorial.prompt.SetActive(true);
    }

    public void HideGamemodeTutorialPrompt()
    {
        var tutorial = _gamemodeTutorials.First(x => x.gamemode == GameSettings.Current.matchGamemode);
        tutorial.prompt.SetActive(false);
    }

    public void InitializeCovidToiletPaperCounter(int amount)
    {
        _toiletPaperAmount = amount;
        _toiletPaperCounterText.text = $"{_toiletPaperAmount}";
        _toiletPaperCounterPanel.SetActive(true);
    }
}
