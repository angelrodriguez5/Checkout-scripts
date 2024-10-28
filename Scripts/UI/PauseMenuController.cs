using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject _firstSelected;
    [SerializeField] private Animator _animator;
    [SerializeField] private OptionsMenuController _optionsPanel;
    [SerializeField] private GameObject _controlsPanel;

    [SerializeField] private AudioClip _pauseMenuSound;
    [SerializeField] private AudioClip _submitSound;
    [SerializeField] private AudioClip _cancelSound;

    private void OnEnable()
    {
        GameManager.onGamePaused += ActivatePauseMenu;
    }


    private void OnDisable()
    {
        GameManager.onGamePaused -= ActivatePauseMenu;
    }

    public void ResumeButtonPressed()
    {
        GameManager.Instance?.TogglePause();
    }

    public void OptionsButtonPressed()
    {
        _optionsPanel.gameObject.SetActive(true);
        EventSystem.current.SetSelectedGameObject(_optionsPanel.firstSelected);
    }

    public void HowToPlayButtonPressed()
    {
        _controlsPanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void ExitButtonPressed()
    {
        GameManager.Instance?.ExitMatch();
    }

    private void CancelAction(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        // Options menu or controls panel: return to pause menu
        if (_optionsPanel.gameObject.activeInHierarchy || _controlsPanel.activeInHierarchy)
        {
            _optionsPanel.gameObject.SetActive(false);
            _controlsPanel.SetActive(false);
            EventSystem.current.SetSelectedGameObject(_firstSelected);
            AudioManager.Instance.UiSource.PlayOneShot(_cancelSound);
        }

        // Pause menu: unpause
        else
        {
            GameManager.Instance.TogglePause();
        }
    }


    private void SubmitAction(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        // Controls panel: return to pause menu
        if (_controlsPanel.gameObject.activeInHierarchy)
        {
            _controlsPanel.SetActive(false);
            AudioManager.Instance.UiSource.PlayOneShot(_submitSound);

            // Wait for the submit event to be consumed
            StartCoroutine(DelaySelectObject(_firstSelected));
        }
    }

    private void ActivatePauseMenu(bool paused)
    {
        if (paused)
        {
            // Set selected button
            EventSystem.current.SetSelectedGameObject(_firstSelected);
            var inputUIModule = (InputSystemUIInputModule)EventSystem.current.currentInputModule;
            inputUIModule.cancel.action.performed += CancelAction;
            inputUIModule.submit.action.performed += SubmitAction;
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(null);
            var inputUIModule = (InputSystemUIInputModule)EventSystem.current.currentInputModule;
            inputUIModule.cancel.action.performed -= CancelAction;
            inputUIModule.submit.action.performed -= SubmitAction;
        }

        // Play sound both when pausing and unpausing
        AudioManager.Instance.UiSource.PlayOneShot(_pauseMenuSound);
        _optionsPanel.gameObject.SetActive(false);
        _controlsPanel.SetActive(false);
        _animator.SetBool("isPaused", paused);
    }

    private IEnumerator DelaySelectObject(GameObject obj) 
    {
        yield return null;
        EventSystem.current.SetSelectedGameObject(obj);
    }
}
