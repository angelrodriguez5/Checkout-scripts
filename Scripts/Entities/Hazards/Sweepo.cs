using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Sweepo : MonoBehaviour
{
    [SerializeField] Transform target;

    NavMeshAgent navMeshAgent;

    bool overloaded;
    readonly float waitTimeAtDestination = 1.5f;
    float destinationIdleCounter;

    // Pathfinding variables
    readonly float[] turnAngles = new float[] { 90, 180, 270, 360 };
    readonly float minDistance = 4f;
    readonly float distanceFromObstacles = 1f;


    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.enabled = false;

        // Snap sweepo to navmesh
        if (NavMesh.SamplePosition(transform.position, out var navMeshHit, 1f, NavMesh.AllAreas))
        {
            transform.position = navMeshHit.position;
        }

        // Unparent target
        target.SetParent(null);
    }

    private void Start()
    {
        navMeshAgent.enabled = true;

        // Set initial destination
        navMeshAgent.SetDestination(target.position);
    }

    private void Update()
    {

        if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            destinationIdleCounter += Time.deltaTime;
            if (destinationIdleCounter >= waitTimeAtDestination)
            {
                GetNewDestination();
                destinationIdleCounter = 0;
            }
        }
    }

    private void GetNewDestination()
    {
        // Move in orthogonal directions
        var directions = new Vector3[]
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right
        }.Shuffle().ToArray();

        for (int i = 0; i < directions.Length; i++)
        {
            // Get a random direction
            var direction = directions[i];

            // Move in a straight line until it collides with the environment
            RaycastHit hitInfo;
            if (Physics.Raycast(transform.position + new Vector3(0f, .2f, 0f), direction, out hitInfo, 30f, Layers.Shelf | Layers.Default))
            {
                if (hitInfo.distance < minDistance)
                    continue;

                // Try to pathfind to some distance of the object hit
                Vector3 destination = hitInfo.point + (-direction * distanceFromObstacles);
                NavMeshHit navHit;
                if (NavMesh.SamplePosition(destination, out navHit, .3f, NavMesh.AllAreas))
                {
                    navMeshAgent.SetDestination(navHit.position);
                }
            }
        }
    }

    private IEnumerator Overload()
    {
        yield return null;
    }
}
