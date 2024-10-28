using System.Collections;
using System.Collections.Generic;
using Tutorial;
using UnityEngine;

namespace Tutorial
{
    public class TutorialDeliveryArea : MonoBehaviour, IInteractive
    {
        [Header("Config")]
        [SerializeField] private TutorialPlayerZone _tutorial;
        [SerializeField] private Transform _counterTop;
        [SerializeField] private float _deliveryDelay = 1f;
        [SerializeField] private ParticleSystem _deliveredParticles;
        [SerializeField] private ItemQueueGUI _itemQueueGUI;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _successAudio;
        [SerializeField] private AudioClip _failAudio;
        [SerializeField] private AudioClip _scanItemAudio;
        [SerializeField] private AudioClip _closeAudio;
        [SerializeField] private AudioClip _openAudio;

        private PlayerAsset _currentPlayer;
        private TutorialItemBehaviour _currentItem;

        private Animator _animator;

        private static readonly int _animIDScanItemTrigger = Animator.StringToHash("ScanItemTrigger");
        private static readonly int _animIDSuccessTrigger = Animator.StringToHash("SuccessTrigger");
        private static readonly int _animIDFailTrigger = Animator.StringToHash("FailTrigger");
        private static readonly int _animIDIsOpen = Animator.StringToHash("IsOpen");

        bool _isOpen = true;
        public bool IsOpen
        {
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
        public bool IsBusy { get; private set; } = false;
        // Used to force this delivery area to instantly process the current item
        public bool ForceFinishProcessing { get; set; } = false;

        #region IInteractive Implementation
        public EInteractivePriorityType Priority => EInteractivePriorityType.Lowest;

        public bool CanInteract(GameObject interactor)
        {
            if (interactor.TryGetComponent<PlayerController>(out var player))
            {
                return IsOpen && !IsBusy && player.ObjectHeld != null;
            }
            return false;
        }

        public void Select() { }
        public void Deselect() { }
        public void Interact(GameObject interactor)
        {
            if (interactor.TryGetComponent<PlayerController>(out var player) && player.ObjectHeld is TutorialItemBehaviour)
            {
                // Get item from player and deliver it
                var item = player.ObjectHeld;
                player.ObjectHeld = null;
                DeliverItem(item as TutorialItemBehaviour);
            }
        }
        #endregion

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            IsOpen = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<TutorialItemBehaviour>(out var item))
            {
                // Check that item is not on the players hands when it enters the trigger
                if (IsOpen && !IsBusy && item.BelongsTo != null && (item.BelongsTo.ObjectHeld as TutorialItemBehaviour) != item)
                {
                    DeliverItem(item);
                }
            }
        }

        // Called from animation event
        public void AnimReadyToScan()
        {
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
            IsBusy = false;
        }

        public void AnimPlayOpenSound()
        {
            _audioSource.PlayOneShot(_openAudio);
        }

        public void AnimPlayClosedSound()
        {
            _audioSource.PlayOneShot(_closeAudio);
        }


        public void DeliverItem(TutorialItemBehaviour item) => StartCoroutine(_DeliverItem(item));
        private IEnumerator _DeliverItem(TutorialItemBehaviour item)
        {
            IsBusy = true;

            // Save current player and item
            _currentPlayer = item.BelongsTo.PlayerAsset;
            _currentItem = item;
            _itemQueueGUI.AddItem(item, item.BelongsTo);

            // Place item in counter
            _currentItem.AttachTo(_counterTop, Vector3.zero, Quaternion.identity);

            // Trigger item delivery animation
            _animator.SetTrigger(_animIDScanItemTrigger);

            // TODO: we will need to refactor this for other gamemodes
            // Wait some time except if this item is the last one on the player's list
            if (GameSettings.Current.matchGamemode != EGamemode.Pandemic)
            {
                var count = _deliveryDelay;
                while (count > 0f && !ForceFinishProcessing)
                {
                    count -= Time.deltaTime;
                    yield return null;
                }
            }

            ProcessItem(_currentPlayer, _currentItem);
        }

        private void ProcessItem(PlayerAsset player, TutorialItemBehaviour item)
        {
            // Try delivering the item to the game manager
            if (_tutorial.DeliverItem(item.ItemAsset))
            {
                // Success
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
                IsBusy = false;
                _itemQueueGUI.ItemProcessed(false);
            }
        }
    }
}