using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuAudioSource : MonoBehaviour
{
    public AudioClip movementClip;

    private AudioSource _source;

    private void Start()
    {
        _source = AudioManager.Instance.UiSource;
    }

    public void PlayOneShot(AudioClip clip)
    {
        if (_source)
            _source.PlayOneShot(clip);
    }

    public void PlayMovementSound(BaseEventData eventData)
    {
        if(eventData is AxisEventData)
        {
            //Debug.Log("navigation with axis event");
            _source.PlayOneShot(movementClip);
        }
        else
        {
            //Debug.Log("navigation WITHOUT axis event");
        }
    }
}
