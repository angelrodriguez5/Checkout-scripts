using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class PlayerSelectionController : MonoBehaviour
{
    [SerializeField] private int _order;
    [SerializeField] private GameObject _placeholder;
    [SerializeField] private GameObject _readyText;
    [SerializeField] private Image _readyButtonImage;

    [SerializeField] private Sprite _keyboardWASDReadyImage;
    [SerializeField] private Sprite _keyboardArrowsReadyImage;
    [SerializeField] private Sprite _controllerReadyImage;

    [Header("Audio")]
    [SerializeField] private AudioClip _playerJoinedAudioClip;
    [SerializeField] private AudioClip _playerLeftAudioClip;
    [SerializeField] private AudioClip _readyAudioClip;
    [SerializeField] private AudioClip _unreadyAudioClip;

    public PlayerController Player { get; private set; }
    public bool IsReady { get; set; }
    public int Order => _order;

    private void Awake()
    {
        _placeholder.SetActive(true);
        _readyText.SetActive(false);
        _readyButtonImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// Moves player to this spawn point
    /// </summary>
    /// <param name="playerInput"> If player is null a placeholder text will be displayed</param>
    public void SetPlayer(PlayerInput playerInput)
    {
        // Remove player
        if (playerInput == null)
        {
            Player.PlayerAsset.Active = false;
            Player.PlayerInput.actions.FindAction("Submit").performed -= Submit;
            Player.PlayerInput.actions.FindAction("Cancel").performed -= Cancel;
            Player.PlayerInput.actions.FindAction("AuxButton").performed -= AuxButton;
            Player.PlayerInput.DeactivateInput();

            StartCoroutine(DelayDestroyPlayer(Player.PlayerInput));
            Player = null;

            _placeholder.SetActive(true);
            _readyText.SetActive(false);
            _readyButtonImage.gameObject.SetActive(false);

            AudioManager.Instance.UiSource.PlayOneShot(_playerLeftAudioClip);
        }
        // Add player
        else
        {
            Player = playerInput.GetComponent<PlayerController>();
            playerInput.actions.FindAction("Submit").performed += Submit;
            playerInput.actions.FindAction("Cancel").performed += Cancel;
            playerInput.actions.FindAction("AuxButton").performed += AuxButton;
            InitialisePlayer(playerInput);
            Player.AllowSkinChange = true;

            if (playerInput.currentControlScheme == "KeyboardWASD")
                _readyButtonImage.sprite = _keyboardWASDReadyImage;
            else if (playerInput.currentControlScheme == "KeyboardArrows")
                _readyButtonImage.sprite = _keyboardArrowsReadyImage;
            else if (playerInput.currentControlScheme == "Gamepad")
                _readyButtonImage.sprite = _controllerReadyImage;
            else
                Debug.Log($"{nameof(PlayerSelectionController)}: couldn't presonalize ready button image, no configuration set for control scheme \"{playerInput.currentControlScheme}\"");

            _placeholder.SetActive(false);
            _readyText.SetActive(false);
            _readyButtonImage.gameObject.SetActive(true);

            AudioManager.Instance.UiSource.PlayOneShot(_playerJoinedAudioClip);
        }

    }

    private void InitialisePlayer(PlayerInput playerInput)
    {
        // Tp player to this position
        Player.Teleport(this.transform);
    }

    private void Submit(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        // Only play sound when pressing submit if not ready
        if (!IsReady)
            AudioManager.Instance.UiSource.PlayOneShot(_readyAudioClip);

        IsReady = true;
        Player.AllowSkinChange = false;
        _readyText.SetActive(true);
        _readyButtonImage.gameObject.SetActive(false);

        // Hold A to play
        MainMenuManager.Instance.isPlayButtonPressed = ctx.ReadValueAsButton();
    }

    private void Cancel(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        // If we press cancel action while ready, stop being ready
        if (IsReady)
        {
            IsReady = false;
            Player.AllowSkinChange = true;
            _readyText.SetActive(false);
            _readyButtonImage.gameObject.SetActive(true);

            AudioManager.Instance.UiSource.PlayOneShot(_unreadyAudioClip);
        }
        // If we were not ready, delete player
        else
        {
            SetPlayer(null);
        }
    }

    private void AuxButton(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        //MainMenuManager.Instance.isPlayButtonPressed = ctx.ReadValueAsButton();
    }

    private IEnumerator DelayDestroyPlayer(PlayerInput playerInput)
    {
        playerInput.uiInputModule = null;
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        Destroy(playerInput.gameObject);
        yield return null;
    }
}
