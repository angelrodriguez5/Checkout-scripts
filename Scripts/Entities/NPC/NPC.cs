using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
///  A NPC that walks to its target destination and can get pushed around.
///  The NPC will match the rotation of the target transform upon arrival.
///  When he reaches the destination it requests a new one to the NPCManager.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NavMeshObstacle))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class NPC : MonoBehaviour, IPushable
{
    [Header("NPC configuration")]
    [SerializeField] private Transform _target;
    [SerializeField] private NPCManager _manager;
    [SerializeField] private float _waitOnTargetTime = 2f;
    [SerializeField] private float _unstuckTime = 3f;

    [Header("Push")]
    [SerializeField] private float _getPushedCooldown = 0.1f;
    [SerializeField] private float _pushbackSpeed = 3f;
    [SerializeField] private float _pushbackTime = 0.2f;
    [SerializeField] private float _groundFriction = 2.5f;

    [Header("Audio")]
    [SerializeField] private float _minPitch = 0.8f;
    [SerializeField] private float _maxPitch = 1.2f;

    private AudioSource _audioSource;
    private Animator _animator;
    private NavMeshAgent _navMeshAgent;
    private NavMeshObstacle _navMeshObstacle;

    private bool _waitingForNewDestination;
    private float _rotationVelocity;
    private float _stuckTimer;
    private float _pushTimer;
    private Vector3 _lastPosition;

    private Coroutine WaitingNewDestinationRoutine;
    private Coroutine BeingPushedRoutine;

    // animation IDs
    private int _animIDSpeed = Animator.StringToHash("Speed");
    private int _animIDPushDirX = Animator.StringToHash("PushDirX");
    private int _animIDPushDirZ = Animator.StringToHash("PushDirZ");
    private int _animIDPushback = Animator.StringToHash("Pushback");

    public Transform Target { get => _target; set => _target = value; }
    public bool CanBePushed => _pushTimer <= 0f;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _navMeshObstacle = GetComponent<NavMeshObstacle>();
        _animator = GetComponent<Animator>();

        _navMeshObstacle.enabled = false;
        _navMeshAgent.enabled = true;
    }

    private void Start()
    {
        _navMeshAgent.SetDestination(Target.position);
    }

    private void Update()
    {
        _animator.SetFloat(_animIDSpeed, _navMeshAgent.velocity.magnitude);

        if (_pushTimer > 0f)
            _pushTimer -= Time.deltaTime;

        CheckStuck();

        // If we reached the current destination wait and get another one
        if (   !_waitingForNewDestination
            && _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance)
        {
            // Debug.Log("Finding new destination");
            _waitingForNewDestination = true;
            WaitingNewDestinationRoutine = StartCoroutine(GetNewDestinationWithDelay());
        }
    }

    private void CheckStuck()
    {
        // The amount of space that the agent would have traveled at 95% speed, dont use 100% speed to allow a little tolerance
        var desiredDeltaPosition = _navMeshAgent.speed * Time.deltaTime * 0.95f;
        // Check if the agent is stuck
        if (_navMeshAgent.enabled && !_waitingForNewDestination && Vector3.Distance(_lastPosition, transform.position) <= desiredDeltaPosition)
        {
            _stuckTimer -= Time.deltaTime;
            if (_stuckTimer <= 0)
            {
                // Unstuck
                //Debug.Log("NPC: unstuck", this.gameObject);
                GetNewDestination();
                _stuckTimer = _unstuckTime;
            }
        }
        else
        {
            _stuckTimer = _unstuckTime;
        }
        _lastPosition = transform.position;
    }

    public void Initialise(NPCManager manager, Transform target, Vector3 initialPosition)
    {
        _manager = manager;
        Target = target;
        _navMeshAgent.Warp(initialPosition);
    }

    public void Stop()
    {
        // Stop all coroutines
        StopAllCoroutines();
        BeingPushedRoutine = null;
        WaitingNewDestinationRoutine = null;

        // Stop navmesh Agent 
        _navMeshObstacle.enabled = false;
        _navMeshAgent.enabled = true;
        _navMeshAgent.isStopped = true;
    }

    public void Resume()
    {
        // Stop navmesh Agent 
        _navMeshObstacle.enabled = false;
        _navMeshAgent.enabled = true;
        _navMeshAgent.isStopped = false;
    }

    public void AddSpeedModifier(float modifier)
    {
        _navMeshAgent.speed += modifier;
    }

    private IEnumerator GetNewDestinationWithDelay()
    {
        var eof = new WaitForEndOfFrame();

        // While we are waiting activate obstacle so other npcs avoid us
        _navMeshAgent.enabled = false;
        yield return null;
        _navMeshObstacle.enabled = true;
        yield return null;

        // Rotate agent to match target's rotation
        var tolerance = 0.1f;
        while(Quaternion.Angle(Target.rotation, transform.rotation) > tolerance)
        {
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, Target.rotation.eulerAngles.y, ref _rotationVelocity, 0.15f);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            yield return null;
        }

        // Wait at current position
        yield return new WaitForSeconds(_waitOnTargetTime);

        _navMeshObstacle.enabled = false;
        yield return null;
        _navMeshAgent.enabled = true;
        yield return null;

        GetNewDestination();
        WaitingNewDestinationRoutine = null;
    }

    private void GetNewDestination()
    {
        // Get new destination from manager and set navmesh destination
        _manager.RequestNewDestination(this);
        _navMeshAgent.SetDestination(Target.position);
        _waitingForNewDestination = false;
    }

    public bool GetPushed(GameObject actor)
    {
        if (CanBePushed)
        {
            bool wasWaiting = WaitingNewDestinationRoutine != null;
            if (wasWaiting)
            {
                StopCoroutine(WaitingNewDestinationRoutine);
                WaitingNewDestinationRoutine = null;
            }

            if (BeingPushedRoutine != null)
                StopCoroutine(BeingPushedRoutine);

            // Play sound
            _audioSource.pitch = Random.Range(_minPitch, _maxPitch);
            _audioSource.Play();

            var pushDir = transform.position - actor.transform.position;
            BeingPushedRoutine =  StartCoroutine(PushbackRoutine(pushDir, wasWaiting));

            // Propagate push to adjacent NPCs in the direction of the push
            // Check sphere in the position where this npc will be pushed towards
            var npcRadius = 0.8f;
            var spherePos = transform.position + (pushDir.normalized * npcRadius);
            foreach (var collider in Physics.OverlapSphere(spherePos, npcRadius, Layers.NPC))
            {
                if (collider.TryGetComponent<NPC>(out var otherNpc))
                {
                    otherNpc.GetPushed(this.gameObject);
                }
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    private IEnumerator PushbackRoutine(Vector3 pushDirection, bool wasWaiting)
    {
        // Cooldown
        _pushTimer = _getPushedCooldown;

        // Ignore y component in push direction, the npc will be pushed back on his XZ plane
        pushDirection.y = 0;

        // Stop navmesh path
        if (_navMeshAgent.enabled)
            _navMeshAgent.isStopped = true;
        else
        {
            _navMeshObstacle.enabled = false;
            yield return null;
            _navMeshAgent.enabled = true;
        }

        // Animation
        var localDirection = (transform.worldToLocalMatrix * pushDirection);
        _animator.SetBool(_animIDPushback, true);
        _animator.SetFloat(_animIDPushDirX, localDirection.normalized.x);
        _animator.SetFloat(_animIDPushDirZ, localDirection.normalized.z);

        // Push the npc and apply friction so it comes to a stop in a certain amount of time
        var pushSpeed = _pushbackSpeed;
        var falloff = pushSpeed / _pushbackTime;

        var eof = new WaitForEndOfFrame();
        var fixedUpdate = new WaitForFixedUpdate();
        while (pushSpeed > 0.1f)
        {
            yield return fixedUpdate;
            _navMeshAgent.Move(pushDirection.normalized * (pushSpeed * Time.deltaTime));

            // Apply friction
            pushSpeed -= falloff * Time.deltaTime;
            falloff = (pushSpeed * _groundFriction) / _pushbackTime;

            yield return eof;
        }

        // Finish animation
        _animator.SetBool(_animIDPushback, false);

        // Don't get moving immediatly
        yield return new WaitForSeconds(0.3f);

        // Resume route
        _navMeshObstacle.enabled = false;
        yield return null;
        _navMeshAgent.enabled = true;
        _navMeshAgent.isStopped = false;
        _waitingForNewDestination = false;

        if (wasWaiting)
            GetNewDestination();

        BeingPushedRoutine = null;
    }
}
