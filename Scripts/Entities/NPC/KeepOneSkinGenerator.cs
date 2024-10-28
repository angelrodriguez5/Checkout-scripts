using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeepOneSkinGenerator : MonoBehaviour
{
    private void Awake()
    {
        var skinGenerators = GetComponents<NPCSkinGenerator>();
        var chosen = skinGenerators.GetRandomElement();

        foreach (var sg in skinGenerators)
            if (sg != chosen) Destroy(sg);
    }
}
