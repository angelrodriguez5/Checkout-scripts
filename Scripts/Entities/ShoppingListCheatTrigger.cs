using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// A trigger area that is enabled after a certain amount of time passes after the beginning
/// of the match. Only active in Memory gamemode. When a player stands on it his shopping
/// list will appear on the UI.
/// 
/// All visual elements must be in child objects.
/// </summary>
[RequireComponent(typeof(Collider))]  // Trigger
public class ShoppingListCheatTrigger : MonoBehaviour
{
    Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    void Start()
    {
        if (GameManager.Instance.GameSettings.matchGamemode != EGamemode.Memory)
            Destroy(gameObject);

        HideChildren();
    }

    private void OnEnable()
    {
        GameManager.onMatchStarted += StartCountdown;
        GameManager.onMatchFinished += HideChildren;
    }

    private void OnDestroy()
    {
        GameManager.onMatchStarted -= StartCountdown;
        GameManager.onMatchFinished -= HideChildren;
    }

    private void OnTriggerEnter(Collider other)
    {
        var playerController = other.gameObject.GetComponent<PlayerController>();
        if (!playerController) return;

        GameManager.Instance.GameUI.ShowPlayerShoppingList(playerController.PlayerAsset);
    }

    private void OnTriggerExit(Collider other)
    {
        var playerController = other.gameObject.GetComponent<PlayerController>();
        if (!playerController) return;

        GameManager.Instance.GameUI.HidePlayerShoppingList(playerController.PlayerAsset);
    }

    private void StartCountdown() => StartCoroutine(_StartCountdown());
    private IEnumerator _StartCountdown()
    {
        yield return new WaitForSeconds(GameSettings.MEMORY_TIME_BEFORE_CHEAT_APPEARS);
        ShowChildren();
    }

    private void ShowChildren()
    {
        _collider.enabled = true;
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
        }
    }

    private void HideChildren()
    {
        _collider.enabled = false;
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }
}
