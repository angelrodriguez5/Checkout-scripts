using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerItemListCovid : MonoBehaviour, IPlayerItemList
{
    [SerializeField] private TMP_Text _counterText;
    [SerializeField] private Image _shoppingListImage;
    [SerializeField] private Color _shoppingListColorTint;
    [SerializeField] private Sprite[] _shoppingListImageChoices;
    
    private PlayerAsset _owner;
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
        _counterText.text = "0";

        // subscribe to item delivered event
        GameManager.onItemDelivered += ItemDelivered;

        // Set shopping list shape and color
        _shoppingListImage.sprite = _availableShoppingListImages.Dequeue();
        _shoppingListImage.color = _owner.PlayerColor;
        _shoppingListImage.color += _shoppingListColorTint;
    }

    public void UpdateUI()
    {
        var playerItems = GameManager.Instance.playerCovidItemsTurnedIn[_owner];

        _counterText.text = $"{playerItems}";
    }

    private void ItemDelivered(PlayerAsset player, ItemAsset item)
    {
        if (player == _owner)
        {
            _counterText.text = $"{GameManager.Instance.playerCovidItemsTurnedIn[_owner]}";
        }
    }
}
