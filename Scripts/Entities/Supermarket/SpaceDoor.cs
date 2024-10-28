using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(AudioSource))]  // Trigger
public class SpaceDoor : MonoBehaviour
{

    AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.IsInLayer(Layers.Player))
            source.PlayOneShot(source.clip);
    }
}
