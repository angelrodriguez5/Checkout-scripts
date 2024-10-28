using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
	[Header("Player")]
	[Tooltip("Move speed of the character in m/s")]
	public float MoveSpeed = 3.5f;
	[Tooltip("Speed when the character is carrying an object")]
	public float RestrictedSpeed = 2.75f;
	[Tooltip("Speed when the character is inside a honey puddle")]
	public float HoneySpeed = 1f;
	[Tooltip("How fast the character turns to face movement direction")]
	[Range(0.0f, 0.3f)]
	public float RotationSmoothTime = 0.12f;
	[Tooltip("Acceleration and deceleration")]
	public float SpeedChangeRate = 10.0f;

	[Space(10)]
	[Tooltip("The height the player can jump")]
	public float JumpHeight = 1.2f;
	[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
	public float Gravity = -15.0f;

	[Space(10)]
	[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
	public float FallTimeout = 0.15f;

	[Header("Player Grounded")]
	[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
	[SerializeField] private bool _grounded = true;
	[Tooltip("Useful for rough ground")]
	public float GroundedOffset = -0.14f;
	[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
	public float GroundedRadius = 0.28f;
	[Tooltip("What layers the character uses as ground")]
	public LayerMask GroundLayers;

	[Header("Knockdown (root motion)")]
	[Tooltip("Whether the knockdown motion is achieved via PlayerMovement component or via animator rootmotion")]
	[SerializeField] private bool _knockdownWithRootMotion = false;
	[Tooltip("Values greater than 1 will exagerate the motion of the knockdown animation")]
	[Range(1f, 3f)] [SerializeField] private float _knockdownAnimationOverdrive = 1.2f;
	[Tooltip("Only necessary if root motion is active")]
	[SerializeField] private float _knockdownAnimationDuration = 0.3f;

	[Header("Knockdown (no root motion)")]
	[SerializeField] private float _knockdownSpeed = 3.5f;
	[SerializeField] private float _knockdownHeight = 0.5f;
	[SerializeField] private float _knockdownAirTime = 0.7f;
	[SerializeField] private float _knockdownRevoceryTime = 0.7f;

	[Header("Pushback")]
	[SerializeField] private float _pushbackSpeed = 6f;
	[SerializeField] private float _pushbackSpeedRestricted = 3f;
	[SerializeField] private float _pushbackFriction = 0.1f;

	[Header("Dash")]
	[SerializeField] private float _dashSpeed = 10f;
	[SerializeField] private float _dashDuration = 0.2f;
	[SerializeField] private float _dashDurationRestricted = 0.1f;

	[Header("Space movement overrides")]
	[SerializeField] private float _spaceSpeedChangeRate;

	private float _speed;
	private float _targetRotation;
	private float _rotationVelocity;
	private float _verticalVelocity;
	private float _terminalVelocity = 53.0f;

	private CharacterController _controller;
	private Animator _animator;

	public bool Grounded => _grounded;
	public float Speed => _speed;
	public Vector3 Velocity => _controller.velocity;
	public bool CanMove { get; set; } = true;
	public bool CanTurn { get; set; } = true;
	public bool IsSprinting { get; set; }
	public bool IsRestricted { get; set; }
	public bool IsInHoney { get; set; }
	public bool IsKnockedDown { get; set; }
	public bool IsPushBack { get; set; }
	public bool IsDashing { get; private set; }
	public Vector3 AttractionForce { get; set; }
	public bool IsInSpace { get; set; }


	private void Awake()
	{
		_controller = GetComponent<CharacterController>();
		_animator = GetComponent<Animator>();
	}

	private void Update()
	{
		GroundedCheck();
	}

	private void OnAnimatorMove()
	{
		if (_animator.applyRootMotion)
		{
			_controller.Move(_animator.deltaPosition * _knockdownAnimationOverdrive);
		}
	}

	private void GroundedCheck()
	{
		// set sphere position, with offset
		Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y + GroundedOffset, transform.position.z);
		_grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
	}

	public void Move(Vector2 movementInput)
	{
		// Target speed changes depending if restricted or dashing (dash prevails)
		float targetSpeed = IsRestricted ? RestrictedSpeed : MoveSpeed;
		targetSpeed = IsDashing ? _dashSpeed : targetSpeed;
		// Honey does not allow dash
		targetSpeed = IsInHoney ? HoneySpeed : targetSpeed;

		if (!CanMove || movementInput == Vector2.zero) targetSpeed = 0.0f;

		// a reference to the players current horizontal velocity
		float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

		float speedOffset = 0.1f;

		// accelerate or decelerate to target speed
		if (currentHorizontalSpeed + speedOffset < targetSpeed || currentHorizontalSpeed - speedOffset > targetSpeed)
		{
			float accel = IsInSpace ? _spaceSpeedChangeRate : SpeedChangeRate;

			// creates curved result rather than a linear one giving a more organic speed change
			// note T in Lerp is clamped, so we don't need to clamp our speed
			_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed, Time.deltaTime * accel);

			// round speed to 3 decimal places
			_speed = Mathf.Round(_speed * 1000f) / 1000f;
		}
		else
		{
			_speed = targetSpeed;
		}

		// normalise input direction
		Vector3 inputDirection = new Vector3(movementInput.x, 0.0f, movementInput.y).normalized;

		// if there is a move input rotate player when the player is moving
		if (CanTurn && movementInput != Vector2.zero)
		{
			//_targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + GameManager.Instance.MainCamera.transform.eulerAngles.y;
			_targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
			float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

			// rotate to face input direction relative to camera position
			transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
		}

		Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

		// move the player except when knocked down or pushed back
		if (!(IsKnockedDown || IsPushBack))
			_controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime + AttractionForce * Time.deltaTime);

		// Prepare vertical velocity for the next iteration
		if (!Grounded)
        {
			if (_verticalVelocity < _terminalVelocity)
				_verticalVelocity += Gravity * Time.deltaTime;
		}
		else
        {
			// Keep the character stuck to the ground even after groundcheck sphere collides
			// ground check should be a bit bellow the player but we dont want to hover above the ground
			_verticalVelocity = -2f;
        }
	}

	public bool Jump()
	{
		// Set _verticalVelocity, actual jump will be executed in the next Move call
		if (Grounded)
		{
			// the square root of H * -2 * G = how much velocity needed to reach desired height
			_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Push the player so it falls backwards on a certain direction
	/// </summary>
	/// <param name="direction">The direction of the movement. The players rotation will be adjusted so forward will be opposite to direction</param>
	public void Knockdown(Vector3 direction)
    {
		// Look at the opposit direction so we fall backwards
		direction.y = 0f;
		transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);

		// Push player backwards
		IsKnockedDown = true;
		_animator.applyRootMotion = _knockdownWithRootMotion;
		StartCoroutine(KnockbackRoutine(-transform.forward));
    }

	public void Pushback(Vector3 direction)
    {
		StartCoroutine(PushbackRoutine(direction));
		StopCoroutine(DashRoutine());
    }

	public void Dash()
    {
		StartCoroutine(DashRoutine());
    }

	private IEnumerator DashRoutine()
	{
		IsDashing = true;
		var wait = IsRestricted ? _dashDurationRestricted : _dashDuration;
		yield return new WaitForSeconds(wait);
		IsDashing = false;
	}

	private IEnumerator KnockbackRoutine(Vector3 pushDirection)
    {
		// Disable player inputs
		CanMove = false;
		CanTurn = false;
		IsKnockedDown = true;

		if (_knockdownWithRootMotion)
        {
			_animator.applyRootMotion = true;
			yield return new WaitForSeconds(_knockdownAnimationDuration);
			_animator.applyRootMotion = false;
        }
		else
        {
			// Ignore y component in push direction, the player will move as if the target and him were on the same plane
			pushDirection.y = 0;
			// Store vertical force to apply gravity
			var verticalComponent = new Vector3(0f, Mathf.Sqrt(_knockdownHeight * -2f * Gravity), 0f);

			var eof = new WaitForEndOfFrame();
			var fixedUpdate = new WaitForFixedUpdate();
			var timer = 0f;
			// Move player backwards as long as he is in the air
			while (timer < _knockdownAirTime)
			{
				yield return fixedUpdate;
				_controller.Move(pushDirection.normalized * (_knockdownSpeed * Time.deltaTime) + verticalComponent * Time.deltaTime);

				// Apply gravity to vertical component
				verticalComponent += new Vector3(0f, Gravity * Time.deltaTime, 0f);

				timer += Time.deltaTime;
				yield return eof;
			}

			// Wait for player recovery
			yield return new WaitForSeconds(_knockdownRevoceryTime);
        }

		// Return control to player
		CanMove = true;
		CanTurn = true;
		IsKnockedDown = false;
    }

	private IEnumerator PushbackRoutine(Vector3 pushDirection)
    {
		// Ignore y component in push direction, the player will be pushed back on his XZ plane
		pushDirection.y = 0;

		// Disable player inputs
		CanMove = false;
		IsPushBack = true;

		// Push the player and apply linear friction so it comes to a stop in a certain amount of time
		var pushSpeed = IsRestricted ? _pushbackSpeedRestricted : _pushbackSpeed;

		var eof = new WaitForEndOfFrame();
		var fixedUpdate = new WaitForFixedUpdate();
        while (pushSpeed > 1f)
        {
            yield return fixedUpdate;

			pushSpeed = Mathf.Lerp(pushSpeed, 0f, _pushbackFriction);
            _controller.Move(pushDirection.normalized * (pushSpeed * Time.deltaTime));
        }

		// Return control to player
		CanMove = true;
		IsPushBack = false;
	}

	private void OnDrawGizmosSelected()
	{
		Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
		Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

		if (Grounded) Gizmos.color = transparentGreen;
		else Gizmos.color = transparentRed;
			
		// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
		Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y + GroundedOffset, transform.position.z), GroundedRadius);
	}
}