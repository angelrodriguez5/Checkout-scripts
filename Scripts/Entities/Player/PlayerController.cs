using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInteractionSystem))]
[RequireComponent(typeof(PlayerChargeSystem))]
[RequireComponent(typeof(CinemachineImpulseSource))]
public class PlayerController : MonoBehaviour, IPushable
{
	[Header("Player Personalisation")]
	[SerializeField] private PlayerAsset _playerAsset;
	[SerializeField] private SpriteRenderer _playerBorder;
	[SerializeField] private SpriteRenderer _playerArrow;
	[SerializeField] private SkinnedMeshRenderer _skinnedMesh;

	[Header("Abilities")]
	[Tooltip("The amount of time you are inmune after getting knocked down")]
	[SerializeField] private float _knockdownInmunityDuration = 0.2f;
	[SerializeField] private float _dashCooldown = 2f;
	[Tooltip("After dashing, if we get pushed before this amount of time passes it would be considered as if we were dashing")]
	[SerializeField] private float _pushbackForgiveness = 0.1f;

	[Header("Rig Sockets")]
	[SerializeField] private Transform _holdItemTwoHandSocket;
	[SerializeField] private Transform _handJointSocket;

	[Header("Physics")]
	[SerializeField] private PhysicMaterial _physicMaterial;
	[SerializeField] private Vector3 _itemDropOffset = new Vector3(0f, 0.4f, 0f);
	[SerializeField] private float _itemDropVelocity = 2.5f;
	[SerializeField] private float _itemThrowAngle = 25f;
	[SerializeField] private float _itemThrowVelocity = 7f;
	[Tooltip("How much of the speed of the player will be transfered to the item when pushing it around. " +
		"1 = item will be pushed with the same speed as the player; " +
		"2 = twice the speed of the player")]
	[Range(1f, 2f)]
	[SerializeField] private float _itemPushVelocityFactor = 1.1f;

	[Header("Audio")]
	[SerializeField] private AudioSource _audioSource;
	[SerializeField] private AudioClip _pickUpSound;
	[SerializeField] private AudioClip _knockdownSound;
	[SerializeField] private AudioClip _slipSound;
	[SerializeField] private AudioClip _dashSound;
	[SerializeField] private AudioClip _dashParrySound;
	[SerializeField] private AudioClip _changeSkinSound;
	[SerializeField] private AudioClip _jetpackSound;

	[Header("Particles")]
	[SerializeField] private ParticleSystem _dashParticles;
	[SerializeField] private ParticleSystem _knockdownParticles;
	[SerializeField] private ParticleSystem _dashParryParticles;

	// Input system values
	private Vector2 _movementInput;

	// animation IDs
	private static readonly int _animIDSpeed = Animator.StringToHash("Speed");
	private static readonly int _animIDCharge = Animator.StringToHash("Charging");
	private static readonly int _animIDGrounded = Animator.StringToHash("Grounded");
	private static readonly int _animIDKnockback = Animator.StringToHash("Knockback");
	private static readonly int _animIDHasItem = Animator.StringToHash("HasItem");
	private static readonly int _animIDCheer = Animator.StringToHash("Cheer");
	private static readonly int _animIDIsInSpace = Animator.StringToHash("IsInSpace");

	// Components
	private Animator _animator;
	private PlayerMovement _movement;
	private PlayerInput _playerInput;
	private PlayerInteractionSystem _interactionSystem;
	private PlayerChargeSystem _chargeSystem;
    private CharacterController _characterController;
    private PlayerSkinSelector _kaykitModel;
	private CinemachineImpulseSource _cameraShaker;
	private IGrabbable _objectHeld;

	private bool _teleport = false;
	private Vector3 _teleportTargetPosition;
	private Quaternion _teleportTargetRotation;

	private float _knockdownInmunityCDTimer;
	private float _dashCDTimer;
	private float _pushbackForgivenessTimer;

    private bool _interactionPressed;
    private float _interactionTapTimer;
	private bool _instantTapInteraction;

	private bool _pausedByThisPlayer;
	private int _isInSpace;

