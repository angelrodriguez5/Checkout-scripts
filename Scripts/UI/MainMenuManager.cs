using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Cinemachine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.Users;
using UnityEngine.UI;
using System.Collections;
using System;
using UnityEngine.Localization.Settings;

public class MainMenuManager : MonoBehaviour
{
    [Header("First Selected Objects")]
    [SerializeField] private GameObject _mainMenuFirstSelected;
    [SerializeField] private GameObject _optionsFirstSelected;
    [SerializeField] private GameObject _matchSettingsTournamentFirstSelected;
    [SerializeField] private GameObject _matchSettingsQuickplayFirstSelected;
    [SerializeField] private GameObject _matchModifiersFirstSelected;

    [Header("Menu Panels")]
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _optionsMenuPanel;
    [SerializeField] private GameObject _howToPlayPanel1;
    [SerializeField] private GameObject _howToPlayPanel2;
    [SerializeField] private GameObject _howToPlayPanel3;
    [SerializeField] private GameObject _matchConfigContainer;
    [SerializeField] private GameObject _matchConfigQuickplaySettingsContent;
    [SerializeField] private GameObject _matchConfigTournamentContent;
    [SerializeField] private GameObject _matchConfigModifiersContent;
    [SerializeField] private GameObject _playerSelectionPanel;
    [SerializeField] private GameObject _creditsPanel;
    [SerializeField] private GameObject _languageSelectionPanel;

    [Header("Cameras")]
    [SerializeField] private CinemachineVirtualCamera _mainMenuVCam;
    [SerializeField] private CinemachineVirtualCamera _playerSelectVCam;

    [Header("Audio")]
    [SerializeField] private AudioClip _submitAudioClip;
    [SerializeField] private AudioClip _cancelAudioClip;
    [SerializeField] private AudioClip _playButtonAudioClip;

    [Header("Other")]
    [SerializeField] private List<PlayerSelectionController> _playerSpawnPoints;
    [SerializeField] private Image _playButtonImageMask;
    [SerializeField] private Image _playButtonPromptImage;
    [SerializeField] private Sprite _playButtonKeyboard;
    [SerializeField] private Sprite _playButtonController;
    [SerializeField] private Image  _inviteFriendsPromptImage;
    [SerializeField] private Sprite _inviteFriendsKeyboard;
    [SerializeField] private Sprite _inviteFriendsController;
    [SerializeField] private LoadingScreen _loadingScreen;
    [SerializeField] private Image _logoScreenBackground;
    [SerializeField] private string[] _tutorialSceneNamesByNumPlayers;
    [SerializeField] private LanguageButton[] _languageButtons;
    [SerializeField] private Button[] _mainMenuDisableBeforeTutorial;
    [SerializeField] private GameObject _tutorialButton;
    [SerializeField] private GameObject _mainMenuTutorialButtonHighlight;
    [SerializeField] private GameObject _playerSelectionTutorialReminder;

    [HideInInspector] public PlayerInput mainMenuPlayerInput;
    [HideInInspector] public bool isPlayButtonPressed;

    private GameSettings _gameSettings;
    private List<PlayerAsset> _playerAssets;
    private PlayerInputManager _inputManager;
    private EventSystem _eventSystem;

    private bool _wasdJoined = false;
    private bool _arrowsJoined = false;
    private static bool _isFirstMenu = true;

    float _playMaskFillTime = .7f;
    float _playMaskEmptyTime = 0.25f;
    bool _isGameLoading;
    bool _signalStartGame;
    bool _isTutorial;
    bool _isTutorialCompleted;

    public static MainMenuManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else throw new System.Exception($"Several {nameof(MainMenuManager)} in scene");

        _inputManager = GetComponent<PlayerInputManager>();
        mainMenuPlayerInput = GetComponent<PlayerInput>();

        _gameSettings = GameSettings.Current;
        _playerAssets = Resources.LoadAll<PlayerAsset>(@"Players").OrderBy(x => x.Id).ToList();

