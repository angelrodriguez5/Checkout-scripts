using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns a honey bottle. After the bottle is picked up, it has a cooldown 
/// before spawning the next one.
/// </summary>
public class HoneySpawnTrap : MonoBehaviour
{
    [SerializeField] GameObject _honeyBottlePrefab;
    [SerializeField] Transform _spawnPoint;
    [SerializeField] float _cooldown = 5;

    HoneyBottle _currentHoney;
    bool _honeyAvailable = false;
    float _timer;

    private void Update()
    {
        if (!_honeyAvailable)
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                // Spawn new honey
                var honeyObj = Instantiate(_honeyBottlePrefab, _spawnPoint.position, _spawnPoint.rotation);
                _currentHoney = honeyObj.GetComponent<HoneyBottle>();
                _currentHoney.onGetGrabbed += StartCooldown;
                _honeyAvailable = true;
            }
        }
    }

    private void StartCooldown()
    {
        // Unsubscribe from the bottle that was grabbed
        _currentHoney.onGetGrabbed -= StartCooldown;

        _timer = _cooldown;
        _honeyAvailable = false;
    }
}
