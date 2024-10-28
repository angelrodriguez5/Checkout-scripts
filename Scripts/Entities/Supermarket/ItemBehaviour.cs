using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// The representation of an ItemAsset as a gameObject
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ItemBehaviour : MonoBehaviour, IInteractive, IPushable, IGrabbable
{
    private static readonly float LOSE_OWNERSHIP_TIME = 3f;  // Amount of time that a thown object will be considered as owned by the player the threw it
    private static readonly float RETURN_TO_SHELF_TIME = 8f;  // Amount of time that an object needs to be idle to return to its shelf
    private static readonly float RETURN_TO_SHELF_TIME_CAP = 12f;  // After this time the object will return to the shelf even if there are players near
    private static readonly float REPOSITION_PUSHED_ITEM_TIME = 6f;  // Amount of time that an object needs to be idle to return to its shelf

    private static float _pushForce = 5f;
    private static float _pushVertical = 0.6f;
    private static float _pushCD = 0.2f;

    private static GameObject _dealParticlesPrefab;

    [SerializeField] private ItemAsset _itemAsset;

    [HideInInspector] public bool keepOwnershipAfterDetatch;

    protected float _pushTimer;

    protected bool _checkReturnToShelf = false;
    protected float _returnToShelfTimer;
    protected float _returnToShelfLastCheck;
    protected bool _checkRepositionPushedItem;
    protected float _repositionPushedItemCountdown;
    protected bool _isDealHighlighted;
    protected float _dealHighlightTimer;
    private GameObject _dealParticlesInstance;
    private int _isInSpace;

    public event Action onItemGrabbed; 

    protected List<Collider> _colliders = new List<Collider>();

    public ItemAsset ItemAsset { get => _itemAsset; set => _itemAsset = value; }
    public Rigidbody Rigidbody { get; private set; }
    public PlayerController BelongsTo { get; protected set; }
    public bool BlockInteraction { get; set; } = false;
    public int IsInSpace
    {
        get => _isInSpace;
        set
        {
            _isInSpace = value;
            if (_isInSpace > 0)
            {
                // Entered space
                Rigidbody.useGravity = false;
                Rigidbody.drag = 2;
                Rigidbody.angularDrag = 0.05f;
                Rigidbody.constraints = RigidbodyConstraints.FreezePositionY;
            }
            else
            {
                // No longer in space
                Rigidbody.useGravity = true;
                Rigidbody.drag = 0;
                Rigidbody.angularDrag = 0.05f;
                Rigidbody.constraints = RigidbodyConstraints.None;
            }
        }
    }
    public Shelf Shelf { get; set; }
    public bool HighlightItemPermanently { get; set; }

    #region IGrabbable implementation
    GameObject IGrabbable.GameObject { get => this.gameObject; }

    public void AttachToPlayer(Transform parent, bool blockInteraction = true)
    {
        AttachTo(parent, Vector3.zero, Quaternion.Euler(ItemAsset.InHandRotation), blockInteraction);
    }
    public void AttachTo(Transform parent, Vector3 localPosition, Quaternion localRotation, bool blockInteraction = true)
    {
        transform.parent = parent;
        transform.localPosition = localPosition;
        transform.localRotation = localRotation;
        Rigidbody.isKinematic = true;
        _checkReturnToShelf = false;
        Shelf = null;
        _checkRepositionPushedItem = false;
        BlockInteraction = blockInteraction;

        _colliders.Map(x => x.enabled = false);
    }

    /// <summary>
    /// Detatches the item from its parent and enables player interaction
    /// </summary>
    public virtual void Detach()
    {
        if (!keepOwnershipAfterDetatch)
            BelongsTo = null;
        else
            StartCoroutine(LoseOwnershipDelay(LOSE_OWNERSHIP_TIME));
        keepOwnershipAfterDetatch = false;

        IsInSpace = 0;
        transform.parent = null;
        Rigidbody.isKinematic = false;
        BlockInteraction = false;
        _checkReturnToShelf = true;
        _returnToShelfTimer = 0f;

        _colliders.Map(x => x.enabled = true);
    }

    public void Throw(Vector3 velocity)
    {
        Detach();
        Rigidbody.AddForce(velocity, ForceMode.VelocityChange);
    }
    #endregion

    #region IInteractive Implementation
    public EInteractivePriorityType Priority 
    {
        get
        {
            if (_itemAsset.ItemCategory == EItemCategory.DealItems)
                return EInteractivePriorityType.Highest;
            else
                return EInteractivePriorityType.Item;
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        if (interactor.TryGetComponent<PlayerController>(out var player))
        {
            return player.ObjectHeld == null && !BlockInteraction;
        }
        return false;
    }

    public void Deselect() { }

    public void Interact(GameObject interactor)
    {
        if (interactor.TryGetComponent<PlayerController>(out var player))
        {
            StopAllCoroutines();
            player.PickUpObject(this);
            BelongsTo = player;
            Shelf = null;
            onItemGrabbed?.Invoke();
            _checkReturnToShelf = false;
        }
    }

    public void Select() { }

    #endregion

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();

        if (_dealParticlesPrefab == null)
        {
            _dealParticlesPrefab = Resources.Load<GameObject>("Particles/DealHighlightParticles");
        }
    }

    private void Start()
    {
        _colliders.Add(GetComponent<Collider>());
        _colliders.AddRange(GetComponentsInChildren<Collider>());
    }

    private void Update()
    {
        // Push cooldown
        if (_pushTimer > 0)
            _pushTimer -= Time.deltaTime;

        // Return to shelf when object is idle outside its shelf
        if (_checkReturnToShelf && ItemAsset.ItemCategory != EItemCategory.DealItems)
        {
            _returnToShelfTimer += Time.deltaTime;

            // After return to shelf time has elapsed check once every second
            if (_returnToShelfTimer >= RETURN_TO_SHELF_TIME && Time.time - _returnToShelfLastCheck >= 1f)
            {
                _returnToShelfLastCheck = Time.time;
                // Return to shelf if no players are near or we reached the time cap
                if (!Physics.CheckSphere(transform.position, 4f, Layers.Player) || _returnToShelfTimer >= RETURN_TO_SHELF_TIME_CAP)
                {
                    Supermarket.Instance.ReturnItemToShelf(this);
                    _checkReturnToShelf = false;
                    _returnToShelfTimer = 0f;
                    _returnToShelfLastCheck = 0f;
                }
            }
        }

        // Reposition item on the shelf if it was pushed but not grabbed
        if (_checkRepositionPushedItem && ItemAsset.ItemCategory != EItemCategory.DealItems)
        {
            _repositionPushedItemCountdown -= Time.deltaTime;

            if (_repositionPushedItemCountdown <= 0)
            {
                Shelf.ReturnSingleItem(this);
                _checkRepositionPushedItem = false;
            }
        }

        // Make deal items levitate after being left on the ground for some time
        else if ((HighlightItemPermanently || ItemAsset.ItemCategory == EItemCategory.DealItems) && BelongsTo == null)
        {
            _dealHighlightTimer += Time.deltaTime;

            if (!_isDealHighlighted && _dealHighlightTimer >= 2f)
            {
                StartDealHighlight();
            }
        }
    }

    private void OnDestroy()
    {
        // Cleanup highlight particles
        if (_dealParticlesInstance != null)
            Destroy(_dealParticlesInstance);
    }

    public bool GetPushed(GameObject actor) 
    {
        if (_pushTimer <= 0)
        {
            _pushTimer = _pushCD;

            // If the item was on a shelf try to reposition it again after some time
            if (Shelf != null)
            {
                _checkRepositionPushedItem = true;
                _repositionPushedItemCountdown = REPOSITION_PUSHED_ITEM_TIME;
            }

            // Push object away from the one who pushed it
            var pushDirection = transform.position - actor.transform.position;
            // Independent of the Y coord add a slight upward force
            pushDirection.y = 0;
            pushDirection.Normalize();
            pushDirection.y = _pushVertical;
            // Apply force
            Rigidbody.AddForceAtPosition(pushDirection.normalized * _pushForce, Rigidbody.ClosestPointOnBounds(actor.transform.position), ForceMode.Impulse);

            return true;
        }
        return false;
    }

    protected IEnumerator LoseOwnershipDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        BelongsTo = null;
    }

    private void StartDealHighlight()
    {
        _isDealHighlighted = true;
        _dealParticlesInstance = Instantiate(_dealParticlesPrefab, transform.position, Quaternion.identity);
        var follow =_dealParticlesInstance.AddComponent<FollowTransform>();
        follow.follow = new Transform[] { transform };
        follow.index = 0;
    }

    private void StopDealHighlight()
    {
        if (HighlightItemPermanently)
            return;

        _isDealHighlighted = false;
        _dealHighlightTimer = 0;
        Destroy(_dealParticlesInstance);
        _dealParticlesInstance = null;
    }
}
