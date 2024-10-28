using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Honey puddle that will slow down the player when inside the trigger area.
/// The puddle will dissapear after some time after being spawned
/// 
/// Required components:
///  - Collider TRIGGER: the area where the honey will take effect
/// </summary>
[RequireComponent(typeof(Collider))]
public class HoneyPuddle : MonoBehaviour
{
    public float duration = 15f;

    private List<PlayerMovement> _playersAffected = new List<PlayerMovement>();

    private void Start()
    {
        StartCoroutine(DisableAfterDelay());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerMovement>(out var playerMovement))
        {
            playerMovement.IsInHoney = true;
            _playersAffected.Add(playerMovement);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<PlayerMovement>(out var playerMovement))
        {
            playerMovement.IsInHoney = false;
            _playersAffected.Remove(playerMovement);
        }
    }

    private IEnumerator DisableAfterDelay()
    {
        yield return new WaitForSeconds(duration);
        gameObject.SetActive(false);

        _playersAffected.Map(x => x.IsInHoney = false);
        _playersAffected.Clear();
    }
}
