using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class DealWarningDisplay : MonoBehaviour
{
    public AudioClip _warningSound;

    Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        DealCounter.onDealStartWindup += ShowGraphic;
    }

    private void OnDisable()
    {
        DealCounter.onDealStartWindup -= ShowGraphic;
    }

    private void ShowGraphic() => _animator.SetTrigger("Trigger");

    public void WarningSoundAnimEvent()
    {
        AudioManager.Instance.EffectSource.PlayOneShot(_warningSound);
    }
}
