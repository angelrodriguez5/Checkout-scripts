using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ShowcaseObject : MonoBehaviour
{
    private static List<float> _allShowcases = new();

    private static float DISAPPEAR_VOLUME;

    public GameObject canvas;
    public AudioClip appearSound, disappearSound;

    Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _animator.enabled = false;
        canvas.SetActive(false);
        DISAPPEAR_VOLUME = 1;
    }

    private void OnEnable()
    {
        _allShowcases.Add(transform.position.x);
        GameManager.onLayoutShowcaseStarted += ShowShowcase;
        GameManager.onLayoutShowcaseFinished += HideShowcase;
    }

    private void OnDisable()
    {
        _allShowcases.Remove(transform.position.x);
        GameManager.onLayoutShowcaseStarted -= ShowShowcase;
        GameManager.onLayoutShowcaseFinished -= HideShowcase;
    }

    private void ShowShowcase() => StartCoroutine(_ShowShowcaseAfterDelay());
    private IEnumerator _ShowShowcaseAfterDelay()
    {
        // Showcase objects will appear from left to right
        float interval = 0.5f;
        _allShowcases.Sort();
        float waitTime = (_allShowcases.IndexOf(transform.position.x) + 1) * interval;
        yield return new WaitForSeconds(waitTime);

        canvas.SetActive(true);
        _animator.enabled = true;
    }

    private void HideShowcase()
    {
        _animator.SetTrigger("dissappear");
        //AudioManager.Instance.EffectSource.PlayOneShot(disappearSound, DISAPPEAR_VOLUME);
        // Only play 1 disappear sound with volume
        DISAPPEAR_VOLUME = 0;
    }

    public void PlayStickSoudAnimEvent()
    {
        AudioManager.Instance.EffectSource.PlayOneShot(appearSound);
    }

    public void DisableAnimatorAndCanvasAnimEvent()
    {
        _animator.enabled = false;
        canvas.SetActive(false);
    }
}
