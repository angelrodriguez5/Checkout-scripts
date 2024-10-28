using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableRigidbody : MonoBehaviour
{
    public GameObject[] objectsToDisable;
    
    void Start()
    {
        foreach (GameObject obj in objectsToDisable)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; // Set the rigidbody to kinematic
                rb.detectCollisions = false; // Disable collision detection
            }
        }
    }

}