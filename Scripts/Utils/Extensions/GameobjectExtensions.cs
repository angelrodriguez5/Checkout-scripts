using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameobjectExtensions
{
    /// <summary>
    /// Checks if the layer of the object is active in a layer mask
    /// </summary>
    public static bool IsInLayer(this GameObject obj, LayerMask layer)
    {
        return (1 << obj.layer & layer) != 0;
    }
}
