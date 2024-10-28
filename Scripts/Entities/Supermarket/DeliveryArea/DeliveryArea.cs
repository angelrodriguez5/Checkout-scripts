using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using UnityEngine.UI;

/// <summary>
/// The counter where the player turns in the items. To use it
/// the player interacts with it while holding an item, then the
/// item takes a second to be processed. While an item is being processed
/// no other item can be turned in at this counter.
/// </summary>
[RequireComponent(typeof(Collider), typeof(Animator))]  // for IInteractive (probably trigger)
public class DeliveryArea : MonoBehaviour, IInteractive
{
    private struct QueueItemData
    {
        public PlayerController owner;
        public ItemBehaviour item;

        public QueueItemData(PlayerController owner, ItemBehaviour item)
        {
            this.owner = owner;
            this.item = item;
        }
    }

    public static List<DeliveryArea> allAreas = new List<DeliveryArea>();

    private static Dictionary<PlayerAsset, List<ItemAsset>> _allPlayerQueuedItems = new();

    private static readonly int _animIDScanItemTrigger = Animator.StringToHash("ScanItemTrigger");
    private static readonly int _animIDSuccessTrigger = Animator.StringToHash("SuccessTrigger");
    private static readonly int _animIDFailTrigger = Animator.StringToHash("FailTrigger");
    private static readonly int _animIDIsOpen = Animator.StringToHash("IsOpen");

    [Header("Config")]
    [SerializeField] private Transform[] _queuedItemPositions;
    [SerializeField] private Transform _counterTop;
    [SerializeField] private float _deliveryDelay = 0.8f;
    [SerializeField] private ParticleSystem _deliveredParticles;
    [SerializeField] private ItemQueueGUI _itemQueueGUI;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _successAudio;
    [SerializeField] private AudioClip _failAudio;
    [SerializeField] private AudioClip _scanItemAudio;
    [SerializeField] private AudioClip _closeAudio;
    [SerializeField] private AudioClip _openAudio;

    private Queue<QueueItemData> _itemQueue = new();
    private bool _isQueueProcessRoutineRunning;
    private bool _forceFinishProcessing;
    private int _bottomQueuePositionIdx = 0;
    private ItemBehaviour _currentItem;
    private bool _isReadyForNextItem;

    private Animator _animator;

    bool _isOpen = true;
    public bool IsOpen {
        get => _isOpen;
        set
        {
            // Play sound only when value is different from current value
            if (_isOpen != value)
            {
                if (value)
                {
                    _itemQueueGUI.ItemProcessed(true);  // Remove closed marker
                }
                else
                { 
                    _itemQueueGUI.AddItem(null, null, true);  // Add closed marker
                }
            }

            _isOpen = value;
            _animator.SetBool(_animIDIsOpen, IsOpen);

        }
    }
    public bool AnyItemSlotsAvailable { get; private set; } = true;
    // Used to force this delivery area to instantly process the current item
    public Coroutine ProcessQueueRoutine { get; private set; }

    #region IInteractive Implementation
    public EInteractivePriorityType Priority => EInteractivePriorityType.Lowest;

    public bool CanInteract(GameObject interactor)
    {
        if (interactor.TryGetComponent<PlayerController>(out var player))
        {
            return IsOpen && AnyItemSlotsAvailable && player.ObjectHeld != null;
        }
        return false;
    }

