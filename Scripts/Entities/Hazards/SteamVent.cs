using UnityEngine;

/// <summary>
/// A steam vent that pushes back the player when he touches it
/// 
/// Requierd components:
///  - Collider TRIGGER: the area where the vent will take effect
/// </summary>
[RequireComponent(typeof(Collider), typeof(AudioSource))]
public class SteamVent : MonoBehaviour
{
    AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerController>(out var player))
        {
            _audioSource.Play();
            player.Pushback(-player.transform.forward);
        }
    }
}
