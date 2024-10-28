using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class ItemDB
{
    private List<ItemAsset> _items;

    private static ItemDB _instance;
    public static ItemDB Instance 
    { 
        get
        {
            if (_instance == null)
                _instance = new ItemDB();
            return _instance;
        }
    }

    public int ItemCount => _items.Count;

    private ItemDB()
    {
        _items = Resources.LoadAll<ItemAsset>("Items").ToList();
        _instance = this;
        Debug.Log($"ItemDB loaded {_items.Count} items");
    }

    public GameObject SpawnRandomItem(EItemCategory? category = null)
    {
        ItemAsset item;
        if (category != null)
        {
            item = _items.Where(x => x.ItemCategory == category).GetRandomElement();
        }
        else
        {
            item = _items.GetRandomElement();
        }

        return item.SpawnNewGameObject();
    }

    public List<ItemAsset> GetRandomItemList(int lenght)
    {
        // List all the item indexes and shuffle them
        var indexes = Enumerable.Range(0, _items.Count).Shuffle().Take(lenght);
        // Pick the first "lenght" amount of them
        List<ItemAsset> assets = new List<ItemAsset>();
        foreach (int index in indexes)
        {
            assets.Add(_items[index]);
        }

        return assets;
    }

    public GameObject SpawnItem(int index)
    {
        return _items[index].SpawnNewGameObject();
    }
}