    // Properties
    public IGrabbable ObjectHeld 
	{ 
		get => _objectHeld;
		set
		{
			_objectHeld = value;
			_animator.SetBool(_animIDHasItem, _objectHeld != null);

			// Restrict movement if player is holding an item
			_movement.IsRestricted = value != null;
		}
	}
	public PlayerAsset PlayerAsset => _playerAsset;
	public PlayerInput PlayerInput => _playerInput;
	public bool AllowSkinChange { get; set; } = false;
	public int IsInSpace 
	{ 
		get => _isInSpace;
		set
		{
			_isInSpace = value;
			if (_isInSpace > 0)
            {
				// Player entered space
				_movement.IsInSpace = true;
				_animator.SetBool(_animIDIsInSpace, true);
				_audioSource.clip = _jetpackSound;
				_audioSource.loop = true;
				_audioSource.Play();
            }
			else
            {
				// Player is no longer in space
				_movement.IsInSpace = false;
				_animator.SetBool(_animIDIsInSpace, false);
				_audioSource.Stop();
			}
		}
    }

    #region PLAYER INPUT MESSAGES
    private void OnMove(InputValue value)
	{
		_movementInput = value.Get<Vector2>();
	}

	private void OnDash(InputValue value)
	{
		if (value.isPressed)
        {
			if (_dashCDTimer <= 0f && !_movement.IsKnockedDown)
            {
				_dashParticles.Play();
				_dashCDTimer = _dashCooldown;
				_movement.Dash();
				_audioSource.PlayOneShot(_dashSound, 0.25f); // Lower the volume to 25%
			}
        }
	}

	private void OnInteractOrThrow(InputValue value)
    {
		if (value.isPressed)
        {
			// Press tap
			_interactionTapTimer = InputSystem.settings.defaultTapTime;

			// If we dont have an item interact immediately since holding the button will do nothing
			if (ObjectHeld == null && _interactionSystem.Selected != null)
            {
				_interactionSystem.PerformInteraction();
				_instantTapInteraction = true;
            }
		}
		else
        {
			// Ignore the button release of an instant tap
			if (_instantTapInteraction)
            {
				_interactionTapTimer = 0f;
				_instantTapInteraction = false;
				return;
            }

			// Release tap
			if (_interactionTapTimer > 0f)
            {
				// Interact or drop item
				if (_interactionSystem.Selected != null)
					_interactionSystem.PerformInteraction();
				else if (ObjectHeld != null)
					DropItem();
            }

			// Release hold
			else
            {
				ThrowItem();
			}

			// Reset tap timer
			_interactionTapTimer = 0f;
        }
    }

	private void OnPlayerPause(InputValue value)
    {
		if (value.isPressed)
		{
			// While paused only allow the player that pressed pause to move the UI
			_pausedByThisPlayer = true;

			// Set input ui module actions to this player's
			var inputUIModule = (InputSystemUIInputModule)EventSystem.current.currentInputModule;
			GameManager.Instance.CurrentPlayerControllingUI = _playerInput;
			if (inputUIModule.actionsAsset != _playerInput.actions)
			{
				inputUIModule.actionsAsset = _playerInput.actions;
			}

			GameManager.Instance.TogglePause();
		}
    }

	private void OnUIPause(InputValue value)
	{
		if (GameManager.Instance && GameManager.Instance.IsGamePaused && _pausedByThisPlayer)
			GameManager.Instance.TogglePause();
	}

