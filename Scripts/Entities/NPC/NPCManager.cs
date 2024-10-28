using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;


/// <summary>
/// Manages NPC spawning and routing.
/// Assings random target points to NPCs, if the target point is near a shelf then
/// it will be oriented so the NPC looks at the shelf when the destination is reached
/// </summary>
public class NPCManager : MonoBehaviour
{
    [SerializeField] private GameObject _npcPrefab;
    [SerializeField] private LayerMask _shelfLayer;
    [SerializeField] private LayerMask _npcLayer;
    [SerializeField] private LayerMask _playerLayer;
    [TagField]
    [SerializeField] private string _npcWalkPlaneTag;

    // These plane colliders define the areas that will be targeted for NPC walking routines
    // Where they overlap there will be a higher probability of a NPC pathfinding to that location
    // All objects with a collider and  tag=_npcWalkPlaneTag will be taken into account
    private List<Collider> _targetPlaneColliders = new List<Collider>();
    private List<float> _colliderProbabilities;
    private List<NPC> _npcs = new List<NPC>();

    public LayerMask NpcLayer => _npcLayer;

    // Singleton
    public static NPCManager Instance { get; protected set; }

    private void Awake()
    {
        // Singleton
        if (Instance != null)
            throw new System.Exception($"Several {typeof(NPCManager)} in scene");
        else
            Instance = this;
    }

    private void Start()
    {
        if (!_npcPrefab.IsInLayer(_npcLayer))
        {
            Debug.LogError($"Npc prefab {_npcPrefab.name} is not set to npc layer {(int)_npcLayer}, assigning correct layer");
        }
    }

    /// <summary>
    /// Initialise the navMesh, this should be called after all stage static elements are in place
    /// </summary>
    public void Initialise()
    {
        // Gather planes
        _targetPlaneColliders.Clear();
        var objects = GameObject.FindGameObjectsWithTag(_npcWalkPlaneTag).ToList();
        foreach (var obj in objects)
        {
            if (obj.TryGetComponent<Collider>(out var collider))
                _targetPlaneColliders.Add(collider);
        }

        CalculateProbabilities();
    }

    /// <summary>
    /// Spawns a number of npcs at random positions who roam the supermarket randomly from point to point
    /// </summary>
    public void SpawnNPCs(int amount)
    {
        // Spawn extra npcs on pandemic mode
        if (GameSettings.Current.matchGamemode == EGamemode.Pandemic && GameSettings.Current.npcAmount != ENpcAmount.None)
            amount += GameManager.Instance.NumberOfPlayers;

        for (int i = 0; i < amount; i++)
        {
            // Instantiate NPC and warp it to a random position
            var target = new GameObject($"NPC{i:00}_Target").transform;
            MoveToRandomDestination(target);
            // Don't allow the npc to spawn on top of players
            while(Physics.CheckSphere(target.position, 0.5f, _playerLayer))
                MoveToRandomDestination(target);

            var npcInstance = Instantiate(_npcPrefab, target.position, target.rotation).GetComponent<NPC>();
            npcInstance.name = $"NPC{i:00}";
            npcInstance.Initialise(this, target, target.position);
            _npcs.Add(npcInstance);

            // In pandemic mode they move a bit faster
            if (GameSettings.Current.matchGamemode == EGamemode.Pandemic)
                npcInstance.AddSpeedModifier(2f);

            // Set random target to another position
                MoveToRandomDestination(target);
        }
    }

    /// <summary>
    /// Stops the movement of all NPCs
    /// </summary>
    public void StopNPCs()
    {
        foreach (var npc in _npcs)
        {
            npc.Stop();
        }
    }

    /// <summary>
    /// Resumes movement for all NPCs
    /// </summary>
    public void ResumeNPCs()
    {
        foreach (var npc in _npcs)
        {
            MoveToRandomDestination(npc.Target);
            npc.Resume();
        }
    }

    /// <summary>
    /// Assign new target destination to NPC,
    /// the new destination is stored in npc.Target
    /// </summary>
    /// <param name="npc"></param>
    public void RequestNewDestination(NPC npc) => MoveToRandomDestination(npc.Target);

    public void RequestNewDestination(Transform target) => MoveToRandomDestination(target);

    private void MoveToRandomDestination(Transform target)
    {
        // Calculate a random point in one of the planes that comprises the floor
        var collider = PickCollider();

        int iterations = 0;
        bool pointFound = false;
        Vector3 navMeshPoint = Vector3.zero;
        while (!pointFound)
        {
            if ((iterations + 1) % 10 == 0)
                Debug.LogWarning($"NPCManager.MoveToRandomDestination too many iterations: {iterations}");

            // Get random point within bounds
            var bounds = collider.bounds;
            var randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z));

            // Check that there is a point in the NavMesh within a 1m radius of the random chosen point
            if (NavMesh.SamplePosition(randomPoint, out var navMeshHit, 1f, 1))
            {
                navMeshPoint = navMeshHit.position;
                pointFound = true;
            }
            
            iterations++;
        }

        // Move target to position
        target.position = navMeshPoint;

        // Rotate target to look at the nearest shelf
        if (Physics.SphereCast(target.position, 1.5f, Vector3.up, out var hit, 2f, _shelfLayer))
        {
            // Debug.Log($"looking at shelf {hit.transform.name}");
            target.LookAt(hit.point);

            // Dont rotate x and z axis
            var newRotation = Quaternion.Euler(0f, target.rotation.eulerAngles.y, 0f);
            target.rotation = newRotation;
        }
        else
        {
            // No shelf within range, assign random rotation
            var newRotation = Random.rotation;
            newRotation = Quaternion.Euler(0f, newRotation.eulerAngles.y, 0f);
            target.rotation = newRotation;
        }
    }

    /// <summary>
    /// Returns a random collider from the available npcWalkinPlanes. Colliders that expand a greater percentage
    /// of the total walking area are more likely to be picked
    /// </summary>
    /// <returns></returns>
    private Collider PickCollider()
    {
        // Pîck collider based on its probability
        float acc = 0f;
        float rng = Random.value;
        for (int i = 0; i < _colliderProbabilities.Count; i++)
        {
            acc += _colliderProbabilities[i];
            if (rng <= acc)
            {
                return _targetPlaneColliders[i];
            }
        }
        return _targetPlaneColliders[0];
    }

    /// <summary>
    /// Computes the probability of picking each collider based on the amount of the total walkable area it encompasses
    /// </summary>
    private void CalculateProbabilities()
    {
        var eof = new WaitForEndOfFrame();

        // For simplicity we will only take into acount the XZ area, assuming that all planes are mostly horizontal
        _colliderProbabilities = new List<float>();
        float totalArea = 0f;
        foreach (var plane in _targetPlaneColliders)
        {
            float area = plane.bounds.size.x * plane.bounds.size.z;
            totalArea += area;

            // at first save plane areas
            _colliderProbabilities.Add(area);
        }

        // Then transform it to list of ratios of total area
        for (int i = 0; i < _colliderProbabilities.Count; i++)
        {
            _colliderProbabilities[i] = _colliderProbabilities[i] / totalArea;
        }
    }

}
