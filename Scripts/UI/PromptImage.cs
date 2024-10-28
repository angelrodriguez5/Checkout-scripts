using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class PromptImage : MonoBehaviour
{
    public Sprite keyboardPrompt;
    public Sprite controllerPrompt;

    Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    private void OnEnable()
    {
        // On the main menu keep track of the main menu player input
        if (MainMenuManager.Instance)
        {
            MainMenuManager.Instance.mainMenuPlayerInput.controlsChangedEvent.AddListener(ChangePrompt);
            ChangePrompt(MainMenuManager.Instance.mainMenuPlayerInput);
        }
        // On the rest of the game check the player that is controlling the interface
        else
        {
            GameManager.onCurrentPlayerControllingUIChanged += ChangePrompt;
            ChangePrompt(GameManager.Instance.CurrentPlayerControllingUI);
        }
    }

    private void OnDisable()
    {
        if (MainMenuManager.Instance)
        {
            MainMenuManager.Instance.mainMenuPlayerInput.controlsChangedEvent.RemoveListener(ChangePrompt);
        }
        else
        {
            GameManager.onCurrentPlayerControllingUIChanged -= ChangePrompt;
        }
    }

    private void ChangePrompt(PlayerInput playerInput)
    {
        if (playerInput.currentControlScheme.Contains("Keyboard"))
        {
            _image.sprite = keyboardPrompt;
        }
        else if (playerInput.currentControlScheme.Contains("Gamepad") || playerInput.currentControlScheme.Contains("Controller"))
        {
            _image.sprite = controllerPrompt;
        }
    }
}
