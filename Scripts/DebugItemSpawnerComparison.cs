using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DebugItemSpawnerComparison : MonoBehaviour
{
    [SerializeField] private Vector2 _spacing = new Vector2(1f, .5f);
    [SerializeField] ItemSet itemSet;

    // Start is called before the first frame update
    void Start()
    {
        Vector3 initialPosition = transform.position;
        Vector3 position = initialPosition;
        var permutations = GetPermutations(itemSet.Items, 2);

        int count = 0;
        int laps = 0;
        foreach (var perm in permutations)
        {
            var ipos = position;
            foreach (var item in perm)
            {
                var i = item.SpawnNewGameObject();
                i.transform.position = ipos;
                ipos.z += _spacing.y;
            }

            position.x += _spacing.x;

            if (count >= itemSet.Items.Count)
            {
                position.x = initialPosition.x;
                position.z += 3 * _spacing.y;
                count = laps;
                laps++;
            }

            count++;
        }
    }

    IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> items, int count)
    {
        int i = 0;
        foreach (var item in items)
        {
            if (count == 1)
                yield return new T[] { item };
            else
            {
                foreach (var result in GetPermutations(items.Skip(i + 1), count - 1))
                    yield return new T[] { item }.Concat(result);
            }

            ++i;
        }
    }
}
