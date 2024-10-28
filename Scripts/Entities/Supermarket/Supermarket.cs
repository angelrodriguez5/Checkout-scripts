using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using Cinemachine;
using UnityEngine.AI;
using System;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct ShelfSectionData
{
    public List<Shelf> shelves;
    [Tooltip("Must be checked for FixedCategory to take effect")]
    public bool hasFixedCategory;
    public EItemCategory fixedCategory;
}


/// <summary>
/// Class that manages supermarkets shelves and objects and assings
/// players shopping lists
/// </summary>
public class Supermarket : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private List<ShelfSectionData> _shelfSections;

    [Header("Player positions")]
    [SerializeField] private List<Transform> _spawnPositions;
    [SerializeField] private NavMeshObstacle _spawnProtectionObstacle;

    private SupermarketTheme _theme;

    private Dictionary<ItemAsset, int> _totalItemAmounts = new Dictionary<ItemAsset, int>();
    private List<EItemCategory> _shelfSectionCategories = new();

    public List<Transform> SpawnPositions => _spawnPositions;
    public NavMeshObstacle SpawnProtectionObstacle => _spawnProtectionObstacle;

    public static Supermarket Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
            throw new Exception($"Several {typeof(Supermarket)} in scene");
        else
            Instance = this;

        // Find out the theme of the scene that contains this supermarket
        _theme = 
            SupermarketTheme.GetAllThemes()
                .Where(theme => theme.Supermarkets
                                                .Where(super => super.sceneName == gameObject.scene.name)
                                                .Count() != 0)
                .FirstOrDefault();

        if (_theme == null)
            throw new Exception($"[{typeof(Supermarket)}] Couldn't find a {typeof(SupermarketTheme)} that contains scene {gameObject.scene.name} as one of its supermarkets");
    }

    private void OnEnable()
    {
        GameManager.onItemDelivered += RespawnDeliveredItems;
    }

    private void OnDisable()
    {
        GameManager.onItemDelivered -= RespawnDeliveredItems;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    /// <summary>
    /// Spawns items in all the shelves of the supermarket, if a section has a fixed category
    /// that category will not spawn in any other section
    /// </summary>
    public void SpawnItems()
    {
        if (GameSettings.Current.matchGamemode == EGamemode.Pandemic)
        {
            _totalItemAmounts.Add(_theme.CovidModeItem, 0);

            // Fill all sections to 1/3 of their capacity with toilet paper
            foreach (var section in _shelfSections)
            {
                // Mark all sections with the category of the pandemic item
                _shelfSectionCategories.Add(_theme.CovidModeItem.ItemCategory);

                var sectionCapacity = section.shelves.Sum(x => x.Capacity);
                int itemsToSpawn = sectionCapacity / 3;
                switch (GameSettings.Current.listItemAmount)
                {
                    case EListItemAmount.Few:
                        itemsToSpawn = sectionCapacity / 5;
                        break;
                    case EListItemAmount.Normal:
                        itemsToSpawn = sectionCapacity / 3;
                        break;
                    case EListItemAmount.Lots:
                        itemsToSpawn = sectionCapacity / 3 + 1;  // 1 extra item per section
                        break;
                    default:
                        break;
                }

                // Spawn items
                var tmpSpawned = new List<GameObject>();
                for (int i = 0; i < itemsToSpawn; i++)
                {
                    tmpSpawned.Add(_theme.CovidModeItem.SpawnNewGameObject());
                }
                _totalItemAmounts[_theme.CovidModeItem] += itemsToSpawn;

                // Randomize if items will be at the beginning or the end of a section
                var shelfObjs = new List<GameObject>();
                shelfObjs.AddRange(tmpSpawned);

                // Load items into the shelves
                int skip = 0;
                foreach (var shelf in section.shelves)
                {
                    try
                    {
                        shelf.LoadItems(shelfObjs.Skip(skip).Take(shelf.Capacity).ToList());
                        skip += shelf.Capacity;
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            }
        }
        else
        {
            var allCategories = _theme.ThemeItemSet.AvailableCategories;
            var fixedCategories = _shelfSections.Where(section => section.hasFixedCategory).Select(section => section.fixedCategory);
            var otherCategories = allCategories.Except(fixedCategories).Shuffle().ToList();

            // Ensure at least one appearance of each OtherCategory before repeating any category
            var sectionCategories = new Queue<EItemCategory>(otherCategories.Concat(allCategories.Shuffle()));

            // In case of players having the same list ensure that there is enough quantity of each item
            int minItemAmount = GameManager.Instance.GameSettings.minItemAmount;
            if (GameManager.Instance.GameSettings.allPlayersSameList && minItemAmount < GameManager.Instance.NumberOfPlayers)
                minItemAmount = GameManager.Instance.NumberOfPlayers;

            foreach (var section in _shelfSections)
            {
                // Select a category for this section
                var category = section.hasFixedCategory ? section.fixedCategory : sectionCategories.Dequeue();
                _shelfSectionCategories.Add(category);
                List<ItemAsset> categoryItems = _theme.ThemeItemSet.Items.Where(item => item.ItemCategory == category).ToList();

                // Select the items that will be spawned in this section, spawning at least MIN_ITEM_COUNT of each
                int totalCapacity = section.shelves.Sum(shelf => shelf.Capacity);
                int numItemsOfCategory = totalCapacity / minItemAmount;
                categoryItems = categoryItems.Shuffle().Take(numItemsOfCategory).ToList();

                // Spawn the minimum amount of each of the items
                var spawnedItems = new List<GameObject>();
                foreach(var item in categoryItems)
                {
                    for (int i = 0; i < minItemAmount; i++)
                    {
                        spawnedItems.Add(item.SpawnNewGameObject());
                    }

                    // Store the amount of each item spawned
                    if (_totalItemAmounts.ContainsKey(item))
                        _totalItemAmounts[item] += minItemAmount;
                    else
                        _totalItemAmounts.Add(item, minItemAmount);
                }

                // Fill the rest of the capacity with more instances of the same items
                int idx = 0;
                while(spawnedItems.Count() < totalCapacity)
                {
                    spawnedItems.Add(categoryItems[idx].SpawnNewGameObject());
                    _totalItemAmounts[categoryItems[idx]]++;

                    idx = (idx + 1) % categoryItems.Count();
                }

                if (GameManager.Instance.GameSettings.shuffleSectionItems)
                    spawnedItems = spawnedItems.Shuffle().ToList();
                else
                    // Sort them so they appear in order in the shelves
                    spawnedItems = spawnedItems.OrderBy(x => x.GetComponent<ItemBehaviour>().ItemAsset.ItemName).ToList();

                // Load all shelves
                int skip = 0;
                foreach(var shelf in section.shelves)
                {
                    shelf.LoadItems(spawnedItems.Skip(skip).Take(shelf.Capacity).ToList());
                    skip += shelf.Capacity;
                }
            }
        }

        // Log items and quantities
        Debug.Log($"Total unique item types: {_totalItemAmounts.Keys.Count}");
        Debug.Log($"Total items spawned: {_totalItemAmounts.Select(x => x.Value).Sum()}");
        Debug.Log($"Spawned items :" + string.Join(", ", _totalItemAmounts.Select(x => x.Key.ItemName + $"({x.Value})")));
    }

    public GameObject SpawnDealItem()
    {
        return _theme.ThemeItemSet.SpawnRandomItem(EItemCategory.DealItems);
    }

    public void SpawnCovidTiebreakerItems(int playerAmount)
    {
        var goldenRoll = _theme.CovidModeItem.SpawnNewGameObject();
        goldenRoll.GetComponent<ItemBehaviour>().HighlightItemPermanently = true;

        _totalItemAmounts[_theme.CovidModeItem] = 1;

        var section = _shelfSections.GetRandomElement();
        var shelf = section.shelves.GetRandomElement();
        shelf.LoadItems(new List<GameObject> { goldenRoll});
    }

    /// <summary>
    /// Generates the players' shopping lists based on the items spawned on the map
    /// </summary>
    /// <param name="playerAmount"> How many shopping lists will be generated </param>
    /// <param name="itemAmount"> How many items per shopping list </param>
    /// <returns></returns>
    public List<ItemAsset>[] GeneratePlayerShoppingLists(int playerAmount, int itemAmount)
    {
        var playerLists = new List<ItemAsset>[playerAmount];

        // Dictionary of categories, with the items belonging to each category and the amount of that item spawned
        Dictionary<EItemCategory, List<(ItemAsset item, int count)>> categoryItemsWithQuantities = new Dictionary<EItemCategory, List<(ItemAsset item, int count)>>();

        foreach (var entry in _totalItemAmounts)
        {
            if (categoryItemsWithQuantities.TryGetValue(entry.Key.ItemCategory, out var list))
            {
                // Add item and amount to list
                list.Add((entry.Key, entry.Value));
            }
            else
            {
                // No items of this category: initialise list
                categoryItemsWithQuantities.Add(entry.Key.ItemCategory, new List<(ItemAsset item, int count)>{ (entry.Key, entry.Value) });
            }
        }

        // Calculate the shopping list for each player
        for (int i = 0; i < playerAmount; i++)
        {
            // If all players must have the same list, generate the first one and the duplicate it
            if (GameManager.Instance.GameSettings.allPlayersSameList && i > 0)
            {
                playerLists[i] = new List<ItemAsset>(playerLists[0]);
                continue;
            }

            // Assign items of different categories until we have the full list
            var playerItems = new List<ItemAsset>();
            var remainingCategories = new Queue<EItemCategory>(categoryItemsWithQuantities.Keys);
            while(playerItems.Count() < itemAmount)
            {
                // Ensure at least one item of each category, after that select random items
                var category = remainingCategories.Count > 0 ? remainingCategories.Dequeue() : categoryItemsWithQuantities.Keys.GetRandomElement();

                // Select a random item of the selected category, ensure that there's stock
                var item = categoryItemsWithQuantities[category].Where(entry => entry.count > 0 && !playerItems.Contains(entry.item)).GetRandomElement().item;

                if (item != null)
                {
                    // Add item to list
                    playerItems.Add(item);

                    //Decrement the number of available items
                    int idx = categoryItemsWithQuantities[category].FindIndex(entry => entry.item == item);
                    categoryItemsWithQuantities[category][idx] = (item, categoryItemsWithQuantities[category][idx].count - 1);
                }
            }

            playerLists[i] = playerItems;
        }

        return playerLists;
    }

    public int GetToiletPaperAmount()
    {
        return _totalItemAmounts[_theme.CovidModeItem];
    }

    public IEnumerator ClearAllItems()
    {
        // Clear items on shelves first
        foreach (var section in _shelfSections)
        {
            foreach (var shelf in section.shelves)
            {
                shelf.ClearShelf();
            }
        }

        yield return null;
        yield return null;

        // Then the rest of loose items
        foreach (var collider in Physics.OverlapSphere(transform.position, 100f, Layers.Item))
        {
            Destroy(collider.gameObject);
        }
    }

    public void ReturnItemToShelf(ItemBehaviour item)
    {
        // Find all sections of the category of the item with free slots
        var sections = _shelfSections
            .Where((section, index) => _shelfSectionCategories[index] == item.ItemAsset.ItemCategory
                                               && section.shelves.Any(shelf => shelf.HasFreeSlots));

        // This can happen when we turn in a deal without taking out any items from the shelves
        if (sections.Count() == 0) return;

        // Select a random shelf with free slots in one of those sections
        var shelf = sections.GetRandomElement().shelves.Where(s => s.HasFreeSlots).GetRandomElement();

        // Respawn item in shelf after a delay
        if (shelf)
            shelf.ReturnSingleItem(item);
        else
            Debug.Log($"Supermarket.ReturnItemToShelf: no shelf available for item {item} of category {item.ItemAsset.ItemCategory}");
    }

    private void RespawnDeliveredItems(PlayerAsset player, ItemAsset item)
    {
        // No item respawning in covid mode
        if (GameSettings.Current.matchGamemode == EGamemode.Pandemic) return;

        // Ignore deal items
        if (item.ItemCategory == EItemCategory.DealItems) return;

        // Find all sections of the category of the item with free slots
        var sections = _shelfSections
            .Where((section, index) => _shelfSectionCategories[index] == item.ItemCategory
                                               && section.shelves.Any(shelf => shelf.HasFreeSlots));

        // This can happen when we turn in a deal without taking out any items from the shelves
        if (sections.Count() == 0) return;

        // Select a random shelf with free slots in one of those sections
        var shelf = sections.GetRandomElement().shelves.Where(s => s.HasFreeSlots).GetRandomElement();

        // Respawn item in shelf after a delay
        if (shelf)
            shelf.SpawnSingleItemWithDelay(item);
        else
            Debug.Log($"Supermarket.RespawnDeliveredItems: no shelf available for item {item} of category {item.ItemCategory}");
    }
}