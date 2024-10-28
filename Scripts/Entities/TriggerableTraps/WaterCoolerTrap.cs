using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// Water cooler that, when interacted by the player, will spawn a water puddle
/// on the opposite side of where the interaction took place.
/// The puddle will disapear after a certain time or when stepped on.
/// The trap will be unavailable for a cooldown after activating it
/// </summary>
[RequireComponent(typeof(Outline), typeof(Animator))]
public class WaterCoolerTrap : MonoBehaviour, IInteractive
{
    [SerializeField] float _cooldown = 20f;
    [SerializeField] float _puddleDuration = 15f;

    [SerializeField] GameObject _leftPuddle, _rightPuddle;

    [SerializeField] private UnityEvent OnEnterCooldown, OnExitCooldown;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _splashSound;

    private static readonly int _animIDTipTrigger = Animator.StringToHash("Tip");
    private static readonly int _animIDTipLeft = Animator.StringToHash("TipLeft");

    Animator _animator;

    bool _isOnCooldown = false;
    Coroutine _leftRoutine, _rightRoutine;
    Outline _outline;
    int _playersLooking = 0;

    public bool IsOnCooldown {
        get => _isOnCooldown;
        set 
        {
            _isOnCooldown = value;
            if (_isOnCooldown)
                OnEnterCooldown?.Invoke();
            else
                OnExitCooldown?.Invoke();
        }
    }

    private void Awake()
    {
        _outline = GetComponent<Outline>();
        _animator = GetComponent<Animator>();

        _leftPuddle.SetActive(false);
        _rightPuddle.SetActive(false);
    }

    private void OnEnable()
    {
        _outline.enabled = false;
    }

    #region IInteractive
    public EInteractivePriorityType Priority => EInteractivePriorityType.Lowest;


    public bool CanInteract(GameObject interactor) => !IsOnCooldown;

    public void Select()
    {
        _outline.enabled = true;
        _playersLooking++;
    }

    public void Deselect()
    {
        _playersLooking--;
        _outline.enabled = _playersLooking > 0;
    }

    public void Interact(GameObject interactor)
    {
        // Find wether the player was left or right of the cooler when they interacted
        Vector3 toPlayerLocal = transform.InverseTransformDirection(interactor.transform.position - transform.position);

        if (toPlayerLocal.x > 0f)
        {
            // If routine was already running reset it
            if (_leftRoutine != null)
                StopCoroutine(_leftRoutine);
            _leftRoutine = StartCoroutine(EnablePuddle(_leftPuddle, true));
        }
        else
        {
            if (_rightRoutine != null)
                StopCoroutine(_rightRoutine);
            _rightRoutine = StartCoroutine(EnablePuddle(_rightPuddle, false));
        }
    }
    #endregion

    private IEnumerator EnablePuddle(GameObject puddle, bool isLeftPuddle)
    {
        _animator.SetBool(_animIDTipLeft, isLeftPuddle);
        _animator.SetTrigger(_animIDTipTrigger);
        yield return new WaitForSeconds(0.05f);

        puddle.SetActive(true);
        IsOnCooldown = true;

        _audioSource.PlayOneShot(_splashSound);

        if (_cooldown < _puddleDuration)
        {
            yield return new WaitForSeconds(_cooldown);
            IsOnCooldown = false;
            yield return new WaitForSeconds(_puddleDuration - _cooldown);
            puddle.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(_puddleDuration);
            puddle.SetActive(false);
            yield return new WaitForSeconds(_cooldown - _puddleDuration);
            IsOnCooldown = false;
        }

        // Set the corresponding routine as finished
        if (puddle == _rightPuddle)
            _rightRoutine = null;
        else
            _leftRoutine = null;
    }
}
