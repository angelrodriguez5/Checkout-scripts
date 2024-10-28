using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tutorial
{
    [System.Serializable]
    public struct PromptData
    {
        public string controlScheme;
        public Sprite interactDefault;
        public Sprite interactPressed;
        public Sprite dashDefault;
        public Sprite dashPressed;
    }

    public class TutorialPlayerZone : MonoBehaviour
    {
        public static List<TutorialPlayerZone> allZones = new();

        [SerializeField] Transform _playerSpawn;
        [SerializeField] List<ItemAsset> _availableItems;
        [SerializeField] ItemAsset _dealItem;
        [SerializeField] Shelf _shelf;
        [SerializeField] TutorialDeliveryArea _counter;
        [SerializeField] TutorialDealCounter _dealCounter;
        [SerializeField] TutorialShowcaseSticker _counterShowcase;
        [SerializeField] TutorialShowcaseSticker _dealStandShowcase;

        [SerializeField] List<TutorialPromptSprite> _promptsObjs;
        [SerializeField] PromptData[] _promptData;

        [Header("Tutorial stages gameobjects")]
        public GameObject stage0_substage1;
        public GameObject stage1;
        public GameObject stage2;
        public GameObject[] dividerWalls;
        public GameObject exitTutorialTrigger;

        private GameObject _currentItem;
        private PlayerController _player;
        public PlayerController Player
        {
            get => _player;
            private set
            {
                _player = value;
                UpdatePrompts();
            }
        }
        public bool IsCurrentStageCompleted { get; private set; }
        public int CurrentStage { get; set; }
        public int CurrentSubstage { get; set; }

        public event Action<int, int> onTutorialAdvanced;
        public event Action onTutorialStageCompleted;

        private void Awake()
        {
            stage0_substage1.SetActive(false);
            stage1.SetActive(false);
            stage2.SetActive(false);

            if (exitTutorialTrigger)
                exitTutorialTrigger.SetActive(false);

            _dealCounter.gameObject.SetActive(false);
            _availableItems = _availableItems.Shuffle().ToList();
        }

        private void OnEnable()
        {
            TutorialGameManager.onTutorialStageChanged += InitializeNewTutorialStage;
            allZones.Add(this);
        }

        private void OnDisable()
        {
            TutorialGameManager.onTutorialStageChanged -= InitializeNewTutorialStage;
            allZones.Remove(this);
        }

        public void AssignPlayer(PlayerController player)
        {
            Player = player;
            TutorialUI.Instance.AddPlayerShoppingList(Player.PlayerAsset, new());
        }

        public void AdvanceSubstage() => StartCoroutine(_AdvanceSubstage());
        public IEnumerator _AdvanceSubstage()
        {
            CurrentSubstage++;
            onTutorialAdvanced?.Invoke(CurrentStage, CurrentSubstage);
            Debug.Log($"Tutorial advanced to stage {CurrentStage} substage {CurrentSubstage}");

            // Stage 0: Pick up and throw items
            if (CurrentStage == 0)
            {
                // Spawn second item
                if (CurrentSubstage == 1)
                {
                    SpawnItem();
                    yield return null;
                    _shelf.LoadItems(new List<GameObject> { _currentItem });
                    _currentItem.GetComponent<TutorialItemBehaviour>().onItemGrabbed += ActivateS0sS1;
                }
            }

            // Stage 1: deals and closed counters
            else if (CurrentStage == 1) 
            {
                if (CurrentSubstage == 2)
                {
                    // Close counter briefly
                    _counter.IsOpen = false;
                    yield return new WaitForSeconds(4f);
                    _counter.IsOpen = true;
                }
            }

            // Stage 2: traps and hazards
            else if (CurrentStage == 2)
            {
                // Spawn item after the trap is activated
                if (CurrentSubstage == 1)
                { 
                    SpawnItem();
                    yield return null;
                    _shelf.LoadItems(new List<GameObject> { _currentItem });
                }
            }
        }

        public void CompleteTutorialStage()
        {
            IsCurrentStageCompleted = true;
            onTutorialStageCompleted?.Invoke();
        }

        public bool DeliverItem(ItemAsset item)
        {
            if (item != _dealItem)
                TutorialUI.Instance.RemoveItemFromShoppingList(Player.PlayerAsset, item);
            else
            {
                TutorialUI.Instance.RemoveItemFromShoppingList(Player.PlayerAsset, _availableItems[0]);
                _availableItems.RemoveAt(0);
            }

            // Pick up and throw items
            if (CurrentStage == 0)
            {
                // Deliver first item
                if (CurrentSubstage == 0)
                {
                    AdvanceSubstage();
                }
                // Deliver second item
                else if (CurrentSubstage == 1)
                {
                    stage0_substage1.SetActive(false);
                    CompleteTutorialStage();
                }
            }

            // Stage 1: deals and closed counters
            else if (CurrentStage == 1)
            {
                _dealCounter.gameObject.SetActive(false);
                stage1.SetActive(false);
                CompleteTutorialStage();
            }

            // Stage 2: traps and hazards
            else if (CurrentStage == 2)
            {
                stage2.SetActive(false);
                CompleteTutorialStage();
            }

                return true;
        }

        public void ReturnItemToShelf()
        {
            if (_currentItem.GetComponent<TutorialItemBehaviour>().ItemAsset.ItemCategory == EItemCategory.DealItems)
                _dealCounter.ReturnDeal();
            else
                _shelf.LoadItems(new List<GameObject> { _currentItem });
        }

        public void InteractedWithTrap()
        {
            if (CurrentStage == 2 && CurrentSubstage == 0)
            {
                AdvanceSubstage();
            }
        }

        private void InitializeNewTutorialStage(int stage) => StartCoroutine(_InitializeNewTutorialStage(stage));
        private IEnumerator _InitializeNewTutorialStage(int stage)
        {
            CurrentStage = stage;
            CurrentSubstage = 0;
            IsCurrentStageCompleted = false;
            Player.EnableMovement();
            onTutorialAdvanced?.Invoke(CurrentStage, CurrentSubstage);
            Debug.Log($"Tutorial began stage {CurrentStage}");

            // Stage 0: Pick up and throw items
            if (CurrentStage == 0)
            {
                // Move player to spawn
                Player.Teleport(_playerSpawn);

                // Add 2 items to shopping list
                TutorialUI.Instance.AddItemToShoppingList(Player.PlayerAsset, new List<ItemAsset> { _availableItems[0], _availableItems[1] });

                // Initialize substage 0
                SpawnItem();
                yield return null;
                _shelf.LoadItems(new List<GameObject> { _currentItem });

                // Wait for blur to disappear
                yield return new WaitForSeconds(4f);
                _counterShowcase.ShowShowcase();
                yield return new WaitForSeconds(2.7f);
                _counterShowcase.HideShowcase();
            }

            // Stage 1: deals and closed counters
            else if (CurrentStage == 1)
            {
                _dealCounter.gameObject.SetActive(true);
                TutorialUI.Instance.AddItemToShoppingList(Player.PlayerAsset, _availableItems[0]);

                _dealCounter.ScheduleDealSpawn();

                yield return new WaitForEndOfFrame();
                _dealStandShowcase.ShowShowcase();
                yield return new WaitForSeconds(2f);
                _dealStandShowcase.HideShowcase();
            }

            // Stage 2: traps and hazards
            else if (CurrentStage == 2)
            {
                stage2.SetActive(true);
                TutorialUI.Instance.AddItemToShoppingList(Player.PlayerAsset, _availableItems[0]);
            }

            // Stage 3: dash and parry
            else if (CurrentStage == 3)
            {
                // Disable every object
                _counter.gameObject.SetActive(false);
                _shelf.gameObject.SetActive(false);
                _dealCounter.gameObject.SetActive(false);
                stage0_substage1.SetActive(false);
                stage1.SetActive(false);
                stage2.SetActive(false);
                foreach (var wall in dividerWalls)
                {
                    if (wall)
                        wall.SetActive(false);
                }

                // Activate player prompt
                GameObject prompt = null;
                foreach (Transform child in Player.transform)
                {
                    if (child.name == "TutorialDashPrompt")
                    {
                        prompt = child.gameObject;
                        prompt.SetActive(true);
                        break;
                    }
                }

                // Wait for some time before allowing them to exit the tutorial
                yield return new WaitForSeconds(5f);
                prompt.SetActive(false);
                if (exitTutorialTrigger)
                    exitTutorialTrigger.SetActive(true);
            }
        }

        private void ActivateS0sS1()
        {
            stage0_substage1.SetActive(true);
            // Once activated unsubscribe
            _currentItem.GetComponent<TutorialItemBehaviour>().onItemGrabbed -= ActivateS0sS1;
        }

        private void SpawnItem()
        {
            _currentItem = _availableItems[0].SpawnNewGameObject();
            _availableItems.RemoveAt(0);
            // Destroy normal ItemBehaviour
            var itemAsset = _currentItem.GetComponent<ItemBehaviour>().ItemAsset;
            Destroy(_currentItem.GetComponent<ItemBehaviour>());

            // Add tutorial version
            var tutorialItem = _currentItem.AddComponent<TutorialItemBehaviour>();
            tutorialItem.ItemAsset = itemAsset;
            tutorialItem.tutorial = this;
        }

        public GameObject SpawnDeal()
        {
            _currentItem = _dealItem.SpawnNewGameObject();
            //_availableItems.RemoveAt(0);
            // Destroy normal ItemBehaviour
            var itemAsset = _currentItem.GetComponent<ItemBehaviour>().ItemAsset;
            Destroy(_currentItem.GetComponent<ItemBehaviour>());

            // Add tutorial version
            var tutorialItem = _currentItem.AddComponent<TutorialItemBehaviour>();
            tutorialItem.ItemAsset = itemAsset;
            tutorialItem.tutorial = this;

            return _currentItem;
        }

        public void UpdatePrompts()
        {
            var promptData = _promptData.First(x => x.controlScheme == _player.PlayerInput.currentControlScheme);

            // Find prompts inside the player object
            _promptsObjs.AddRange(Player.GetComponentsInChildren<TutorialPromptSprite>(true));

            // Check for default struct (control scheme not found)
            if (promptData.interactDefault == null)
                Debug.LogError($"Update prompts: control scheme \"{_player.PlayerInput.currentControlScheme}\" not found");

            foreach (var prompt in _promptsObjs)
            {
                prompt.ConfigurePrompt(promptData);
            }
        }
    }
}