using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AudioSourceExtensions
{
    public static IEnumerator FadeOut(this AudioSource audioSource, float seconds = 0.5f)
    {
        float startVolume = audioSource.volume;

        while (audioSource.volume > 0)
        {
            audioSource.volume -= startVolume * Time.deltaTime / seconds;
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume;
    }

    public static IEnumerator FadeIn(this AudioSource audioSource, float seconds = 0.5f)
    {
        float startVolume = audioSource.volume;
        audioSource.volume = 0;
        audioSource.Play();

        while (audioSource.volume < startVolume)
        {
            audioSource.volume += startVolume * Time.deltaTime / seconds;
            yield return null;
        }

        audioSource.volume = startVolume;
    }
}