    public void Select() { }
    public void Deselect() { }
    public void Interact(GameObject interactor)
    {
        if (interactor.TryGetComponent<PlayerController>(out var player) && player.ObjectHeld is ItemBehaviour)
        {
            // Get item from player and deliver it
            var item = player.ObjectHeld;
            player.ObjectHeld = null;
            QueueItem(item as ItemBehaviour, player);
        }
    }
    #endregion

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        IsOpen = true;
    }

    private void OnEnable()
    {
        allAreas.Add(this);
    }

    private void OnDisable()
    {
        allAreas.Remove(this);
    }

    // Called from animation event
    public void AnimReadyToScan()
    {
        _isReadyForNextItem = true;
    }
    
    public void AnimScanItem()
    {
        _audioSource.PlayOneShot(_scanItemAudio);
    }

    public void AnimDeliverItem()
    {
        Destroy(_currentItem.gameObject);
        _audioSource.PlayOneShot(_successAudio);
        _deliveredParticles.Play();
    }

    public void AnimPlayOpenSound()
    {
        _audioSource.PlayOneShot(_openAudio);
    }

    public void AnimPlayClosedSound()
    {
        _audioSource.PlayOneShot(_closeAudio);
    }

    public void QueueItem(ItemBehaviour item, PlayerController player)
    {
        // Add item to the queue
        var itemData = new QueueItemData(player, item);
        _itemQueue.Enqueue(itemData);
        AnyItemSlotsAvailable = _itemQueue.Count < _queuedItemPositions.Length - 2;  
        // -1 to account for the item being processed (which is not on the queue anymore)
        // -1 for saving up a slot to insert the closed marker when the cashier closes

        // Update GUI
        _itemQueueGUI.AddItem(item, player);

        // Place item in the lowest available spot
        var queuePlacement = _queuedItemPositions[_bottomQueuePositionIdx];
        _bottomQueuePositionIdx = (_bottomQueuePositionIdx + 1) % _queuedItemPositions.Length;

        item.AttachTo(queuePlacement, Vector3.zero, Quaternion.identity);

        // Keep track of all players' items pending in all queues
        if(_allPlayerQueuedItems.ContainsKey(player.PlayerAsset))
        {
            _allPlayerQueuedItems[player.PlayerAsset].Add(item.ItemAsset);
        }
        else
        {
            _allPlayerQueuedItems.Add(player.PlayerAsset, new List<ItemAsset> { item.ItemAsset });
        }
        // If this item was the last on the player's list end the match
        CheckIsPlayerLastItem(player.PlayerAsset);

        // If queue was empty restart the checkout process
        if (!_isQueueProcessRoutineRunning)
        {
            ProcessQueueRoutine = StartCoroutine(ProcessQueue());
        }
    }

    public void ForceFinishProcessing()
    {
        _forceFinishProcessing = true;
        IsOpen = false;
    }

    private IEnumerator ProcessQueue()
    {
        _isQueueProcessRoutineRunning = true;
        while (_itemQueue.Count > 0)
        {
            var itemData = _itemQueue.Dequeue();
            AnyItemSlotsAvailable = true;

            // Move item to the counter
            itemData.item.AttachTo(_counterTop, Vector3.zero, Quaternion.identity);
            _isReadyForNextItem = false;

            // Trigger item delivery animation
            _animator.SetTrigger(_animIDScanItemTrigger);

            // Move the rest of the items to the bottom
            ShiftQueueItemPositions();

            if (_forceFinishProcessing)
                yield return new WaitForSeconds(1f);
            else
                yield return new WaitForSeconds(_deliveryDelay);

            ProcessItem(itemData.owner.PlayerAsset, itemData.item);
            _allPlayerQueuedItems[itemData.owner.PlayerAsset].Remove(itemData.item.ItemAsset);

            // Wait a minimum time for the success/fail animations to play
            yield return new WaitForSeconds(0.3f);

            // Dont process next item until the cashier is idle again
            while (!_isReadyForNextItem)
                yield return null;
        }

        ProcessQueueRoutine = null;
        _isQueueProcessRoutineRunning = false;
    }

    private void ProcessItem(PlayerAsset player, ItemBehaviour item)
    {
        // Try delivering the item to the game manager
        if (GameManager.Instance.TryDeliverItem(player, item.ItemAsset))
        {
            // Success
            _currentItem = item;
            _animator.SetTrigger(_animIDSuccessTrigger);
            // FX and such will be called with animation event
            _itemQueueGUI.ItemProcessed(true);
        }
        else
        {
            // Failure
            // Detatch item from counter
            item.Detach();

            // Throw item towards the front of the counter and upwards
            var target = _counterTop.position + _counterTop.forward * 6f + new Vector3(0f, 3f, 0f);
            var launchDirection = target - item.transform.position;
            var launchForce = 4f;

            item.Rigidbody.AddForce(launchDirection.normalized * launchForce, ForceMode.VelocityChange);

            _animator.SetTrigger(_animIDFailTrigger);
            _audioSource.PlayOneShot(_failAudio);
            _itemQueueGUI.ItemProcessed(false);
        }
    }

    private void ShiftQueueItemPositions()
    {
        Transform previous = _queuedItemPositions.Last();

        foreach (var queuedItem in _queuedItemPositions)
        {
            queuedItem.DOMove(previous.position, 0.2f).SetEase(Ease.OutBack);
            previous = queuedItem;
        }
    }

    private void CheckIsPlayerLastItem(PlayerAsset player)
    {
        if (GameSettings.Current.matchGamemode == EGamemode.Pandemic)
            return;

        var playerRemainingItems = GameManager.Instance.GetRemainingItemsForPlayer(player);
        var playerQueuedItems = _allPlayerQueuedItems[player];
        int deals = playerQueuedItems.Where(item => item.ItemCategory == EItemCategory.DealItems).Count();

        // If all player remaining items are in a queue then end the match
        if (playerRemainingItems.Except(playerQueuedItems).Count() - deals <= 0)
        {
            GameManager.Instance.EndMatch(EGameFinishReason.ListComplete);
        }
    }
}
