using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A puddle that, when touched by a player, will make him slip and then the puddle will dissapear
/// 
/// Requierd components:
///  - Collider TRIGGER: the area where the puddle will take effect
/// </summary>
[RequireComponent(typeof(Collider))]
public class WaterPuddle : MonoBehaviour
{
    static readonly float DISSAPEAR_TIME = 0.3f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerController>(out var player))
        {
            StartCoroutine(KnockPlayerAndDissapear(player));
        }
    }

    private IEnumerator KnockPlayerAndDissapear(PlayerController player)
    {
        player.Knockdown(player.transform.forward, true);
        yield return new WaitForSeconds(DISSAPEAR_TIME);
        gameObject.SetActive(false);
    }
}
