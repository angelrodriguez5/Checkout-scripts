using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies a random variation of rotation of +- variance to the object on each of the selected axis
/// </summary>
public class RotateOnStart : MonoBehaviour
{
    [Tooltip("Angle will be altered by a random value between -variance and +variance")]
    public float variance = 5f;

    [Header("Axis")]
    public bool x;
    public bool y;
    public bool z = true;

    private void Start()
    {
        Vector3 eulerAngles = transform.rotation.eulerAngles;

        if (x)
        {
            eulerAngles += new Vector3(Random.Range(-variance, variance), 0f, 0f);
        }
        if (y)
        {
            eulerAngles += new Vector3(0f, Random.Range(-variance, variance), 0f);
        }
        if (z)
        {
            eulerAngles += new Vector3(0f, 0f, Random.Range(-variance, variance));
        }

        transform.rotation = Quaternion.Euler(eulerAngles);
        Canvas.ForceUpdateCanvases();
    }
}
