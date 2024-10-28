using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(menuName = "PartyGame/ItemSet")]
public class ItemSet : ScriptableObject
{
    [SerializeField] private List<ItemAsset> _items;

    public List<ItemAsset> Items => _items;

    /// <summary>
    /// Categories of the items present in this item set, excluding DealItems
    /// </summary>
    public List<EItemCategory> AvailableCategories => _items.Where(x => x.ItemCategory != EItemCategory.DealItems).Select(x => x.ItemCategory).Distinct().ToList();

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
}

#if UNITY_EDITOR
    [CustomEditor(typeof(ItemSet))]
    public class Example : Editor
    {
        public override void OnInspectorGUI()
        {
            ItemSet itemSet = (ItemSet)target;

            // Display number of items in each category
            var itemsByCategory = itemSet.Items.GroupBy(item => item.ItemCategory);
            foreach (var categoryGroup in itemsByCategory)
            {
                EditorGUILayout.LabelField($"{categoryGroup.Key} ({categoryGroup.Count()})");
            }

            // Show default inspector property editor
            if (DrawDefaultInspector())
            {
            }
        }
    }
#endif
