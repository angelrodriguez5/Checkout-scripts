using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tutorial
{
    public class TutorialGameManager : MonoBehaviour
    {
        [SerializeField] TutorialPlayerZone[] _tutorialZones;
        [SerializeField] GameObject _playerPrefab;
        [SerializeField] LoadingScreen _loadingScreen;
        [SerializeField] AudioSource _music;

        private PlayerInputManager _playerInputManager;

        public int TutorialStage { get; private set; } = 0;
        public List<PlayerAsset> Players { get; private set; }

        public static event Action<int> onTutorialStageChanged;

        private void Awake()
        {
            _playerInputManager = GetComponent<PlayerInputManager>();
            _playerInputManager.playerPrefab = _playerPrefab;

            _loadingScreen.InstantIn();
        }

        private IEnumerator Start()
        {
            SpawnPlayers();
            yield return new WaitForSeconds(0.1f);

            StartCoroutine(_music.FadeIn());

            // Start tutorial
            TutorialStage = 0;
            onTutorialStageChanged?.Invoke(TutorialStage);
            yield return new WaitForEndOfFrame();
            TutorialUI.Instance.HideAllPlayersShoppingLists();

            yield return _loadingScreen.FadeOut();

            yield return new WaitForSeconds(0.5f);
            TutorialUI.Instance.ShowAllPlayersShoppingLists();

            yield return new WaitForSeconds(3f);
            TutorialUI.Instance.RemoveBlur();
        }

        private void OnEnable()
        {
            foreach (var tutorialZone in _tutorialZones)
            {
                tutorialZone.onTutorialStageCompleted += CheckAdvanceTutorial;
            }
        }

        private void OnDisable()
        {
            foreach (var tutorialZone in _tutorialZones)
            {
                tutorialZone.onTutorialStageCompleted -= CheckAdvanceTutorial;
            }
        }

        private void CheckAdvanceTutorial()
        {
            if (_tutorialZones.All(zone => zone.IsCurrentStageCompleted))
            {
                // Advance tutorial
                TutorialStage++;
                onTutorialStageChanged?.Invoke(TutorialStage);
            }
        }

        protected void SpawnPlayers()
        {
            // Gather all active players
            List<PlayerAsset> playerAssets;
            if (GameSettings.Current.isTest)
            {
                // if is test spawn one player with keyboard no matter what
                playerAssets = Resources.LoadAll<PlayerAsset>("Players").OrderBy(x => x.Id).Take(1).ToList();
                playerAssets[0].ControlScheme = "KeyboardWASD";
                playerAssets[0].Device = Keyboard.current;
            }
            else
                playerAssets = Resources.LoadAll<PlayerAsset>("Players").Where(x => x.Active).OrderBy(x => x.Id).ToList();

            // Set number of players
            Players = playerAssets.ToList();

            // Spawn and configure each active player
            for (int i = 0; i < playerAssets.Count; i++)
            {
                var asset = playerAssets[i];

                // Spawn player
                var playerInput = _playerInputManager.JoinPlayer(playerIndex: asset.Id, controlScheme: asset.ControlScheme, pairWithDevice: asset.Device);

                // Load player asset which determines customization and input system configuration
                var playerController = playerInput.GetComponent<PlayerController>();
                playerController.LoadPlayerAsset(asset);

                // Move player to its tutorial zone
                _tutorialZones[i].AssignPlayer(playerController);
            }
        }
    }
}