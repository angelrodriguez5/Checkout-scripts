using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// When an object containing this component is activated, the obstacle will not activate
/// if there are any NPC inside its bounds to prevent agent teleportation
/// </summary>
[RequireComponent(typeof(NavMeshObstacle))]
public class NPCSafeNavmeshObstacle : MonoBehaviour
{
    NavMeshObstacle _obstacle;

    private void Awake()
    {
        _obstacle = GetComponent<NavMeshObstacle>();
    }

    private void OnEnable()
    {
        _obstacle.enabled = false;
    }

    private void Update()
    {
        if (!_obstacle.enabled)
        {
            _obstacle.enabled = !Physics.CheckBox(
                transform.TransformPoint(_obstacle.center),
                _obstacle.size / 2,
                _obstacle.transform.rotation,
                NPCManager.Instance.NpcLayer
                );
        }
    }
}
