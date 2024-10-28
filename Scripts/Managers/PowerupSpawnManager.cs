using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Spawns powerups for the memory game. Powerups will not spawn near players
/// </summary>
public class PowerupSpawnManager : MonoBehaviour
{
    [Header("List of Prefabs")]
    [SerializeField] List<BasePowerup> _powerupSequence;

    [Header("Config")]
    [SerializeField] float _minDistanceToPlayer = 3;
    [SerializeField] float _delayFromMatchBeginning = 12f;
    [SerializeField] float _delayBetweenSpawns = 8f;
    [SerializeField] float _spawnTimeVariance = 1f;
    [SerializeField] AudioClip _powerupSpawnSound;

    IEnumerable<Transform> _spawnPoints;

    private void Start()
    {
        // Spawn point must be children of this object, ignore parent transform
        _spawnPoints = transform.GetComponentsInChildren<Transform>().Where(x => x != transform);

        // If no powerups need to be spawned, the manager disables itself
        if (GameManager.Instance.GameSettings.matchGamemode != EGamemode.Memory)
            gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GameManager.onMatchStarted += StartSpawningPowerups;
        GameManager.onMatchFinished += StopSpawningPowerups;
    }

    private void OnDisable()
    {
        GameManager.onMatchStarted -= StartSpawningPowerups;
        GameManager.onMatchFinished -= StopSpawningPowerups;
    }

    private void StartSpawningPowerups() => StartCoroutine(SpawnPowerups());
    private void StopSpawningPowerups() => StopAllCoroutines();
    private IEnumerator SpawnPowerups()
    {
        // Start spawning after a delay
        yield return new WaitForSeconds(_delayFromMatchBeginning);

        int powerupIndex = 0;
        while(true)
        {
            var point = FindSuitableSpawnPoint();
            if (point)
            {
                Instantiate(_powerupSequence[powerupIndex], point);
                powerupIndex = (powerupIndex + 1) % _powerupSequence.Count;
                AudioManager.Instance.EffectSource.PlayOneShot(_powerupSpawnSound);
                yield return new WaitForSeconds(_delayBetweenSpawns + Random.Range(-_spawnTimeVariance, _spawnTimeVariance));
            }
            else
            {
                Debug.LogWarning($"{nameof(PowerupSpawnManager)} was not able to find a suitable powerup spawn point");
                yield return new WaitForSeconds(1f);
            }
        }
    }

    private Transform FindSuitableSpawnPoint()
    {
        // Dont spawn powerups on top of each other
        var validSpawns = _spawnPoints.Where(x => x.childCount == 0);
        if (validSpawns.Count() == 0) return null;

        int iterations = 0;
        int index;

        while(iterations < 10)
        {
            iterations++;
            index = Random.Range(0, validSpawns.Count());

            // Check that there are no players in the vicinity of the spawn point
            if (!Physics.CheckSphere(validSpawns.ElementAt(index).position, _minDistanceToPlayer, Layers.Player))
                return validSpawns.ElementAt(index);
        }

        return null;
    }

}
