using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ThrownObjectCatcher : MonoBehaviour
{
    [SerializeField] DeliveryArea _deliveryArea;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<ItemBehaviour>(out var item))
        {
            // Check that item is not on the players hands when it enters the trigger
            if (_deliveryArea.IsOpen && _deliveryArea.AnyItemSlotsAvailable && item.BelongsTo != null && (item.BelongsTo.ObjectHeld as ItemBehaviour) != item)
            {
                _deliveryArea.QueueItem(item, item.BelongsTo);
            }
        }
    }
}
