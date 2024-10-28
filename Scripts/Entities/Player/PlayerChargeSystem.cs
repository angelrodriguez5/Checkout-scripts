using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// While this component is active the player will push any IPushable in the target area
/// </summary>
public class PlayerChargeSystem : MonoBehaviour
{
    [SerializeField] private Vector3 _chargeBoxOffset;
    [SerializeField] private Vector3 _chargeBoxSize;

    List<GameObject> _objectsPushed = new List<GameObject>();

    private void OnDisable()
    {
        _objectsPushed.Clear();
    }

    private void Update()
    {
        // Overlap box and check for IPushable inside it
        var collisions = Physics.OverlapBox(
            transform.TransformPoint(_chargeBoxOffset),
            _chargeBoxSize / 2,
            transform.rotation
            );

        foreach (var collision in collisions)
        {
            if (collision.gameObject.TryGetComponent<IPushable>(out var pushable))
            {
                // Only push an object once per charge
                if (!_objectsPushed.Contains(collision.gameObject))
                {
                    _objectsPushed.Add(collision.gameObject);
                    pushable.GetPushed(gameObject);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Select color
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);
        Gizmos.color = transparentRed;

        // Set origin and rotation of the gizmo to be the same as gameobject's transform
        Gizmos.matrix = transform.localToWorldMatrix;

        // Draw cube
        Gizmos.DrawCube(_chargeBoxOffset, _chargeBoxSize);
    }
}
