using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class IEnumerableExtensions
{
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> list)
    {
        return list.OrderBy(x => Random.Range(0, list.Count() + 1));
    }

    public static void Map<T>(this IEnumerable<T> list, System.Action<T> action)
    {
        foreach (var value in list)
            action(value);
    }

    public static T GetRandomElement<T>(this IEnumerable<T> list)
    {
        if (list.Count() == 0) return default(T);
        return list.ElementAt(Random.Range(0, list.Count()));
    }
}
