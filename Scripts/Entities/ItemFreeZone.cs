using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ItemFreeZone : MonoBehaviour
{
    [SerializeField] LayerMask _itemLayer;
    [SerializeField] float _delay;
    [SerializeField] float _spawnRadius;
    [SerializeField] Transform _respawnPoint;

    BoxCollider _collider;
    Dictionary<GameObject, float> _items = new();

    private void OnTriggerStay(Collider other)
    {
        var obj = other.gameObject;
        if (obj.IsInLayer(_itemLayer))
        {
            if (_items.ContainsKey(obj))
                _items[obj] += Time.deltaTime;
            else
                _items.Add(obj, 0);

            if (_items[obj] >= _delay)
            {
                TransportItem(obj);
                _items.Remove(obj);
            }
        }
    }

    private void TransportItem(GameObject obj)
    {
        var randomDir = new Vector3(
            Random.Range(-_spawnRadius, _spawnRadius),
            Random.Range(-0.1f, 0.1f),
            Random.Range(-_spawnRadius, _spawnRadius));

        obj.transform.position = _respawnPoint.position + randomDir;
    }

    private void OnDrawGizmos()
    {
        if (_collider == null) _collider = GetComponent<BoxCollider>();

        var color = Color.cyan;
        color.a = 0.5f;
        Gizmos.color = color;

        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawCube(_collider.center, _collider.size);
    }
}