	// Allow all players to interact with post match UI
	// To do this swap current action map in the input system ui module to match the current player's action map
	private void OnNavigate(InputValue value) 
	{
		// During pause don't change input module
		if (GameManager.Instance && GameManager.Instance.IsGamePaused) return;

		// During game
		if (GameState.CurrentState == EGameState.Game)
        {

			var inputUIModule = (InputSystemUIInputModule)EventSystem.current.currentInputModule;
			if (inputUIModule.actionsAsset != _playerInput.actions)
			{
				inputUIModule.actionsAsset = _playerInput.actions;
				GameManager.Instance.CurrentPlayerControllingUI = _playerInput;

				// Repeat event so that it is registered by the UI
				var data = new AxisEventData(EventSystem.current);
				var vector = value.Get<Vector2>();
				if (vector.x > 0.5)
					data.moveDir = MoveDirection.Right;
				else if (vector.x < -0.5)
					data.moveDir = MoveDirection.Left;
				else if (vector.y > 0.5)
					data.moveDir = MoveDirection.Up;
				else if (vector.y < -0.5)
					data.moveDir = MoveDirection.Down;

				data.selectedObject = EventSystem.current.currentSelectedGameObject;
				ExecuteEvents.Execute(data.selectedObject, data, ExecuteEvents.moveHandler);
			}
		}

		// During character selection
		else if (GameState.CurrentState == EGameState.PlayerSelection && AllowSkinChange)
        {
			var vector = value.Get<Vector2>();
			if (vector.x > 0.5)
				ChangeSkin(1);
			else if (vector.x < -0.5)
				ChangeSkin(-1);
			//else if (vector.y > 0.5)
			//	ChangeColor(1);
			//else if (vector.y < -0.5)
			//	ChangeColor()-1;
		}
	}

	private void OnSubmit(InputValue value) 
	{
		if (GameState.CurrentState == EGameState.Game && _pausedByThisPlayer)
		{
			var inputUIModule = (InputSystemUIInputModule)EventSystem.current.currentInputModule;
			inputUIModule.actionsAsset = _playerInput.actions;
			GameManager.Instance.CurrentPlayerControllingUI = _playerInput;
			// For some reason there's no need to repeat this event, it's consumed by the UI automatically
		}
	}

	private void OnDeviceLost(PlayerInput player)
    {
		Debug.Log($"player {player.playerIndex} device lost: {player.devices}", this.gameObject);
		if (DeviceConectivityManager.Instance)
			DeviceConectivityManager.Instance.onDeviceLost.Invoke(player);
    }

	private void OnDeviceRegained(PlayerInput player)
    {
		Debug.Log($"player {player.playerIndex} device regained: {player.devices}", this.gameObject);
		if (DeviceConectivityManager.Instance)
			DeviceConectivityManager.Instance.onDeviceRegained.Invoke(player);
	}
	#endregion

	private void Awake()
	{
		_animator = GetComponent<Animator>();
		_movement = GetComponent<PlayerMovement>();
		_playerInput = GetComponent<PlayerInput>();
		_interactionSystem = GetComponent<PlayerInteractionSystem>();
		_chargeSystem = GetComponent<PlayerChargeSystem>();
		_characterController = GetComponent<CharacterController>();
		_kaykitModel = GetComponent<PlayerSkinSelector>();
		_cameraShaker = GetComponent<CinemachineImpulseSource>();

		_characterController.material = _physicMaterial;

		if (_playerAsset != null)
			LoadPlayerAsset(_playerAsset);

		// Disable movement until game begins
		_movement.CanMove = false;
	}

    private void Start()
    {
		// Unparent particles so we can play them at a certain world position
		_dashParryParticles.transform.parent = null;

		_playerArrow.enabled = false;

	}

	private void OnEnable()
    {
		GameManager.onMatchStarted += GameHasStarted;
		GameManager.onMatchFinished += GameHasFinished;
		GameManager.onGamePaused += GamePaused;
    }

    private void OnDisable()
    {
		GameManager.onMatchStarted -= GameHasStarted;
		GameManager.onMatchFinished -= GameHasFinished;
		GameManager.onGamePaused -= GamePaused;
	}

    private void Update()
    {
		// Update dash cooldown and forgiveness timer
		if (_dashCDTimer > 0f)
			_dashCDTimer -= Time.deltaTime;

		if (_movement.IsDashing)
			_pushbackForgivenessTimer = _pushbackForgiveness;
		else if (_pushbackForgivenessTimer > 0)
			_pushbackForgivenessTimer -= Time.deltaTime;

		// Update knockdown inmunity and particles
		if (_knockdownInmunityCDTimer > 0f)
        {
			_knockdownInmunityCDTimer -= Time.deltaTime;
			if (_knockdownInmunityCDTimer <= 0f)
				_kaykitModel.SetBlinkEffect(false);
        }

		// Update interaction tap timer
		if (_interactionTapTimer > 0f)
        {
			_interactionTapTimer -= Time.deltaTime;
			if (!_instantTapInteraction && _interactionTapTimer <= 0f && ObjectHeld != null)
				PrepareThrow();
        }

		// Enable chargeSystem while dashing
		_chargeSystem.enabled = _movement.IsDashing;

		// Dash particles
		if(_movement.IsDashing && !_dashParticles.isEmitting)
			_dashParticles.Play();
		if (_dashParticles.isEmitting && !_movement.IsDashing)
			_dashParticles.Stop();
    }

