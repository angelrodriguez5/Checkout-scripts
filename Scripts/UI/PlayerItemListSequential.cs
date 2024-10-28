using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlayerItemListSequential : MonoBehaviour, IPlayerItemList
{
    [System.Serializable]
    private struct ItemMarkerData
    {
        public PlayerColor color;
        public Sprite markerSprite;
        public Sprite backgroundSprite;
    }

    [SerializeField] Image _currentItemImage;
    [SerializeField] GameObject _itemsRemainingLayout;

    [SerializeField] private Image _shoppingListImage;
    [SerializeField] private Color _shoppingListColorTint;
    [SerializeField] private Sprite[] _shoppingListImageChoices;
    [SerializeField] private ItemMarkerData[] _itemMarkers;

    private PlayerAsset _owner;

    private void OnDestroy()
    {
        // Allow response to itemDelivered event even when disabled, only unsuscribe when destroyed
        if (_owner) GameManager.onItemDelivered -= ItemDelivered;
    }

    public void Initialise(PlayerAsset player, List<ItemAsset> items)
    {
        // Set owner
        _owner = player;

        // subscribe to item delivered event
        GameManager.onItemDelivered += ItemDelivered;

        // Set shopping list shape and color
        _shoppingListImage.sprite = _shoppingListImageChoices.GetRandomElement();
        _shoppingListImage.color = player.PlayerColor;
        _shoppingListImage.color += _shoppingListColorTint;

        // Set image markers to the correct width to fit the layout
        var spritesData = _itemMarkers.Where(x => x.color == _owner.ColorAsset).First();
        _itemsRemainingLayout.GetComponent<Image>().sprite = spritesData.backgroundSprite;
        int numItems = GameManager.Instance.GetRemainingItemsForPlayer(_owner).Count;
        for (int i = 0; i < _itemsRemainingLayout.transform.childCount; i++)
        {
            _itemsRemainingLayout.transform.GetChild(i).GetComponent<Image>().sprite = spritesData.markerSprite;
            _itemsRemainingLayout.transform.GetChild(i).gameObject.SetActive(i < numItems);
        }
        LayoutRebuilder.MarkLayoutForRebuild(_itemsRemainingLayout.transform as RectTransform);
        StartCoroutine(PrepareLayoutForGameAfterDelay());

        // Show the first object
        var item = GameManager.Instance.GetRemainingItemsForPlayer(_owner).Last();
        _currentItemImage.sprite = item.ItemIcon;
    }

    public void UpdateUI()
    {
        // Set remaining item markers
        int numItems = GameManager.Instance.GetRemainingItemsForPlayer(_owner).Count;
        for (int i = 0; i < _itemsRemainingLayout.transform.childCount; i++)
        {
            // Activate one marker per item
            _itemsRemainingLayout.transform.GetChild(i).gameObject.SetActive(i < numItems);
        }
        LayoutRebuilder.MarkLayoutForRebuild(_itemsRemainingLayout.transform as RectTransform);

        // Set current item image
        if (GameManager.Instance.GetRemainingItemsForPlayer(_owner).Count > 0)
        {
            var item = GameManager.Instance.GetRemainingItemsForPlayer(_owner).Last();
            _currentItemImage.sprite = item.ItemIcon;
        }
        else
        {
            _currentItemImage.sprite = null;
            _currentItemImage.color = new Color(0f, 0f, 0f, 0f);
        }
    }

    private void ItemDelivered(PlayerAsset player, ItemAsset item)
    {
        if (player == _owner)
        {
            UpdateUI();
        }
    }

    private IEnumerator PrepareLayoutForGameAfterDelay()
    {
        // Let the layout be rebuilt so the total amount of markers fill the bar
        var eof = new WaitForEndOfFrame();
        while (!gameObject.activeInHierarchy)
            yield return eof;  // Busy wait =(

        // Don't rescale the markers when turning in items
        yield return eof;
        var layout = _itemsRemainingLayout.GetComponent<HorizontalLayoutGroup>();
        layout.childControlWidth = false;
        layout.childForceExpandWidth = false;
    }
}
