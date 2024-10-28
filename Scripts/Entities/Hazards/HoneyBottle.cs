using System;
using UnityEngine;


/// <summary>
/// A honey bottle that can be grabbed, dropped and thrown. After being thrown or dropped, if it collides
/// with any object on the _groundLayer it will spawn a _honeySplashPrefab where the bottle hit the ground
/// 
/// Required components:
///  - Collider: needed to be detected by the PlayerInteractionSystem. If the collider for the bottle is 
///              inside a child gameobject, then we need a trigger on the root object (where this script is)
/// </summary>
[RequireComponent(typeof(Collider), typeof(Outline))]
public class HoneyBottle : MonoBehaviour, IGrabbable, IInteractive
{
    [SerializeField] LayerMask _groundLayer;
    [SerializeField] GameObject _honeySplashPrefab;

    bool _primed = false;
    bool _expended = false;
    Rigidbody _rb;
    Outline _outline;
    int _playersLooking = 0;

    public event Action onGetGrabbed;

    bool _isGrabbed;
    public bool IsGrabbed
    {
        get => _isGrabbed;
        private set
        {
            _isGrabbed = value;
            onGetGrabbed?.Invoke();
        }
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _outline = GetComponent<Outline>();
    }

    private void OnEnable()
    {
        _outline.enabled = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!_expended && _primed && collision.gameObject.IsInLayer(_groundLayer))
        {
            // Cast a ray from the bottle down til it hits the ground
            if (Physics.Raycast(transform.position, Vector3.down, out var rayHit, 2f, _groundLayer))
            {
                // Spawn a honey splash just above the ground
                Instantiate(_honeySplashPrefab, rayHit.point + new Vector3(0f, 0.001f, 0f), Quaternion.identity);
                _expended = true;

                // Destroy this honey bottle
                Destroy(gameObject);
            }
            else
            {
                _primed = false;
            }
        }
    }

    #region IGrabbable implementation
    public GameObject GameObject => this.gameObject;

    public void AttachToPlayer(Transform parent, bool blockInteraction = true)
    {
        AttachTo(parent, Vector3.zero, Quaternion.identity);
    }

    public void AttachTo(Transform parent, Vector3 localPosition = default, Quaternion localRotation = default, bool blockInteraction = true)
    {
        transform.SetParent(parent);
        transform.localPosition = localPosition;
        transform.localRotation = localRotation == default ? Quaternion.identity : localRotation;

        IsGrabbed = true;
        _rb.isKinematic = true;
    }

    public void Detach()
    {
        transform.SetParent(null);

        IsGrabbed = false;
        _rb.isKinematic = false;
    }


    public void Throw(Vector3 velocity)
    {
        Detach();
        _primed = true;
        _rb.AddForce(velocity, ForceMode.VelocityChange);
    }
    #endregion

    #region IInteractive implementation
    public EInteractivePriorityType Priority => EInteractivePriorityType.Lowest;

    public bool CanInteract(GameObject interactor)
    {
        if (interactor.TryGetComponent<PlayerController>(out var playerController))
        {
            return !_primed && !IsGrabbed && playerController.ObjectHeld == null;
        }
        return !_primed && !IsGrabbed;
    }

    public void Interact(GameObject interactor)
    {
        if (interactor.TryGetComponent<PlayerController>(out var player) && !IsGrabbed)
        {
            player.PickUpObject(this);
        }
    }

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
    #endregion
}
