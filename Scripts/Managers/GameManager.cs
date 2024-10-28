using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum EGameFinishReason
{
    TimerFinished,
    ListComplete
}

/// <summary>
/// Class that orchestrates the flow of the game
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private NPCManager _npcManager;
    [SerializeField] private GameObject _playerPrefab;

    [Header("Cameras")]
    [SerializeField] private CinemachineVirtualCamera _gameVCam;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _matchEndWinnerSound;
    [SerializeField] private AudioClip _matchEndTieSound;
    [SerializeField] private AudioClip _timerFinishedSound;
    [SerializeField] private AudioClip _startCountdownSound;

    [Header("UI")]
    [SerializeField] private GameUIController _gameUI;
    [SerializeField] private LoadingScreen _loadingScreen;

    private List<PlayerController> _playerControllers = new List<PlayerController>();
    private GameSettings _gameSettings;
    private PlayerInputManager _playerInputManager;
    private PlayerInput _currentPlayerControllingUI;
    private bool _tutorialDisplayed;
    private bool _playersReady;
    private bool _isEndOfMatchRoutine;

    // Static Events
    public static event Action<PlayerAsset, ItemAsset> onItemDelivered;
    public static event Action onMatchStarted;
    public static event Action onMatchTiebreaker;
    public static event Action onMatchFinished;
    public static event Action onLayoutShowcaseStarted;
    public static event Action onLayoutShowcaseFinished;
    public static event Action<bool> onGamePaused;
    public static event Action<PlayerInput> onCurrentPlayerControllingUIChanged;

    public Dictionary<PlayerAsset, List<ItemAsset>> playerShoppingLists = new Dictionary<PlayerAsset, List<ItemAsset>>();
    public Dictionary<PlayerAsset, int> playerCovidItemsTurnedIn = new Dictionary<PlayerAsset, int>();

    // Properties
    public bool IsGamePaused { get; private set; } = false;
    public bool IsMatchStarted { get; private set; } = false;
    public bool IsMatchFinished { get; private set; } = false;
    public bool IsTiebreaker { get; private set; } = false;
    public int NumberOfPlayers { get; private set; }
    public int PandemicRemaingToiletPaper { get; private set; }
    public EGameFinishReason GameFinishReason { get; private set; }
    public PlayerAsset Winner { get; private set; }
    public List<PlayerAsset> Players { get; private set; }
    public Camera MainCamera { get; private set; }
    public GameSettings GameSettings => _gameSettings;
    public GameUIController GameUI => _gameUI;
    public PlayerInput CurrentPlayerControllingUI 
    {
        get => _currentPlayerControllingUI;
        set
        {
            _currentPlayerControllingUI = value;
            onCurrentPlayerControllingUIChanged?.Invoke(value);
        }
    }

    // Singleton
    public static GameManager Instance { get; protected set; }

    private void Awake()
    {
        // Singleton
        if (Instance != null)
            throw new Exception("Several game managers in scene");
        else
            Instance = this;

        // Game State
        GameState.SetState(EGameState.Game);

        // Reference to camera
        MainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        // Get game settings
        _gameSettings = GameSettings.Current;

        // Configure PlayerInputManager to manually join players
        _playerInputManager = GetComponent<PlayerInputManager>();
        _playerInputManager.playerPrefab = _playerPrefab;

        // Start with loading screen active
        _loadingScreen.InstantIn();

        // Set current match gamemode
        if (_gameSettings.isTournament)
        {
            _gameSettings.matchGamemode = _gameSettings.tournamentGamemodes[_gameSettings.tournamentMatchIndex];
        }
        else
        {
            if (_gameSettings.gamemode == EGamemode.Random)
            {
                var gamemodes = Enum.GetValues(typeof(EGamemode)).Cast<EGamemode>().ToList();
                gamemodes.Remove(EGamemode.Random);
                _gameSettings.matchGamemode = gamemodes.GetRandomElement();
            }
            else
                _gameSettings.matchGamemode = _gameSettings.gamemode;
        }
    }

    private void Start()
    {
        StartCoroutine(GameSetupRoutine());
    }

    public void TogglePause(bool dontInvoke = false)
    {
        IsGamePaused = !IsGamePaused;
        if (IsGamePaused)
        {
            Time.timeScale = 0;
            AudioListener.pause = true;
        }
        else
        {
            Time.timeScale = 1;
            AudioListener.pause = false;
        }

        if(!dontInvoke)
            onGamePaused?.Invoke(IsGamePaused);
    }

    public bool TryDeliverItem(PlayerAsset player, ItemAsset item)
    {
        // In covid mode all items count
        if (GameSettings.Current.matchGamemode == EGamemode.Pandemic)
        {
            PandemicRemaingToiletPaper--;
            playerCovidItemsTurnedIn[player]++;
            onItemDelivered?.Invoke(player, item);

            // End the game when out of stock
            if (PandemicRemaingToiletPaper <= 0)
                EndMatch(EGameFinishReason.ListComplete);

            return true;
        }

        // In sequential mode only the last item in the list can be turned in
        if (GameSettings.Current.matchGamemode == EGamemode.Sequential)
        {
            var items = GetRemainingItemsForPlayer(player);

            // Deal items always count
            int i;
            if (item.ItemCategory == EItemCategory.DealItems)
                i = items.Count - 1;
            else
                i = items.IndexOf(item);

            // Check that the turned in item is the last of the list
            if (i == items.Count - 1)
            {
                items.RemoveAt(i);
                onItemDelivered?.Invoke(player, item);
                return true;
            }
            else
                return false;
        }

        if (!playerShoppingLists.TryGetValue(player, out var playerItems)) return false;

        // Deal items
        var dealItem = item as DealItem;
        if (dealItem != null)
        {
            // Select which items will be turned in depending on the deal item type
            List<ItemAsset> items = new List<ItemAsset>();
            switch (dealItem.DealType)
            {
                case EDealType.OneItem:
                    items.Add(playerItems.GetRandomElement());
                    break;
                case EDealType.TwoItems:
                    items.AddRange(playerItems.Shuffle().Take(2));
                    break;
                default:
                    break;
            }

            onItemDelivered?.Invoke(player, item);
            // Deliver the selected items
            items.Map(x => TryDeliverItem(player, x));

            return true;
        }

        // Normal items
        if (playerItems.Contains(item))
        {
            playerItems.Remove(item);
            onItemDelivered?.Invoke(player, item);

            // If it was the last item of that player end the game
            if(playerItems.Count == 0)
            {
                EndMatch(EGameFinishReason.ListComplete);
            }
            return true;
        }
        else
        {
            return false;
        }

    }

    public List<ItemAsset> GetRemainingItemsForPlayer(PlayerAsset player)
    {
        // Beware: passing a reference to the original list instead of a copy
        if (playerShoppingLists.ContainsKey(player))
            return playerShoppingLists[player];
        else
            return null;
    }

    public void EndMatch(EGameFinishReason reason)
    {
        if (!_isEndOfMatchRoutine)
            StartCoroutine(EndMatchRoutine(reason));
    }

    public void Rematch()
    {
        // Get theme from game settings
        var supermarketTheme = _gameSettings.supermarketTheme;

        // Random theme and supermarket
        if (supermarketTheme == null)
            supermarketTheme = SupermarketTheme.GetAllThemes().GetRandomElement();

        // Reload scene
        SceneManager.LoadScene(supermarketTheme.Supermarkets.GetRandomElement().sceneName);
    }

    public void NextMatch()
    {
        Debug.Log("Next match!");
        StartCoroutine(NextMatchRoutine());
    }

    public void ExitMatch()
    {
        // Unpause before loading main menu
        if (IsGamePaused) TogglePause();

        StartCoroutine(ExitMatchRoutine());
    }

    protected virtual void SpawnPlayers()
    {
        // Gather all active players
        List<PlayerAsset> playerAssets;
        if (_gameSettings.isTest)
        {
            // if is test spawn one player with keyboard no matter what
            playerAssets = Resources.LoadAll<PlayerAsset>("Players").OrderBy(x => x.Id).Take(1).ToList();
            playerAssets[0].ControlScheme = "KeyboardWASD";
            playerAssets[0].Device = Keyboard.current;
        }
        else
            playerAssets = Resources.LoadAll<PlayerAsset>("Players").Where(x => x.Active).OrderBy(x => x.Id).ToList();

        // Set number of players
        NumberOfPlayers = playerAssets.Count;
        Players = playerAssets.ToList();

        // Spawn and configure each active player
        for (int i = 0; i < playerAssets.Count; i++)
        {
            var asset = playerAssets[i];
            var spawn = Supermarket.Instance.SpawnPositions[i];

            // Spawn player
            var playerInput = _playerInputManager.JoinPlayer(playerIndex: asset.Id, controlScheme: asset.ControlScheme, pairWithDevice: asset.Device);

            // Load player asset which determines customization and input system configuration
            var playerController = playerInput.GetComponent<PlayerController>();
            playerController.LoadPlayerAsset(asset);

            // Move player to its spawn position
            playerController.Teleport(spawn);

            // Add to player list
            _playerControllers.Add(playerController);

            // Subscribe to action for skipping the tutorial
            playerInput.actions.FindAction("Submit").performed += PlayerSkipTutorial;
        }
    }

    private void PlayerSkipTutorial(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        if (_tutorialDisplayed) _playersReady = true;
    }

    private void GeneratePlayerShoppingLists()
    {
        switch (GameSettings.Current.matchGamemode)
        {
            // Shouldn't happen
            case EGamemode.Random:
                break;

            case EGamemode.TimeAttack:
            case EGamemode.Memory:
            case EGamemode.Sequential:
                // Supermarket generates the lists based on item rarity
                var shoppingLists = Supermarket.Instance.GeneratePlayerShoppingLists(_playerControllers.Count(), (int)_gameSettings.listItemAmount);

                // Assign each list to a player and initialise its UI
                for (int i = 0; i < _playerControllers.Count; i++)
                {
                    var player = _playerControllers[i];
                    var list = shoppingLists[i];
                    playerShoppingLists.Add(player.PlayerAsset, list);
                    if (GameSettings.Current.matchGamemode == EGamemode.Sequential)
                        _gameUI.AddPlayerShoppingListSequential(player.PlayerAsset, list);
                    else
                        _gameUI.AddPlayerShoppingList(player.PlayerAsset, list);
                }
                break;

            case EGamemode.Pandemic:
                // Initialise player counters
                playerCovidItemsTurnedIn.Clear();
                for (int i = 0; i < _playerControllers.Count; i++)
                {
                    var player = _playerControllers[i];
                    playerCovidItemsTurnedIn.Add(player.PlayerAsset, 0);
                    _gameUI.AddPlayerShoppingListPandemic(player.PlayerAsset);
                }
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Winner is the player with the least items remaining
    /// if several players have the same amount if items there is a tie and nobody wins
    /// </summary>
    private void DeclareWinner()
    {
        PlayerAsset winner = null;
        bool tie = true;
        int winnerItemCount;

        if (GameSettings.Current.matchGamemode == EGamemode.Pandemic)
        {
            // Pandemic mode
            // The winner is the one with the most toilet paper
            winnerItemCount = -1;
            foreach (var entry in playerCovidItemsTurnedIn)
            {
                if (entry.Value > winnerItemCount)
                {
                    winner = entry.Key;
                    winnerItemCount = entry.Value;
                    tie = false;
                }
                else if (entry.Value == winnerItemCount)
                {
                    tie = true;
                }
            }
        }
        else
        {
            // Every other mode
            // The winner is the one with the least items remaining in their list
            winnerItemCount = 9999;
            foreach (var entry in playerShoppingLists)
            {
                if (entry.Value.Count < winnerItemCount)
                {
                    winner = entry.Key;
                    winnerItemCount = entry.Value.Count;
                    tie = false;
                }
                else if (entry.Value.Count == winnerItemCount)
                {
                    tie = true;
                }
            }

        }

        if (tie)
            Winner = null;
        else
            Winner = winner;
    }

    private IEnumerator GameSetupRoutine()
    {
        _gameUI.HideTimer();

        // First show gamemode tutorial
        _gameUI.ShowGamemodeTutorial();

        // Load players
        SpawnPlayers();
        yield return new WaitForEndOfFrame();

        // Instantiate items in the supermarket
        Supermarket.Instance.SpawnItems();
        yield return new WaitForEndOfFrame();

        // Player shopping lists
        GeneratePlayerShoppingLists();
        // Wait for the layout to calculate the shopping list starting positions and then hide them
        yield return new WaitForEndOfFrame();
        _gameUI.HideAllPlayersShoppingLists();

        // Initialise navmesh and npcs
        _npcManager.Initialise();
        _npcManager.SpawnNPCs((int)_gameSettings.npcAmount - NumberOfPlayers);

        // Timer
        _gameUI.ConfigureTimer((int)_gameSettings.matchDuration, () => EndMatch(EGameFinishReason.TimerFinished));

        // Remove loading screen
        yield return null;
        yield return null;
        yield return _loadingScreen.FadeOut();

        // Start initial countdown
        StartCoroutine(PreMatchRoutine());
    }

    private IEnumerator PreMatchRoutine()
    {
        #if !UNITY_EDITOR
            // wait for people to read the tutorial
            yield return new WaitForSeconds(3f);
        #endif

        // Display "play" prompt on the corresponding tutorial screen
        _gameUI.ShowGamemodeTutorialPrompt();

        // Wait for button press
        _tutorialDisplayed = true;
        while (!_playersReady)
            yield return null;

        // Unsubscribe event for skipping the tutorial
        foreach (var player in _playerControllers)
        {
            player.PlayerInput.actions.FindAction("Submit").performed -= PlayerSkipTutorial;
        }

        // Fade out tutorial screen
        _gameUI.HideGamemodeTutorial();

        // Start ambience sound
        StartCoroutine(AudioManager.Instance.AmbienceSource.FadeIn(1f));

        // Showcase important parts of the layout
        onLayoutShowcaseStarted?.Invoke();
        yield return new WaitForSeconds(4f);
        onLayoutShowcaseFinished?.Invoke();

        // Show UI
        _gameUI.ShowTimer();
        _gameUI.ShowAllPlayersShoppingLists();
        if (GameSettings.Current.matchGamemode == EGamemode.Pandemic)
        {
            PandemicRemaingToiletPaper = Supermarket.Instance.GetToiletPaperAmount();
            _gameUI.InitializeCovidToiletPaperCounter(PandemicRemaingToiletPaper);
        }

        // Wait for lists appear animation
        yield return new WaitForSeconds(0.8f + 0.2f * NumberOfPlayers);

        // Memory gamemode warning
        if (_gameSettings.matchGamemode == EGamemode.Memory)
        {
            _gameUI.memoryModeSticker.SetActive(true);
            yield return new WaitForSeconds(GameSettings.MEMORY_LIST_TIME_BEFORE_MATCH);
            _gameUI.memoryModeSticker.SetActive(false);
            _gameUI.HideAllPlayersShoppingLists();
        }

        // Game start sequence
        AudioManager.Instance.EffectSource.PlayOneShot(_startCountdownSound);
        _gameUI.ShowReadySticker();
        yield return new WaitForSeconds(1f);
        _gameUI.HideReadySticker();
        _gameUI.ShowSetSticker();
        yield return new WaitForSeconds(1f);
        _gameUI.HideSetSticker();
        _gameUI.ShowGoSticker();

        // Start match
        Supermarket.Instance.SpawnProtectionObstacle.enabled = false;
        IsMatchStarted = true;
        onMatchStarted?.Invoke();
        StartCoroutine(AudioManager.Instance.MusicSource.FadeIn(2f));
        _gameUI.StartTimer();

        // Keep the text "Go!" another second on the screen
        yield return new WaitForSeconds(1f);
        _gameUI.HideGoSticker();
    }

    private IEnumerator EndMatchRoutine(EGameFinishReason reason)
    {
        _isEndOfMatchRoutine = true;

        // Stop NPCs
        _npcManager.StopNPCs();

        // Stop Players
        foreach (var player in _playerControllers)
        {
            player.DisableMovement(true);
        }

        // Signal all delivery areas to finish processing their items
        foreach (var area in DeliveryArea.allAreas)
        {
            area.ForceFinishProcessing();
        }

        // Show end of match text
        if (_gameSettings.matchGamemode == EGamemode.Memory)
        {
            foreach (var player in Players)
                _gameUI.ShowPlayerShoppingList(player);

            _gameUI.HideTimer();
            _gameUI.timesUpSticker.SetActive(true);
            yield return new WaitForSeconds(3f);
            _gameUI.timesUpSticker.SetActive(false);
        }
        else
        {
            _gameUI.HideTimer();
            _gameUI.timesUpSticker.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            _gameUI.timesUpSticker.SetActive(false);
        }

        // Wait for all delivery areas to finish processing before declaring the winner
        foreach (var area in DeliveryArea.allAreas)
        {
            yield return area.ProcessQueueRoutine;
        }

        yield return new WaitForSeconds(0.3f);

        // Determine winner
        DeclareWinner();

        // Tiebreaker
        if (Winner == null)
        {
            StartCoroutine(TiebreakerRoutine());
            _isEndOfMatchRoutine = false;
            yield break;  // Break out of this coroutine
        }

        // Normal end of match
        _gameUI.StopTimer();
        if (GameSettings.Current.isTournament)
        {
            GameSettings.Current.tournamentPlayerWins[Winner]++;
        }
        IsMatchFinished = true;
        GameFinishReason = reason;
        onMatchFinished?.Invoke();
        yield return new WaitForSeconds(0.5f);

        // Fade out music and play timer finished sound
        StartCoroutine(AudioManager.Instance.MusicSource.FadeOut(0.4f));
        StartCoroutine(AudioManager.Instance.AmbienceSource.FadeOut(0.4f));
        AudioManager.Instance.EffectSource.PlayOneShot(_timerFinishedSound);

        // Show matches won or tournament/quickplay results
        _gameUI.ShowPostMatchUI();

        // Play the corresponding match end sound
        if (Winner != null)
            _audioSource.PlayOneShot(_matchEndWinnerSound);
        else
            _audioSource.PlayOneShot(_matchEndTieSound);
  
        // Winner dance
        var winner = _playerControllers.FirstOrDefault(x => x.PlayerAsset == Winner);
        if (winner) winner.Cheer();

        _isEndOfMatchRoutine = false;
    }

    private IEnumerator TiebreakerRoutine()
    {
        IsTiebreaker = true;
        onMatchTiebreaker?.Invoke();

        List<PlayerAsset> tiedPlayers;
        // Get tied players and setup tiebreaker for each of the gamemodes
        if (GameSettings.Current.matchGamemode == EGamemode.Pandemic)
        {
            var tiedPlayersItems = playerCovidItemsTurnedIn.Max(x => x.Value);
            tiedPlayers = playerCovidItemsTurnedIn.Where(entry => entry.Value == tiedPlayersItems).Select(entry => entry.Key).ToList();

            yield return Supermarket.Instance.ClearAllItems();
            yield return null;
            Supermarket.Instance.SpawnCovidTiebreakerItems(tiedPlayers.Count);

            // Update UI
            PandemicRemaingToiletPaper = Supermarket.Instance.GetToiletPaperAmount();
            _gameUI.InitializeCovidToiletPaperCounter(PandemicRemaingToiletPaper);
        }
        else
        {
            var tiedPlayersItems = playerShoppingLists.Select(entry => entry.Value.Count).Min();
            tiedPlayers = playerShoppingLists.Where(entry => entry.Value.Count == tiedPlayersItems).Select(entry => entry.Key).ToList();

            // Put the same item on all players' list
            var item = playerShoppingLists[tiedPlayers.GetRandomElement()].GetRandomElement();
            foreach (var player in tiedPlayers)
            {
                playerShoppingLists[player] = new List<ItemAsset> {item};
            }

            // Update UI
            _gameUI.ForceReloadItemLists();

            foreach (var player in tiedPlayers)
            {
                _gameUI.ShowPlayerShoppingList(player);
            }
        }


        // Stop npcs and move players
        _npcManager.StopNPCs();
        int spawnIndex = 0;
        foreach(var player in _playerControllers)
        {
            // Disable all player movement
            player.DisableMovement(dropItem: true);

            if (tiedPlayers.Contains(player.PlayerAsset))
            {
                // Teleport tied players to the beginning of the level, next to each other
                player.Teleport(Supermarket.Instance.SpawnPositions[spawnIndex]);
                spawnIndex++;
            }
            else
            {
                // The rest of the player are not present during the tiebreaker
                player.Teleport(new Vector3(0f, -10f, 0f));
                _gameUI.HidePlayerShoppingList(player.PlayerAsset);
            }
        }

        // Show tieabreaker graphic
        _gameUI.ShowTiebreakerSticker(tiedPlayers);
        yield return new WaitForSeconds(2.5f);
        _gameUI.HideTiebreakerSticker();

        // Memory gamemode warning
        if (_gameSettings.matchGamemode == EGamemode.Memory)
        {
            _gameUI.memoryModeSticker.SetActive(true);
            yield return new WaitForSeconds(2f);
            _gameUI.memoryModeSticker.SetActive(false);
            _gameUI.HideAllPlayersShoppingLists();
        }

        // Allow tied players to move again
        foreach (var player in _playerControllers)
        {
            if (tiedPlayers.Contains(player.PlayerAsset))
                player.EnableMovement();
        }

        // Allow NPCs to move
        _npcManager.ResumeNPCs();

        // Open all counters
        foreach (var counter in DeliveryArea.allAreas)
        {
            counter.IsOpen = true;
        }

        // Show "Go" sticker
        _gameUI.ShowGoSticker();
        yield return new WaitForSeconds(0.5f);
        _gameUI.HideGoSticker();
    }

    private IEnumerator NextMatchRoutine()
    {
        // Get theme from game settings
        var supermarketTheme = _gameSettings.supermarketTheme;

        // Random theme and supermarket
        if (supermarketTheme == null)
            supermarketTheme = SupermarketTheme.GetAllThemes().GetRandomElement();

        // Fade to black
        yield return _loadingScreen.FadeIn();

        if (_gameSettings.isTournament)
        {
            // Advance tournament match index
            _gameSettings.tournamentMatchIndex++;

            var sceneName = _gameSettings.tournamentSceneNames[_gameSettings.tournamentMatchIndex];
            Debug.Log($"Starting to load game scene: {sceneName}");
            var sceneLoad = SceneManager.LoadSceneAsync(sceneName);
        }
        else
        {
            // Start random layout on the selected theme, dont repeat the current one
            var supermarket = supermarketTheme.Supermarkets.Where(x => x.sceneName != SceneManager.GetActiveScene().name).GetRandomElement();
            Debug.Log($"Starting to load game scene: {supermarket.sceneName}");
            var sceneLoad = SceneManager.LoadSceneAsync(supermarket.sceneName);
        }
    }

    private IEnumerator ExitMatchRoutine()
    {
        yield return _loadingScreen.FadeIn();

        SceneManager.LoadScene("MainMenu");
    }
}
