using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Rendering.PostProcessing;

namespace Tutorial
{
    public class TutorialUI : MonoBehaviour
    {
        [SerializeField] GameObject[] _stageHeaderObjects;
        [SerializeField] TutorialListDisplay _playerItemListPrefab;
        [SerializeField] Transform _playerListLayout;
        [SerializeField] bool _stackTutorialStageStickers;
        [SerializeField] PostProcessVolume _blur;

        public LoadingScreen loadingScreen;

        private Dictionary<PlayerAsset, TutorialListDisplay> _playerListObjects = new Dictionary<PlayerAsset, TutorialListDisplay>();

        public static TutorialUI Instance { get; private set; }

        private void Awake()
        {
            Instance = this;

            foreach (var sticker in _stageHeaderObjects)
            {
                sticker.SetActive(false);
            }
        }

        private void Start()
        {
            GameManager.onGamePaused += HideUIOnPause;
            TutorialGameManager.onTutorialStageChanged += TutorialStageChanged;
        }

        private void OnDestroy()
        {
            GameManager.onGamePaused -= HideUIOnPause;
            TutorialGameManager.onTutorialStageChanged -= TutorialStageChanged;
        }

        private void HideUIOnPause(bool value)
        {
            gameObject.SetActive(!value);
        }

        public void AddPlayerShoppingList(PlayerAsset player, List<ItemAsset> items)
        {
            var instance = Instantiate(_playerItemListPrefab.gameObject, _playerListLayout);
            instance.GetComponent<IPlayerItemList>().Initialise(player, items);

            _playerListObjects.Add(player, instance.GetComponent<TutorialListDisplay>());
        }

        public void AddItemToShoppingList(PlayerAsset player, List<ItemAsset> items)
        {
            foreach (var item in items)
            {
                AddItemToShoppingList(player, item);
            }
        }
        public void AddItemToShoppingList(PlayerAsset player, ItemAsset item)
        {
            _playerListObjects[player].AddItem(item);
        }

        public void RemoveItemFromShoppingList(PlayerAsset player, ItemAsset item)
        {
            _playerListObjects[player].RemoveItem(item);
        }

        public void ShowAllPlayersShoppingLists()
        {
            _playerListObjects.Values.Map(x => x.gameObject.SetActive(true));

            var sequence = DOTween.Sequence();

            float seqPosition = 0;
            foreach (var list in _playerListObjects.Values)
            {
                // Punch each transform into existence
                RectTransform rectTransform = list.transform as RectTransform;
                var tweenActive = rectTransform.DOScale(Vector3.zero, 0f);
                var tween = rectTransform.DOScale(new Vector3(3f,3f,3f), 0.4f).SetEase(Ease.OutBack).From();

                // Start moving next list after the previous is half way done
                sequence.Insert(0f, tweenActive);  // Scale to 0 at the beginning so the list isnt shown
                sequence.Insert(seqPosition, tween);
                seqPosition += 0.5f;
            }

            sequence.Play();
        }

        public void HideAllPlayersShoppingLists()
        {
            _playerListObjects.Values.Map(x => x.gameObject.SetActive(false));
        }

        public void RemoveBlur()
        {
            _blur.enabled = false;
        }

        private void TutorialStageChanged(int stage)
        {
            //// Don't hide previous stage stickers
            //if (_stackTutorialStageStickers)
            //    _stageHeaderObjects[stage].SetActive(true);

            //// Hide previous stage stickers
            //else
            //{
            //    for (int i = 0; i < _stageHeaderObjects.Length; i++)
            //    {
            //        _stageHeaderObjects[i].SetActive(i == stage);
            //    }
            //}
        }

        
    }
}