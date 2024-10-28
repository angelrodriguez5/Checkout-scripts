using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowTransform : MonoBehaviour
{
    public Transform[] follow;
    public int index;
    public bool copyRotation;

    // Update is called once per frame
    void Update()
    {
        transform.position = follow[index].position;

        if (copyRotation)
            transform.rotation = follow[index].rotation;
    }
}
