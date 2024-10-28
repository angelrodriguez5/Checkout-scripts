using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceArea : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Players
        if (other.TryGetComponent(out PlayerController player))
        {
            player.IsInSpace += 1;
        }

        // Items
        if (other.TryGetComponent(out ItemBehaviour item))
        {
            item.IsInSpace += 1;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Players
        if (other.TryGetComponent(out PlayerController player))
        {
            player.IsInSpace -= 1;
        }

        // Items
        if (other.TryGetComponent(out ItemBehaviour item))
        {
            item.IsInSpace -= 1;
        }
    }
}
