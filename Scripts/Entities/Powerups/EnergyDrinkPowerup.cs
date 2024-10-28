using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyDrinkPowerup : BasePowerup
{
    [SerializeField] float _showListDuration = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip _energyDrink;

    protected override void TriggerPowerup(PlayerController playerController) => StartCoroutine(_TriggerPowerup(playerController));
    private IEnumerator _TriggerPowerup(PlayerController playerController)
    {
        // Disable the collider so no other players trigger this powerup
        _collider.enabled = false;

        // Play pick up sound
        AudioManager.Instance.EffectSource.PlayOneShot(_energyDrink);

        GameManager.Instance.GameUI.ShowPlayerShoppingList(playerController.PlayerAsset, _showListDuration);
        yield return null;
        Destroy(gameObject, 0.4f);
    }

}
