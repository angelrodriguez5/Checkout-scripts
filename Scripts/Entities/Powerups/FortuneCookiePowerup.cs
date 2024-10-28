using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class FortuneCookiePowerup : BasePowerup
{
    [SerializeField] GameObject _canvas;
    [SerializeField] Image _itemIconImage;

    [SerializeField] private Image _shoppingListImage;
    [SerializeField] private Color _shoppingListColorTint;
    [SerializeField] private Sprite[] _shoppingListImageChoices;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _fortuneMunchie;

    private Queue<Sprite> _availableShoppingListImages;

    public UnityEvent onCookieSpawn;

    protected override void TriggerPowerup(PlayerController playerController) => StartCoroutine(_TriggerPowerup(playerController));
    private IEnumerator _TriggerPowerup(PlayerController playerController)
    {
        // Create a randomized queue of shopping lists
        _availableShoppingListImages = new Queue<Sprite>(_shoppingListImageChoices.Shuffle());

        // Play pick up sound
        _audioSource.PlayOneShot(_fortuneMunchie);

        // Disable the collider so no other players trigger this powerup
        _collider.enabled = false;

        // Select a random item to show
        var item = GameManager.Instance.GetRemainingItemsForPlayer(playerController.PlayerAsset).GetRandomElement();

        // Display it in the UI
        _itemIconImage.sprite = item.ItemIcon;
        //_playerColorImage.color = playerController.PlayerAsset.PlayerColor;

        // Set shopping list shape and color
        _shoppingListImage.sprite = _availableShoppingListImages.Dequeue();
        _shoppingListImage.color = playerController.PlayerAsset.PlayerColor;
        _shoppingListImage.color += _shoppingListColorTint;

        // Show it for a certain amount of time and then destroy this
        _canvas.SetActive(true);
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }
}
