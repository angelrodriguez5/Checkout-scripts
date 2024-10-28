using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class Shelf : MonoBehaviour
{
    [SerializeField] private List<Transform> _spawnPoints = new List<Transform>();
    [SerializeField] private GameObject _itemSpawnParticles;
    [SerializeField] private AudioClip _itemSpawnSound;
    [SerializeField] private bool _isSpaceShelf;

    private List<ItemBehaviour> _spawnedItems = new();
    private List<Action> _subscribedActions = new();
    public int Capacity => _spawnPoints.Count;
    public bool HasFreeSlots => _spawnedItems.Any(item => item == null);
    public bool HasItems => _spawnedItems.Any(item => item != null);

    private void Awake()
    {
        // Initialize list to the size of the shelf
        for (int i = 0; i < _spawnPoints.Count; i++)
        {
            _spawnedItems.Add(null);
            _subscribedActions.Add(null);
        }
    }

    /// <summary>
    /// Put items in the shelf
    /// </summary>
    public void LoadItems(List<GameObject> items)
    {
        if (items.Count > Capacity)
            throw new System.Exception($"Trying to load to many items in shelf: passed {items.Count}, capacity {Capacity}");

        for (int i = 0; i < items.Count; i++)
        {
            // Allow passing nulls to create gaps in the shelves
            if (items[i] == null)
                continue;

            // Set the item in its position
            items[i].transform.position = _spawnPoints[i].position;
            items[i].transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);  // Orient the item towards the camera

            if (items[i].TryGetComponent<ItemBehaviour>(out var itemBehaviour))
            {
                // Respond to item grabbed event by freeing up the item's slot in the shelf
                // we need to save a reference to the subscribed action to then unsubscribe
                // We need to make a copy of i, otherwise is passed by reference?!
                itemBehaviour.Shelf = this;
                int ix = i;
                _subscribedActions[i] = () => FreeSlotOnItemPickup(ix);
                itemBehaviour.onItemGrabbed += _subscribedActions[i];
                _spawnedItems[i] = itemBehaviour;

                ResetItemPhysics(itemBehaviour);
            }
        }
    }

    public void SpawnSingleItemWithDelay(ItemAsset itemAsset) 
    {
        if (!HasFreeSlots)
            throw new System.Exception($"{gameObject.name}: shelf trying to load item with no available slots");

        // Mark slot as occupied instantly even though the item will not appear until after the delay
        int i = _spawnedItems.IndexOf(null);

        StartCoroutine(_SpawnSingleItemWithDelay(itemAsset, i));
    }
    private IEnumerator _SpawnSingleItemWithDelay(ItemAsset itemAsset, int slotIdx)
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 3f));

        var item = itemAsset.SpawnNewGameObject();

        item.transform.position = _spawnPoints[slotIdx].position;
        item.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);  // Orient the item towards the camera
        Instantiate(_itemSpawnParticles, _spawnPoints[slotIdx].position, _spawnPoints[slotIdx].rotation);
        AudioManager.Instance.EffectSource.PlayOneShot(_itemSpawnSound);

        if (item.TryGetComponent(out ItemBehaviour itemBehaviour))
        {
            // Respond to item grabbed event by freeing up the item's slot in the shelf
            // we need to save a reference to the subscribed action to then unsubscribe
            itemBehaviour.Shelf = this;
            _spawnedItems[slotIdx] = itemBehaviour;
            _subscribedActions[slotIdx] = () => FreeSlotOnItemPickup(slotIdx);
            itemBehaviour.onItemGrabbed += _subscribedActions[slotIdx];

            ResetItemPhysics(itemBehaviour);
        }
    }

    public void ReturnSingleItem(ItemBehaviour item)
    {
        // Allow returning an item that is already on the shelf i.e. it was pushed instead of picked up
        // otherwise find a free slot for it
        int slotIdx;
        bool isReposition = false;
        if (_spawnedItems.Contains(item))
        {
            slotIdx = _spawnedItems.IndexOf(item);
            isReposition = true;
        }
        else if (!HasFreeSlots)
            throw new System.Exception($"{gameObject.name}: shelf trying to return item with no available slots");
        else
        {
            slotIdx = _spawnedItems.IndexOf(null);
            isReposition = false;
        }

        item.transform.position = _spawnPoints[slotIdx].position;
        item.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);  // Orient the item towards the camera
        Instantiate(_itemSpawnParticles, _spawnPoints[slotIdx].position, _spawnPoints[slotIdx].rotation);
        AudioManager.Instance.EffectSource.PlayOneShot(_itemSpawnSound);

        // When the item is being repositioned this is already set properly
        if (!isReposition)
        {
            // Respond to item grabbed event by freeing up the item's slot in the shelf
            // we need to save a reference to the subscribed action to then unsubscribe
            item.Shelf = this;
            _spawnedItems[slotIdx] = item;
            _subscribedActions[slotIdx] = () => FreeSlotOnItemPickup(slotIdx);
            item.onItemGrabbed += _subscribedActions[slotIdx];
        }

        ResetItemPhysics(item);
    }

    public void ClearShelf()
    {
        for (int i = 0; i < _spawnedItems.Count; i++)
        {
            if (_spawnedItems[i] != null)
            {
                _spawnedItems[i].onItemGrabbed -= _subscribedActions[i];
                Destroy(_spawnedItems[i].gameObject);
                _spawnedItems[i] = null;
            }
        }
    }

    private void ResetItemPhysics(ItemBehaviour itemBehaviour)
    {
        if (_isSpaceShelf)
        {
            // Scatter items through space
            itemBehaviour.Rigidbody.velocity = Random.insideUnitSphere.normalized * Random.Range(2f, 4f);
            itemBehaviour.Rigidbody.angularVelocity = Random.insideUnitSphere.normalized * Random.Range(.75f, 1.5f);
        }
        else
        {
            itemBehaviour.Rigidbody.velocity = Vector3.zero;
            itemBehaviour.Rigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void FreeSlotOnItemPickup(int slotIdx)
    {
        // Unsubscribe from event
        _spawnedItems[slotIdx].onItemGrabbed -= _subscribedActions[slotIdx];

        // Free up the slot
        _spawnedItems[slotIdx] = null;
    }

}
