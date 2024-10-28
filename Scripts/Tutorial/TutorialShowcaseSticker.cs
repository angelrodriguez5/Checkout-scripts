using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class TutorialShowcaseSticker : MonoBehaviour
{
    public GameObject canvas;
    public AudioClip appearSound, disappearSound;

    Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _animator.enabled = false;
        canvas.SetActive(false);
    }

    public void ShowShowcase()
    {
        canvas.SetActive(true);
        _animator.enabled = true;
    }

    public void HideShowcase()
    {
        _animator.SetTrigger("dissappear");
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
