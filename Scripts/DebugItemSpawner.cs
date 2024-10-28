using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugItemSpawner : MonoBehaviour
{
    [SerializeField] private Vector2 _spacing = new Vector2(1.5f, 3f);
    [SerializeField] private int _objectsPerRow = 10;
    [SerializeField] ItemSet itemSet;

    private ItemDB _itemDB;

    // Start is called before the first frame update
    void Start()
    {
        Vector3 position = transform.position;
        for (int i = 0; i < itemSet.Items.Count; i++)
        {
            var item = itemSet.Items[i].SpawnNewGameObject();
            item.transform.position = position;

            // Advance in row
            position.x += _spacing.x;

            // Switch column, reset row
            if ((i+1) % _objectsPerRow == 0)
            {
                position.x = transform.position.x;
                position.z += _spacing.y;
            }
        }
    }
}