        // Set game state
        GameState.SetState(EGameState.Menu);
        int tutorialPlayerPref = PlayerPrefs.GetInt("isTutorialCompleted", 0);
        _isTutorialCompleted = tutorialPlayerPref != 0;

        // Deactivate all players, only activate them when they join
        _playerAssets.Map(x => x.Active = false);

        _mainMenuPanel.SetActive(false);

        Cursor.visible = false;
    }

    private void Start()
    {
        _inputManager.enabled = false;

        //Loading screen
        _eventSystem = EventSystem.current;
        _eventSystem.enabled = false;
        if (_isFirstMenu)
        {
            _loadingScreen.gameObject.SetActive(false);
            _logoScreenBackground.gameObject.SetActive(true);
        }
        else
        {
            _loadingScreen.gameObject.SetActive(true);
            _logoScreenBackground.gameObject.SetActive(false);
            _loadingScreen.InstantIn();
        }

        StartCoroutine(OpenMainMenu());
    }

    private void OnEnable()
    {
        _inputManager.joinAction.action.performed += CheckPlayersOnKeyboard;
    }

    private void OnDisable()
    {
        _inputManager.joinAction.action.performed -= CheckPlayersOnKeyboard;
    }

    private void Update()
    {
        // Only start the game if there are enough players and all are ready
        if (   isPlayButtonPressed
            && _playerSpawnPoints.Where(x => x.Player != null).All(x => x.IsReady)  // All players are ready
            && 
               (
                    _playerSpawnPoints.Where(x => x.Player != null).Count() >= 2  // Dont start match with less than 2 players
                 ||  (_isTutorial && _playerSpawnPoints.Where(x => x.Player != null).Count() >= 1)  // Allow to start tutorial with 1 player
               )
            )
        {
            _playButtonImageMask.fillAmount += (1 / _playMaskFillTime) * Time.deltaTime;

            if (_playButtonImageMask.fillAmount >= 1 && !_isGameLoading)
            {
                _isGameLoading = true;
                StartCoroutine(LoadGameSceneRoutine());
            }
        }

        // Also animate the fill back down to 0
        else if (_playButtonImageMask.fillAmount > 0 && !_isGameLoading)
        {
            _playButtonImageMask.fillAmount -= (1 / _playMaskEmptyTime) * Time.deltaTime;
        }
    }

    #region Buttons
    public void QuickplayButtonPressed()
    {
        // Config game settings
        _gameSettings.isTournament = false;

        // Hide menu, open game settings
        ShowMatchSettings();
    }

    public void TournamentButtonPressed()
    {
        // Config game settings
        _gameSettings.isTournament = true;

        // Hide menu, open game settings
        ShowMatchSettings();
    }

    public void HowToPlayButtonPressed()
    {
        _mainMenuPanel.SetActive(false);
        _howToPlayPanel1.SetActive(true);
    }

    public void TutorialButtonPressed()
    {
        _mainMenuPanel.SetActive(false);
        _isTutorial = true;
        SetPlayerSelectScreenActive(true);
    }

    public void OptionsButtonPressed()
    {
        _mainMenuPanel.SetActive(false);
        _optionsMenuPanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(_optionsFirstSelected);
    }

    public void ExitButtonPressed()
    {
        Application.Quit();
    }

    public void CreditsButtonPresset ()
    {
        _optionsMenuPanel.SetActive(false);
        _creditsPanel.SetActive(true);
    }
    #endregion

    public void Back()
    {
        bool playSound = false;

        // Check what we should do depending on the active panel and game state
        
        // Player selection with no players ready: return to match settings
        if (GameState.CurrentState == EGameState.PlayerSelection && _playerSpawnPoints.All(x => x.Player == null))
        {
            SetPlayerSelectScreenActive(false);
            if (_isTutorial)
            {
                _mainMenuPanel.SetActive(true);
                EventSystem.current.SetSelectedGameObject(_isTutorialCompleted ? _mainMenuFirstSelected : _tutorialButton);
                _isTutorial = false;
            }
            else
            {
                ShowMatchSettings();
            }
            playSound = true;
        }

        // Match modifiers: return to match settings
        else if (GameState.CurrentState == EGameState.MatchSettings && _matchConfigModifiersContent.activeInHierarchy)
        {
            ShowMatchSettings();
            playSound = true;
        }

        // How to play panel 1: return to menu
        else if (_howToPlayPanel1.activeInHierarchy)
        {
            _howToPlayPanel1.SetActive(false);
            _mainMenuPanel.SetActive(true);
            EventSystem.current.SetSelectedGameObject(_isTutorialCompleted ? _mainMenuFirstSelected : _tutorialButton);
            playSound = true;
        }

        // How to play panel 2: return to panel 1
        else if (_howToPlayPanel2.activeInHierarchy)
        {
            _howToPlayPanel1.SetActive(true);
            _howToPlayPanel2.SetActive(false);
            playSound = true;
        }

        // How to play panel 3: return to panel 2
        else if (_howToPlayPanel3.activeInHierarchy)
        {
            _howToPlayPanel2.SetActive(true);
            _howToPlayPanel3.SetActive(false);
            playSound = true;
        }

        // Match settings, options or credits: return to main menu
        else if (
               _optionsMenuPanel.activeInHierarchy 
            || _creditsPanel.activeInHierarchy
            || _matchConfigTournamentContent.activeInHierarchy 
            || _matchConfigQuickplaySettingsContent.activeInHierarchy)
        {
            GameState.SetState(EGameState.Menu);
            _optionsMenuPanel.SetActive(false);
            _matchConfigContainer.SetActive(false);
            _creditsPanel.SetActive(false);
            _mainMenuPanel.SetActive(true);
            EventSystem.current.SetSelectedGameObject(_isTutorialCompleted ? _mainMenuFirstSelected : _tutorialButton);
            playSound = true;
        }

        if (playSound)
            AudioManager.Instance.UiSource.PlayOneShot(_cancelAudioClip);
    }

    public void Submit()
    {
        bool playSound = false;

        // Match settings (when not in modifier screen): go to player selection
        if (GameState.CurrentState == EGameState.MatchSettings && !_matchConfigModifiersContent.activeInHierarchy)
        {
            SetPlayerSelectScreenActive(true);
            playSound = true;
        }

        // Player selection (loading screen): signal transition scenes
        else if (GameState.CurrentState == EGameState.PlayerSelection)
        {
            _signalStartGame = true;
        }

        // How to play panel 1: advance to panel 2
        else if (_howToPlayPanel1.activeInHierarchy)
        {
            _howToPlayPanel1.SetActive(false);
            _howToPlayPanel2.SetActive(true);
            playSound = true;
        }

        // How to play panel 2: advance to panel 3
        else if (_howToPlayPanel2.activeInHierarchy)
        {
            _howToPlayPanel2.SetActive(false);
            _howToPlayPanel3.SetActive(true);
            playSound = true;
        }

        // How to play panel 3: return to main menu
        else if (_howToPlayPanel3.activeInHierarchy)
        {
            _howToPlayPanel3.SetActive(false);
            _mainMenuPanel.SetActive(true);

            // Deselect current gameobject to avoid calling submit on it, then select main menu object on the next frame
            EventSystem.current.SetSelectedGameObject(null);
            StartCoroutine(DelayCallEoF(() => EventSystem.current.SetSelectedGameObject(_isTutorialCompleted ? _mainMenuFirstSelected : _tutorialButton)));
            playSound = true;
        }

        // Options menu: return to main menu
        //else if (_optionsMenuPanel.activeInHierarchy)
        //{
        //    _optionsMenuPanel.SetActive(false);
        //    _mainMenuPanel.SetActive(true);

        //    // Deselect current gameobject to avoid calling submit on it, then select main menu object on the next frame
        //    EventSystem.current.SetSelectedGameObject(null);
        //    StartCoroutine(DelayCallEoF(() => EventSystem.current.SetSelectedGameObject(_mainMenuFirstSelected)));
        //    playSound = true;
        //}

        if (playSound)
            AudioManager.Instance.UiSource.PlayOneShot(_submitAudioClip);
    }

    public void AuxButton()
    {
        // Match settings: display modifiers if not already active
        if (GameState.CurrentState == EGameState.MatchSettings && !_matchConfigModifiersContent.activeInHierarchy)
        {
            _matchConfigQuickplaySettingsContent.SetActive(false);
            _matchConfigTournamentContent.SetActive(false);
            _matchConfigModifiersContent.SetActive(true);
            EventSystem.current.SetSelectedGameObject(_matchModifiersFirstSelected);
            AudioManager.Instance.UiSource.PlayOneShot(_submitAudioClip);
        }
    }

    public void ShowMatchSettings()
    {
        GameState.SetState(EGameState.MatchSettings);

        _mainMenuPanel.SetActive(false);

        _matchConfigQuickplaySettingsContent.SetActive(!_gameSettings.isTournament);
        _matchConfigTournamentContent.SetActive(_gameSettings.isTournament);

        var firstSelected = _gameSettings.isTournament ?
            _matchSettingsTournamentFirstSelected :
            _matchSettingsQuickplayFirstSelected;

        _matchConfigContainer.SetActive(true);
        _matchConfigModifiersContent.SetActive(false);
        EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    public void SetPlayerSelectScreenActive(bool active)
    {
        if (active)
        {
            _playerSelectionTutorialReminder.SetActive(_isTutorial);

            // Setup the main menu player input for joining keyboard players
            mainMenuPlayerInput.SwitchCurrentControlScheme("KeyboardPlayerSelection", Keyboard.current);
            mainMenuPlayerInput.neverAutoSwitchControlSchemes = true;

            // Allow player joining and swap cameras
            GameState.SetState(EGameState.PlayerSelection);
            _matchConfigContainer.SetActive(false);
            _playerSelectionPanel.SetActive(true);
            _inputManager.enabled = true;
            _inputManager.EnableJoining();

            _mainMenuVCam.Priority = 0;
            _playerSelectVCam.Priority = 1;
        }
        else
        {
            GameState.SetState(EGameState.Menu);
            _playerSelectionPanel.SetActive(false);
            _inputManager.enabled = false;
            _inputManager.DisableJoining();

            _mainMenuVCam.Priority = 1;
            _playerSelectVCam.Priority = 0;

            // Remove all players
            foreach (var spawn in _playerSpawnPoints)
            {
                if (spawn.Player != null)
                {
                    spawn.SetPlayer(null);
                }
            }

            // Restore player tracking variables
            _wasdJoined = false;
            _arrowsJoined = false;

            // Enable auto switch for main menu playerInput so all players can control the menu
            mainMenuPlayerInput.neverAutoSwitchControlSchemes = false;
        }
    }

    public void PlayerDeviceLost(PlayerInput player)
    {
        // Device lost while in menu, do nothing
        if (player == mainMenuPlayerInput) return;

        if (GameState.CurrentState == EGameState.PlayerSelection)
        {
            // delete player and free its spawn position, when the device is regained he join again
            var playerController = player.GetComponent<PlayerController>();
            var playerSpawn = _playerSpawnPoints.First(x => x.Player == playerController);
            if (playerSpawn)
                playerSpawn.SetPlayer(null);
        }
    }

    public void HideLanguageSelectionPanel()
    {
        _languageSelectionPanel.SetActive(false);
        _mainMenuPanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(_isTutorialCompleted ? _mainMenuFirstSelected : _tutorialButton);
    }

    private void CheckPlayersOnKeyboard(InputAction.CallbackContext ctx)
    {
        // Check if WASD or the arrows have been pressed and join a player if it has not been joined already
        if (ctx.performed && _inputManager && _inputManager.enabled && _inputManager.joiningEnabled)
        {
            var controlSchemes = ctx.action.GetBindingForControl(ctx.control).Value.groups.Split(";");
            if (!_wasdJoined && controlSchemes.Contains("KeyboardWASD"))
            {
                _inputManager.JoinPlayer(controlScheme: "KeyboardWASD", pairWithDevice: Keyboard.current);
                _wasdJoined = true;
            }
            else if (!_arrowsJoined && controlSchemes.Contains("KeyboardArrows"))
            {
                _inputManager.JoinPlayer(controlScheme: "KeyboardArrows", pairWithDevice: Keyboard.current);
                _arrowsJoined = true;
            }
        }
    }

    private IEnumerator OpenMainMenu()
    {
        // Check for forced tutorial
        if (!_isTutorialCompleted)
        {
            // Disable play buttons
            foreach (var button in _mainMenuDisableBeforeTutorial)
            {
                button.interactable = false;
            }

            _mainMenuTutorialButtonHighlight.SetActive(true);
        }
        else
        {
            _mainMenuTutorialButtonHighlight.SetActive(false);
        }

        // Default menu: main menu
        yield return null;
        _mainMenuPanel.SetActive(true);
        _optionsMenuPanel.SetActive(false);
        _matchConfigContainer.SetActive(false);
        _matchConfigQuickplaySettingsContent.SetActive(false);
        _matchConfigTournamentContent.SetActive(false);
        _matchConfigModifiersContent.SetActive(false);
        _howToPlayPanel1.SetActive(false);
        _howToPlayPanel2.SetActive(false);
        _howToPlayPanel3.SetActive(false);
        _playerSelectionPanel.SetActive(false);
        _languageSelectionPanel.SetActive(false);
        _eventSystem.SetSelectedGameObject(_isTutorialCompleted? _mainMenuFirstSelected : _tutorialButton);

        // The first time the game opens display a language selection panel
        string localeStr = PlayerPrefs.GetString("selected-locale", "");
        if (localeStr == "")
        {
            // Try to pre-select the button of the correct locale, else default to english
            var locale = LocalizationSettings.SelectedLocale;
            GameObject defaultLanguageButton = _languageButtons[0].gameObject;
            foreach (var button in _languageButtons)
            {
                if (button.locale == locale)
                {
                    defaultLanguageButton = button.gameObject;
                    break;
                }
            }

            _languageSelectionPanel.SetActive(true);
            _mainMenuPanel.SetActive(false);
            _eventSystem.SetSelectedGameObject(defaultLanguageButton);
        }

        // Lift loading screen
        yield return new WaitForSeconds(0.2f);
        if (_isFirstMenu)
        {
            _isFirstMenu = false;
            _logoScreenBackground.CrossFadeAlpha(0, 0.5f, true);
        }
        else
        {
            // Return with a loading screen
            yield return _loadingScreen.FadeOut();
        }

        // Music
        yield return null;
        _eventSystem.enabled = true;
        StartCoroutine(AudioManager.Instance.MusicSource.FadeIn(1f));
    }

    private IEnumerator LoadGameSceneRoutine()
    {

        // Get theme from game settings
        var supermarketTheme = _gameSettings.supermarketTheme;

        // Random theme and supermarket
        if (supermarketTheme == null)
            supermarketTheme = SupermarketTheme.GetAllThemes().GetRandomElement();

        // If its a tournament initialize tournament variables
        if (_gameSettings.isTournament)
        {
            var numPlayers = _playerAssets.Where(x => x.Active).Count();
            var tournamentMaxMatches = (numPlayers * (_gameSettings.tournamentWinsTarget - 1)) + 1;

            // Player tournament wins
            _gameSettings.tournamentPlayerWins.Clear();
            foreach (var player in _playerAssets.Where(x => x.Active))
            {
                _gameSettings.tournamentPlayerWins.Add(player, 0);
            }

            // Tournament gamemodes
            if (_gameSettings.gamemode == EGamemode.Random)
            {
                // RANDOM GAMEMODE
                var allGamemodes = Enum.GetValues(typeof(EGamemode)).Cast<EGamemode>().ToList();
                allGamemodes.Remove(EGamemode.Random);

                // Ensure that all gamemodes will appear before any mode is repeated
                var repetitions = Math.Ceiling(tournamentMaxMatches / (float)allGamemodes.Count);
                var gamemodeList = new List<EGamemode>();
                for (int i = 0; i < repetitions; i++)
                {
                    gamemodeList.AddRange(allGamemodes.Shuffle());
                }

                _gameSettings.tournamentGamemodes = gamemodeList.Take(tournamentMaxMatches).ToList();
            }
            else
            {
                // SPECIFIC GAMEMODE
                _gameSettings.tournamentGamemodes = Enumerable.Repeat(_gameSettings.gamemode, tournamentMaxMatches).ToList();
            }

            // Tournament scenes
            if(_gameSettings.supermarketTheme == null)
            {
                // RANDOM THEME
                // Ensure aprox equal appearance of all themes and layouts
                var allThemes = SupermarketTheme.GetAllThemes();
                var sceneNames = new List<string>();
                Dictionary<SupermarketTheme, List<int>> themesAvailableLayoutIndexes = new();

                // Init dictionary with list of random supermarket indexes within its theme
                foreach (var theme in allThemes)
                {
                    themesAvailableLayoutIndexes.Add(theme, Enumerable.Range(0, theme.Supermarkets.Length).Shuffle().ToList());
                }

                Queue<SupermarketTheme> themeQueue= new();
                while (sceneNames.Count() < tournamentMaxMatches)
                {
                    // Show all themes before repeating
                    if (themeQueue.Count() == 0)
                    {
                        foreach (var t in allThemes.Shuffle())
                        {
                            themeQueue.Enqueue(t);
                        }
                    }

                    var theme = themeQueue.Dequeue();
                    // Reset theme available layouts if we run out
                    if (themesAvailableLayoutIndexes[theme].Count() == 0)
                        themesAvailableLayoutIndexes[theme] = Enumerable.Range(0, theme.Supermarkets.Length).Shuffle().ToList();

                    // Take a random layout from that theme
                    var index = themesAvailableLayoutIndexes[theme][0];
                    themesAvailableLayoutIndexes[theme].RemoveAt(0);

                    sceneNames.Add(theme.Supermarkets[index].sceneName);
                }

                _gameSettings.tournamentSceneNames = sceneNames.Take(tournamentMaxMatches).ToList();
            }
            else
            {
                // SPECIPIC THEME
                // Ensure that all layouts will be played at least once before any are repeated
                var repetitions = Math.Ceiling(tournamentMaxMatches / (float)_gameSettings.supermarketTheme.Supermarkets.Length);
                var allThemeScenes = _gameSettings.supermarketTheme.Supermarkets.Select(x => x.sceneName);
                var sceneNames = new List<string>();
                for (int i = 0; i < repetitions; i++)
                {
                    sceneNames.AddRange(allThemeScenes.Shuffle());
                }

                _gameSettings.tournamentSceneNames = sceneNames.Take(tournamentMaxMatches).ToList();
            }

            _gameSettings.tournamentMatchIndex = 0;
        }

        // Disable isTest when entering a match from the main menu in unity editor
        _gameSettings.isTest = false;

        // Play sound
        AudioManager.Instance.UiSource.PlayOneShot(_playButtonAudioClip);
        StartCoroutine(AudioManager.Instance.MusicSource.FadeOut(0.4f));

        // Fade to black
        yield return _loadingScreen.FadeIn();

        if (_isTutorial)
        {
            int numPlayers = _playerAssets.Where(x => x.Active).Count();
            SceneManager.LoadScene(_tutorialSceneNamesByNumPlayers[numPlayers - 1]);
        }
        else if (_gameSettings.isTournament)
        {
            // Start tournament
            var sceneName = _gameSettings.tournamentSceneNames[_gameSettings.tournamentMatchIndex];
            Debug.Log($"Starting to load game scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            // Start random layout on the selected theme
            var supermarket = supermarketTheme.Supermarkets.GetRandomElement();
            Debug.Log($"Starting to load game scene: {supermarket.sceneName}");
            SceneManager.LoadScene(supermarket.sceneName);
        }
    }

    private IEnumerator DelayCallEoF(System.Action action)
    {
        yield return new WaitForEndOfFrame();
        action();
    }

    #region PLAYER INPUT MANAGER MESSAGES
    private void OnPlayerJoined(PlayerInput player)
    {
        player.SwitchCurrentActionMap("UI");

        // If the player that joins does not have a PlayerController means that its the playerInput that controls the menu,
        // Assign it the keyboardplayerSelection control scheme so keyboardplayers can join
        if (player.GetComponent<PlayerController>() == null)
        {
            Debug.Log($"UI controller player joined, switching control scheme and device");

            player.SwitchCurrentControlScheme("KeyboardPlayerSelection", Keyboard.current);
            player.neverAutoSwitchControlSchemes = true;
            // Skip the rest of the configuration
            return;
        }

        Debug.Log($"Player {player.playerIndex} joined: {player.devices[0]} | {player.currentControlScheme}");

        // Register players device to the menuPlayerInput so all devices can move the shared UI (main menu, match settings...)
        InputUser.PerformPairingWithDevice(player.devices[0], mainMenuPlayerInput.user);

        // Load player asset into player prefab, and save player input configuration in player asset to load it later on the game scene
        var playerController = player.GetComponent<PlayerController>();
        var playerAsset = _playerAssets.First(x => x.Active == false);
        playerController.LoadPlayerAsset(playerAsset);
        playerAsset.Active = true;
        playerAsset.Device = player.devices[0]; // We asume only one device per player
        playerAsset.ControlScheme = player.currentControlScheme;

        // Player spawn point manages player selection UI, we just need to set the player that each spawn controls
        // Set new player to the first empty spawn
        var playerSpawn = _playerSpawnPoints.First(x => x.Player == null);
        playerSpawn.SetPlayer(player);

        // Set play button prompt to the control scheme of the first player to join
        if (player.playerIndex == 1)
        {
            if (player.currentControlScheme.Contains("Keyboard"))
            {
                _playButtonPromptImage.sprite = _playButtonKeyboard;
                //_inviteFriendsPromptImage.sprite = _inviteFriendsKeyboard;
            }
            else
            {
                _playButtonPromptImage.sprite = _playButtonController;
                //_inviteFriendsPromptImage.sprite = _inviteFriendsController;
            }
        }
    }

    private void OnPlayerLeft(PlayerInput player)
    {
        Debug.Log($"player {player.playerIndex} left");

        // If a KB player leaves mark its control scheme as available
        if (player.currentControlScheme == "KeyboardWASD")
            _wasdJoined = false;
        else if (player.currentControlScheme == "KeyboardArrows")
            _arrowsJoined = false;

        // If the player was not on keyboard unpair its device from menuPlayerInput so it can rejoin instead of consuming its input through the menuPlayerInput
        if (player.devices[0] != Keyboard.current)
            mainMenuPlayerInput.user.UnpairDevice(player.devices[0]);

        // If this was the only player and he left, return to main menu
        if (_playerSpawnPoints.All(x => x.Player == null)) Back();
    }
    #endregion

    #region PLAYER INPUT MESSAGES
    private void OnSubmit(InputValue value)
    {
        if (!value.isPressed) return;

        Submit();
    }

    private void OnCancel(InputValue value)
    {
        if (!value.isPressed) return;

        Back();
    }

    private void OnAuxButton(InputValue value)
    {
        if (value.isPressed) AuxButton();
    }

    private void OnControlsChanged()
    {
        if (!mainMenuPlayerInput) return;

        // Propagate event
        mainMenuPlayerInput.controlsChangedEvent.Invoke(mainMenuPlayerInput);
    }

    private void OnDebugForceTutorial(InputValue value)
    {
        #if INCLUDE_DEBUGS
            if (value.isPressed && _mainMenuPanel.activeInHierarchy)
            {
                // Disable play buttons
                foreach (var button in _mainMenuDisableBeforeTutorial)
                {
                    button.interactable = !button.interactable;
                }
                _eventSystem.SetSelectedGameObject(_tutorialButton);
            }
        #endif
    }

#endregion
}
