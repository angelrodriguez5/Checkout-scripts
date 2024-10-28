using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

/// <summary>
/// Laser turret trap composed by two turrets, one at either side of a corridor. 
/// When activated by the player a laser will join the two turrets and knockdown any player that touches it
/// </summary>
[RequireComponent(typeof(Outline), typeof(Animator))]
public class LaserTurret : MonoBehaviour, IInteractive
{
    [Header("Config")]
    [SerializeField] float maxDuration = 6f;
    public Transform laserEmmiter;
    [SerializeField] ParticleSystem _laserParticles;
    [SerializeField] ParticleSystem _chargeParticles;
    [SerializeField] LaserTurret _otherSideTurret;
    [SerializeField] Cinemachine.CinemachineImpulseSource _startupShake;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _chargingSound;
    [SerializeField] private AudioClip _activeSound;

    private static readonly int _animIDButtonPressed = Animator.StringToHash("ButtonPressed");

    Animator _animator;
    Outline _outline;

    int _playersLooking = 0;
    bool _isLaserActive;
    bool _isSlave;
    Ray _laserRay;
    float _laserRayLenght;

    bool _isOnCooldown;
    public bool IsOnCooldown 
    {
        get => _isOnCooldown;
        set
        {
            _isOnCooldown = value;
            if (value)
            {

            }
            else
            {
                // Remove slave status when exiting cooldown
                _isSlave = false;
            }
        }
    }

    #region IInteractive
    public EInteractivePriorityType Priority => EInteractivePriorityType.Lowest;

    public bool CanInteract(GameObject interactor) => !IsOnCooldown && !_isLaserActive && !_isSlave;

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
        ActivateLaser(false);
    }
    #endregion

    private void Awake()
    {
        _outline = GetComponent<Outline>();
        _animator = GetComponent<Animator>();

        _laserRay = new Ray(laserEmmiter.position, _otherSideTurret.laserEmmiter.position - laserEmmiter.position);
        _laserRayLenght = Vector3.Distance(laserEmmiter.position, _otherSideTurret.laserEmmiter.position);
    }

    private void OnEnable()
    {
        _outline.enabled = false;
    }

    private void Update()
    {
        if (_isLaserActive && !_isSlave)  // Only check collision in the master turret
        {
            if (Physics.Raycast(_laserRay, out var hitInfo, _laserRayLenght, Layers.Player))
            {
                if (hitInfo.collider.TryGetComponent<PlayerController>(out var playerController))
                {
                    // Get vector perpendicular to laser
                    var dir = Vector3.Cross(_laserRay.direction, Vector3.up);
                    // Choose the direction based on which side of the laser the player is at
                    dir = Vector3.Dot(dir, playerController.transform.position - laserEmmiter.position) < 0 ? -dir : dir;

                    playerController.Knockdown(dir);

                    // Reset both sides of the laser
                    StopAllCoroutines();
                    DeactivateLaser();
                    _otherSideTurret.DeactivateLaser();
                }
            }
        }
    }

    public void ActivateLaser(bool isSlave)
    {
        _isSlave = isSlave;
        IsOnCooldown = true;
        _outline.enabled = false;

        if (!isSlave)
        {
            // Mark the other side of the laser as a slave so it does no calculations
            _otherSideTurret.ActivateLaser(true);

            // Setup particles
            var mainModule = _laserParticles.main;
            mainModule.startSizeY = _laserRayLenght;

            StartCoroutine(LaserChargeRoutine());
        }

        // Both sides of the laser
        _chargeParticles.transform.LookAt(_otherSideTurret.laserEmmiter);
        _chargeParticles.Play();
        _animator.SetBool(_animIDButtonPressed, true);
    }

    public void DeactivateLaser()
    {
        _isSlave = false;
        _isLaserActive = false;
        IsOnCooldown = false;
        _laserParticles.Stop();
        _chargeParticles.Stop();
        _animator.SetBool(_animIDButtonPressed, false);
        StartCoroutine(_audioSource.FadeOut());
    }

    private IEnumerator LaserChargeRoutine()
    {
        // Laser start delay, linked to charge particles duration
        _audioSource.PlayOneShot(_chargingSound);
        yield return new WaitForSeconds(1.3f);
        _isLaserActive = true;

        // Start laser beam particles
        _laserParticles.transform.LookAt(_otherSideTurret.laserEmmiter);
        _laserParticles.Play();
        _startupShake.GenerateImpulse();
        _audioSource.PlayOneShot(_activeSound);

        // If nobody got hit stop the laser after some time
        yield return new WaitForSeconds(maxDuration);
        DeactivateLaser();
        _otherSideTurret.DeactivateLaser();
    }

    private void OnDrawGizmos()
    {
        if (!_otherSideTurret) return;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(laserEmmiter.position, (_otherSideTurret.laserEmmiter.position - laserEmmiter.position).normalized * Vector3.Distance(laserEmmiter.position, _otherSideTurret.laserEmmiter.position));
    }
}
