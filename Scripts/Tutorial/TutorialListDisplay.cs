using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tutorial
{
    public class TutorialListDisplay : MonoBehaviour, IPlayerItemList
    {
        [SerializeField] private GameObject _itemIconPrefab;
        [SerializeField] private Transform _itemsLayoutContainer;

        [SerializeField] private Image _shoppingListImage;
        [SerializeField] private Color _shoppingListColorTint;
        [SerializeField] private Sprite[] _shoppingListImageChoices;

        private PlayerAsset _owner;
        private Dictionary<ItemAsset, GameObject> _uiItemDict = new Dictionary<ItemAsset, GameObject>();
        private Queue<Sprite> _availableShoppingListImages;

        private void Awake()
        {
            _availableShoppingListImages = new Queue<Sprite>(_shoppingListImageChoices.Shuffle());
        }

        private void OnDestroy()
        {
            // Allow response to itemDelivered event even when disabled, only unsuscribe when destroyed
            if (_owner) GameManager.onItemDelivered -= ItemDelivered;
        }

        public void Initialise(PlayerAsset player, List<ItemAsset> items)
        {
            // Set owner
            _owner = player;

            // Create images for items
            foreach (var item in items)
            {
                var icon = Instantiate(_itemIconPrefab, _itemsLayoutContainer);
                icon.GetComponent<Image>().sprite = item.ItemIcon;

                _uiItemDict.Add(item, icon);
            }

            // subscribe to item delivered event
            GameManager.onItemDelivered += ItemDelivered;

            // Set shopping list shape and color
            _shoppingListImage.sprite = _availableShoppingListImages.Dequeue();
            _shoppingListImage.color = _owner.PlayerColor;
            _shoppingListImage.color += _shoppingListColorTint;

            LayoutRebuilder.MarkLayoutForRebuild(_itemsLayoutContainer.transform as RectTransform);
        }

        public void UpdateUI()
        {
            var playerItems = GameManager.Instance.playerShoppingLists[_owner];

            foreach (var entry in _uiItemDict)
            {
                entry.Value.SetActive(playerItems.Contains(entry.Key));
            }
        }

        public void AddItem(ItemAsset item)
        {
            var icon = Instantiate(_itemIconPrefab, _itemsLayoutContainer);
            icon.GetComponent<Image>().sprite = item.ItemIcon;

            _uiItemDict.Add(item, icon);
        }

        public void RemoveItem(ItemAsset item)
        {
            Destroy(_uiItemDict[item].gameObject);
            _uiItemDict.Remove(item);
        }

        private void ItemDelivered(PlayerAsset player, ItemAsset item)
        {
            if (player == _owner)
            {
                // Hide the delivered item icon in the shopping list
                if (_uiItemDict.TryGetValue(item, out var icon))
                {
                    icon.SetActive(false);
                }

                LayoutRebuilder.MarkLayoutForRebuild(_itemsLayoutContainer.transform as RectTransform);
            }
        }
    }
}