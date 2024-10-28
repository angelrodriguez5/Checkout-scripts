using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using System.Collections;

public class AnimatedTimer : Timer
{
    [SerializeField] Animator _animator;
    [SerializeField] AudioClip _30secondWarning;

    private bool _warningPlayed = false;

    private int _animLastSeconds = Animator.StringToHash("LastSeconds");

    private void Update()
    {
        if (!Active) return;

        if (!_warningPlayed && TargetSeconds - _currentSeconds <= 30)
        {
            AudioManager.Instance.EffectSource.PlayOneShot(_30secondWarning);
            AudioManager.Instance.AccelerateMusic();
            _warningPlayed = true;
        }

        if (TargetSeconds - _currentSeconds <= 10)
        {
            _animator.SetBool(_animLastSeconds, true);
            TextFormat = FormatIntSeconds;
        }
    }

    public void HideTimer()
    {
        _animator.gameObject.SetActive(false);
    }
}
