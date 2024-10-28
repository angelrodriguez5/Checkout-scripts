using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]  // Trigger
public abstract class BasePowerup : MonoBehaviour
{
    protected Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerController player))
            TriggerPowerup(player);
    }

    protected abstract void TriggerPowerup(PlayerController playerController);
}
