using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private int _itemAmount = 10;
    [SerializeField] private float _forceAmount = 5f;

    private ItemDB _itemDB;

    private void Awake()
    {
        _itemDB = ItemDB.Instance;
    }

    private void Start()
    {
        StartCoroutine(SpawnItems());
    }

    private IEnumerator SpawnItems()
    {
        for (int i = 0; i < _itemAmount; i++)
        {
            SpawnItem();
            yield return new WaitForSeconds(0.3f);
        }
    }

    private void SpawnItem()
    {
        // Instantiate item in a random 1m circle around this spawner
        Vector3 pos = Random.insideUnitSphere;
        Quaternion rot = Random.rotation;

        var instance = _itemDB.SpawnRandomItem();
        instance.transform.position = transform.position + pos;
        instance.transform.rotation = transform.rotation;

        // Add random force where the vertical component is always positive
        Vector3 force = Random.insideUnitSphere * _forceAmount;
        force.y = Mathf.Abs(force.y);
        if (instance.TryGetComponent<Rigidbody>(out var rb))
            rb.AddForce(force, ForceMode.Impulse);
    }
}
