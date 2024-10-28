using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// Crystal zapper that, when interacted by the player, will take a little to charge up.
/// When fully charged the next player that gets in range will be zapped.
/// The trap discharges and enters cooldown after a player is zapped or after a set amount of time.
/// </summary>
[RequireComponent(typeof(Outline), typeof(Animator))]
public class CrystalZapper : MonoBehaviour, IInteractive
{
    [SerializeField] float _cooldown = 15f;
    [SerializeField] float _chargeDuration = 10f;
    [SerializeField] float _radius = 4f;
    [SerializeField] LayerMask _playerLayer;
    [SerializeField] ParticleSystem _shockParticleFX;

    [SerializeField] private UnityEvent OnEnterCooldown, OnExitCooldown;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _chargingSound;
    [SerializeField] private AudioClip _activationSound;
    [SerializeField] private AudioClip _chargedLoopSound;
    [SerializeField] private AudioClip _electrocutePlayerSound;

    private static readonly int _animIDBeginCharging = Animator.StringToHash("BeginCharging");
    private static readonly int _animIDReturnToIdle = Animator.StringToHash("ReturnToIdle");
    private static readonly int _animIDIsOnCooldown = Animator.StringToHash("IsOnCooldown");

    Animator _animator;
    Outline _outline;

    bool _isOnCooldown = false;
    int _playersLooking = 0;
    bool _isCharging;
    bool _isFullyCharged;
    float _chargedTime;

    public bool IsOnCooldown
    {
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
    }

    private void OnEnable()
    {
        _outline.enabled = false;
    }

    private void Update()
    {
        if (_isFullyCharged)
        {
            // Stay charged for a certain amount of time then return to idle
            _chargedTime += Time.deltaTime;
            if (_chargedTime >= _chargeDuration)
            {
                _isFullyCharged = false;
                _animator.SetTrigger(_animIDReturnToIdle);
                _audioSource.Stop();
            }
            else
            {
                // When fully charged hit the first player that walks in range and discharge the zapper
                var hits = Physics.OverlapSphere(transform.position, _radius, _playerLayer);

                bool didHit = false;
                foreach (var hit in hits)
                {
                    if (hit.TryGetComponent<PlayerController>(out var controller))
                    {
                        controller.Knockdown(controller.transform.position - transform.position);
                        didHit = true;

                        _shockParticleFX.transform.LookAt(controller.transform.position + new Vector3(0f,0.5f,0f));
                        _shockParticleFX.gameObject.SetActive(true);
                    }
                }

                if (didHit)
                {
                    _isFullyCharged = false;
                    StartCoroutine(CooldownRoutine());

                    _audioSource.Stop();
                    _audioSource.PlayOneShot(_electrocutePlayerSound);
                }
            }
        }
    }

    /// <summary>
    /// Called from animation event to signal when the zapper is fully charged
    /// </summary>
    public void AnimEventFullCharge()
    {
        _isCharging = false;
        _isFullyCharged = true;
        _chargedTime = 0f;

        _audioSource.PlayOneShot(_activationSound);
        _audioSource.loop = true;
        _audioSource.clip = _chargedLoopSound;
        _audioSource.Play();
    }

    private IEnumerator CooldownRoutine()
    {
        IsOnCooldown = true;
        _animator.SetBool(_animIDIsOnCooldown, IsOnCooldown);

        yield return new WaitForSeconds(_cooldown);

        IsOnCooldown = false;
        _animator.SetBool(_animIDIsOnCooldown, IsOnCooldown);
    }

    #region IInteractive
    public EInteractivePriorityType Priority => EInteractivePriorityType.Lowest;

    public bool CanInteract(GameObject interactor) => !IsOnCooldown && !_isCharging && !_isFullyCharged;

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
        _isCharging = true;
        _animator.SetTrigger(_animIDBeginCharging);

        _audioSource.loop = false;
        _audioSource.clip = _chargingSound;
        _audioSource.Play();
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
