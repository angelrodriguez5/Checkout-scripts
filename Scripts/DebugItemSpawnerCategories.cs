using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DebugItemSpawnerCategories : MonoBehaviour
{
    [SerializeField] private Vector2 _spacing = new Vector2(1f, .5f);
    [SerializeField] ItemSet itemSet;

    // Start is called before the first frame update
    void Start()
    {
        var items = itemSet.Items.OrderBy(x => x.ItemCategory);

        Vector3 position = transform.position;
        float initialX = position.x;
        EItemCategory prevCategory = items.First().ItemCategory;
        foreach (var item in items)
        {
            if (item.ItemCategory != prevCategory)
            {
                position.x = initialX;
                position.z += _spacing.y;
                prevCategory = item.ItemCategory;
            }

            var obj = item.SpawnNewGameObject();
            obj.transform.position = position;
            position.x += _spacing.x;


        }
    }
}
