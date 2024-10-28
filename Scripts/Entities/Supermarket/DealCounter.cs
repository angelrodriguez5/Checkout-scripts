using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Spawns a deal item after a certain amount of time, then only when the item is picked up 
/// the timer for spawning the next item starts
/// </summary>
public class DealCounter : MonoBehaviour
{
    [SerializeField] protected Transform _spawnPoint;
    [SerializeField] protected float _spawnInterval = 25f;
    [SerializeField] protected float _spawnVariance = 2f;
    [Tooltip("If a deal is spawned and not picked up in this amount of time, it will dissapear")]
    [SerializeField] float _dealDespawnTime = 5f;
    [SerializeField] protected Animator _animator;
    [SerializeField] AudioClip _warningWindUpSound;
    [SerializeField] protected AudioClip _spawnSound;
    [SerializeField] AudioSource _despawnWarningLoopSource;

    protected static readonly int _animIDdealPresent = Animator.StringToHash("dealPresent");
    protected static readonly int _animIDdespawnSecondsRemaining = Animator.StringToHash("despawnSecondsRemaining");
    protected static readonly int _animIDStartWindupTrigger = Animator.StringToHash("spawnWindup");
    protected static readonly int _animIDResetTrigger = Animator.StringToHash("reset");

    protected bool _dealDespawnCountdownActive;
    protected float _dealDespawCounter;

    public static event Action onDealStartWindup;

    public UnityEvent onDealSpawn;

    protected ItemBehaviour _currentDealItem;

    public bool IsDealPresent { get; protected set; }

    private void OnEnable()
    {
        GameManager.onMatchStarted += ScheduleDealSpawn;
        GameManager.onMatchTiebreaker += DisableDeals;
        GameManager.onMatchFinished += DisableDeals;
        GameManager.onItemDelivered += CheckDealDelivered;
    }

    private void OnDisable()
    {
        GameManager.onMatchStarted -= ScheduleDealSpawn;
        GameManager.onMatchTiebreaker -= DisableDeals;
        GameManager.onMatchFinished -= DisableDeals;
        GameManager.onItemDelivered -= CheckDealDelivered;
    }

    private void Update()
    {
        if (_dealDespawnCountdownActive)
        {
            _dealDespawCounter -= Time.deltaTime;
            _animator.SetFloat(_animIDdespawnSecondsRemaining, _dealDespawCounter);

            if (_dealDespawCounter <= 3f && !_despawnWarningLoopSource.isPlaying)
            {
                _despawnWarningLoopSource.Play();
            }

            // A deal has not been picked up before the time passes, destroy it if no players are near (disabled)
            //if(_dealDespawCounter <= 0 && !Physics.CheckSphere(transform.position, 7f, Layers.Player))
            if (_dealDespawCounter <= 0)
            {
                _dealDespawnCountdownActive = false;
                _currentDealItem.onItemGrabbed -= DetectDealPickedUp;
                Destroy(_currentDealItem.gameObject, 0.1f);
                _currentDealItem = null;
                _animator.SetBool(_animIDdealPresent, false);
                _despawnWarningLoopSource.Stop();

                // Schedule a new deal to spawn
                ScheduleDealSpawn();
            }
        }
    }

    public virtual void SpawnDealAnimEvent()
    {
        var item = Supermarket.Instance.SpawnDealItem();
        item.transform.position = _spawnPoint.position;
        IsDealPresent = true;
        _currentDealItem = item.GetComponent<ItemBehaviour>();
        _currentDealItem.onItemGrabbed += DetectDealPickedUp;
        _animator.SetBool(_animIDdealPresent, true);

        _dealDespawnCountdownActive = true;
        _dealDespawCounter = _dealDespawnTime;

        AudioManager.Instance.EffectSource.PlayOneShot(_spawnSound);
        onDealSpawn?.Invoke();
    }

    public void WarningWindUpSoundAnimEvent()
    {
        if (_warningWindUpSound)
            AudioManager.Instance.EffectSource.PlayOneShot(_warningWindUpSound);
    }

    public void AttachDealToSpawnPoint()
    {
        _currentDealItem.AttachTo(_spawnPoint, Vector3.zero, Quaternion.identity, false);
    }
    
    public void ScheduleDealSpawn() => StartCoroutine(_SpawnDeals());
    protected virtual IEnumerator _SpawnDeals()
    {
        // Disable deals in these gamemodes
        if (GameSettings.Current.matchGamemode == EGamemode.Pandemic)
            yield break;

        // Wait before starting to wind up
        yield return new WaitForSeconds(_spawnInterval + UnityEngine.Random.Range(-_spawnVariance, _spawnVariance));

        // Start wind up animation
        onDealStartWindup?.Invoke();
        _animator.SetTrigger(_animIDStartWindupTrigger);
        // Deal item will be spawned from animation event
    }

    private void DisableDeals()
    {
        // Stop spawning routine
        StopAllCoroutines();
        _dealDespawnCountdownActive = false;

        // Return animator to idle, remove current deal if present
        _animator.SetTrigger(_animIDResetTrigger);
        _animator.SetBool(_animIDdealPresent, false);
        _despawnWarningLoopSource.Stop();

        if (_currentDealItem)
        {
            // Avoid memory leak
            if (_animator.GetBool(_animIDdealPresent))
                _currentDealItem.onItemGrabbed -= DetectDealPickedUp;

            // If the item is being carried by a player dispose of it properly
            if (_currentDealItem.BelongsTo && (_currentDealItem.BelongsTo.ObjectHeld as ItemBehaviour) == _currentDealItem)
                _currentDealItem.BelongsTo.DropItem();

            Destroy(_currentDealItem.gameObject);
            _currentDealItem = null;
        }
    }

    private void CheckDealDelivered(PlayerAsset player, ItemAsset item)
    {
        // WARNING: this asumes only 1 deal stand will exist in the scene
        if (_currentDealItem && _currentDealItem.ItemAsset == item)
        {
            // Only start next deal spawn cycle once the previous deal was delivered
            ScheduleDealSpawn();
            _currentDealItem = null;
        }
    }

    protected virtual void DetectDealPickedUp()
    {
        _currentDealItem.onItemGrabbed -= DetectDealPickedUp;
        _animator.SetBool(_animIDdealPresent, false);
        _dealDespawnCountdownActive = false;
        IsDealPresent = false;
        _despawnWarningLoopSource.Stop();
    }

}