    private void FixedUpdate()
	{
		// Movement
		_movement.Move(_movementInput);

		// Queued teleportation
		if (_teleport)
        {
			transform.position = _teleportTargetPosition;
			transform.rotation = _teleportTargetRotation;
			_teleport = false;
        }
	}

    private void LateUpdate()
    {
		_animator.SetBool(_animIDGrounded, _movement.Grounded);
		_animator.SetFloat(_animIDSpeed, _movement.Speed);

		if (!_movement.CanMove)
			_animator.SetFloat(_animIDSpeed, 0f);
	}

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
		var rb = hit.collider.attachedRigidbody;

		if (!hit.gameObject.CompareTag("Item") || rb == null || rb.isKinematic)
			return;

		rb.velocity = hit.moveDirection * _movement.Velocity.magnitude * _itemPushVelocityFactor;
    }

	public void ChangeSkin(int offset)
    {
		var skin = PlayerSkin.GetAdjacentSkin(PlayerAsset.PlayerSkin, offset);
		_kaykitModel.ChangeSkin(skin);
		PlayerAsset.PlayerSkin = skin;

		AudioManager.Instance.UiSource.PlayOneShot(_changeSkinSound);
    }

    /// <summary>
    /// Assings a PlayerAsset to this controller.
    /// Also loads the skin, texture and configures the playerInput component
    /// </summary>
    /// <param name="playerAsset"></param>
    public void LoadPlayerAsset(PlayerAsset playerAsset)
    {
		if (playerAsset == null) return;

		_playerAsset = playerAsset;

		// Apply the same player input parameters that were saved in the menu scene
		if (playerAsset.Active)
			_playerInput.SwitchCurrentControlScheme(playerAsset.ControlScheme, playerAsset.Device);

		// Character customisation
		_playerBorder.color = playerAsset.PlayerColor;
		_playerArrow.color = playerAsset.PlayerColor;

		// Initialise player skin
		if (!_kaykitModel)
			_kaykitModel = GetComponent<PlayerSkinSelector>();

		_kaykitModel.ShowModel(playerAsset.PlayerSkin);
		_kaykitModel.ShowColor(playerAsset.PlayerColor);
    }

	public void Cheer()
    {
		_animator.SetBool(_animIDCheer, true);
	}

	public bool GetPushed(GameObject actor)
	{
		// Can't get pushed in these states
		if (_movement.IsKnockedDown || _movement.IsPushBack) return false;

		var fromThisToActor = actor.transform.position - this.transform.position;
		// Situations where pushback is caused
		if (_movement.IsDashing || _pushbackForgivenessTimer > 0f || _knockdownInmunityCDTimer > 0f)
        {
			_audioSource.PlayOneShot(_dashParrySound);
			var midpoint = transform.position + (actor.transform.position - transform.position) / 2;
			_dashParryParticles.transform.position = midpoint + new Vector3(0f,0.5f,0f);
			_dashParryParticles.Play();
			_cameraShaker.GenerateImpulseWithForce(0.5f);

			// When invulnerable cause pushback, but not on yourself
			if (_knockdownInmunityCDTimer <= 0f)
				this.Pushback(-fromThisToActor);
			
			if (actor.TryGetComponent<PlayerController>(out var actorController))
            {
				actorController.Pushback(fromThisToActor);
            }

			return false;
        }
		else
        {
			Knockdown(-fromThisToActor);
			return true;
        }

	}

	/// <summary>
	/// Push back the player without making it lose control or drop the item
	/// </summary>
	/// <param name="direction"></param>
	public void Pushback(Vector3 direction)
    {
		_movement.Pushback(direction);
    }

	/// <summary>
	/// Knock down the player and make it throw their item
	/// </summary>
	/// <param name="direction"></param>
	/// <param name="isSlip"></param>
	public void Knockdown(Vector3 direction, bool isSlip = false)
    {
		if (_knockdownInmunityCDTimer > 0f) return;

		_movement.Knockdown(direction);

		_knockdownInmunityCDTimer = _knockdownInmunityDuration;

		DropItem();

		_knockdownParticles.Play();
		_kaykitModel.SetBlinkEffect(true);
		_animator.SetTrigger(_animIDKnockback);
		_playerArrow.enabled = false;

		if (!isSlip)
			_audioSource.PlayOneShot(_knockdownSound, 0.5f);
		else
			_audioSource.PlayOneShot(_slipSound, 0.5f);

		_cameraShaker.GenerateImpulse();
	}

	public void PickUpObject(IGrabbable obj)
    {
		ObjectHeld = obj;
		_audioSource.PlayOneShot(_pickUpSound);

		// Set object as child of the skeleton
		obj.AttachToPlayer(_holdItemTwoHandSocket);
	}

    #region Teleport
    public void Teleport(Transform target)
    {
		Teleport(target.position, target.rotation);
    }

	public void Teleport(Vector3 position)
    {
		Teleport(position, transform.rotation);
    }

	public void Teleport(Vector3 position, Quaternion rotation)
    {
		_teleportTargetPosition = position;
		_teleportTargetRotation = rotation;
		_teleport = true;
    }
    #endregion

    public void DisableMovement(bool dropItem = false)
    {
		_movement.CanMove = false;

		if (dropItem && ObjectHeld != null)
			DropItem();
    }

	public void EnableMovement()
    {
		_movement.CanMove = true;
		_playerInput.SwitchCurrentActionMap("Player");
	}

	public void DropItem()
	{
		if (ObjectHeld != null)
		{
			// Eject item with a little velocity relavite to the player's velocity
			var ejectVelocity = (transform.forward + _itemDropOffset).normalized * _itemDropVelocity;
			ejectVelocity += _movement.Velocity;
			ObjectHeld.Throw(ejectVelocity);

			// Remove reference
			ObjectHeld = null;
			_playerArrow.enabled = false;
		}
	}

	private void PrepareThrow()
    {
		if (ObjectHeld != null)
        {
			// Disable movement
			_movement.CanMove = false;

			// Show arrow sprite
			_playerArrow.enabled = true;
        }
    }

	private void ThrowItem()
    {
		if (ObjectHeld != null)
		{
			// When throwing an item keep ownership for a certain time
			if (ObjectHeld is ItemBehaviour)
				(ObjectHeld as ItemBehaviour).keepOwnershipAfterDetatch = true;

			// Eject forward with the desired angle
			var angle = Mathf.Deg2Rad * _itemThrowAngle;
			var throwDirection = new Vector3(0f, Mathf.Sin(angle), Mathf.Cos(angle)).normalized;
			throwDirection = transform.localToWorldMatrix.MultiplyVector(throwDirection);
			ObjectHeld.Throw(throwDirection * _itemThrowVelocity);

			// Remove reference
			ObjectHeld = null;

			// Allow movement again
			_movement.CanMove = true;

			// Hide arrow sprite
			_playerArrow.enabled = false;
		}
	}

	private void GamePaused(bool isPaused)
    {
		if (isPaused)
			_playerInput.SwitchCurrentActionMap("UI");
		else
        {
			_playerInput.SwitchCurrentActionMap("Player");
			_pausedByThisPlayer = false;
        }
	}

	private void GameHasStarted()
    {
		_movement.CanMove = true;
		_playerInput.SwitchCurrentActionMap("Player");
	}

	private void GameHasFinished()
    {
		// Disable movement
		_movement.CanMove = false;

		// Drop item
		if (ObjectHeld != null)
			DropItem();

		// Enable navigation in postgame menu
		_playerInput.SwitchCurrentActionMap("UI");
	}
}