using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A pipe hazard that, when activated, releases steam for an amount of time and then
/// is turned off again.
/// 
/// Required components:
///  - Animator: the smoke particles and the hazard are controlled via animation
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class PipeHazard : MonoBehaviour
{
    public static List<PipeHazard> allPipeHazards = new List<PipeHazard>();

    [SerializeField] float _duration = 6f;
    [SerializeField] AudioClip _steamWarning;

    int _burstPipeAnimId = Animator.StringToHash("burst");
    
    Animator _animator;
    AudioSource _audioSource;

    public bool IsActive { get; private set; }

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        allPipeHazards.Add(this);   
    }

    private void OnDisable()
    {
        allPipeHazards.Remove(this);
    }

    public void ActivateHazard()
    {
        StopAllCoroutines();
        StartCoroutine(_ActivateHazard());
    }
    private IEnumerator _ActivateHazard()
    {
        IsActive = true;
        _animator.SetBool(_burstPipeAnimId, true);
        
        yield return new WaitForSeconds(_duration);

        IsActive = false;
        _animator.SetBool(_burstPipeAnimId, false);
    }

    public void PlayWarningSound()
    {
        _audioSource.PlayOneShot(_steamWarning);
    }

    public void PlaySteamSound()
    {
        _audioSource.Play();
    }

    public void StopSteamSound()
    {
        StartCoroutine(_audioSource.FadeOut());
    }
}
