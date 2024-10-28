using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptableObjectSingleton<T> : ScriptableObject where T : ScriptableObjectSingleton<T>
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                // Get scriptable object from resources
                T[] assets = Resources.LoadAll<T>("");
                if (assets == null || assets.Length < 1)
                    throw new System.Exception($"No resources of type {typeof(T).Name}");
                else if (assets.Length > 1)
                    throw new System.Exception($"Several resources of type {typeof(T).Name} were found");

                _instance = assets[0];
                _instance.hideFlags = HideFlags.DontUnloadUnusedAsset;
            }

            return _instance;
        }
    }

}
