using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HullBreach : MonoBehaviour
{
    public static List<HullBreach> allBreaches = new();

    [SerializeField] bool active;
    [SerializeField] Transform center;
    [SerializeField] float radius;
    [SerializeField] float force;
    [SerializeField] float duration;
    [SerializeField] ParticleSystem particles;
    [SerializeField] GameObject wall;
    [SerializeField] GameObject brokenWall;
    [SerializeField] AudioSource audioSource;

    Collider[] hits;

    public bool IsActive => active;

    private void Awake()
    {
        hits = new Collider[4];  // Max number of players
        wall.SetActive(true);
        brokenWall.SetActive(false);
    }

    private void OnEnable()
    {
        allBreaches.Add(this);
    }

    private void OnDisable()
    {
        allBreaches.Remove(this);
    }

    private void FixedUpdate()
    {
        if(active)
        {
            // Cast a slightly bigger sphere to detect when the players exit the range of the breach
            int numHits = Physics.OverlapSphereNonAlloc(center.position, radius + .5f, hits, Layers.Player);

            for (int i = 0; i < numHits; i++)
            {
                if(hits[i].TryGetComponent(out PlayerMovement playerMovement))
                {
                    // Only affect players in front and within the radius
                    if (   Vector3.Distance(playerMovement.transform.position, center.position) >= radius
                        || Vector3.Dot(center.forward, hits[i].transform.position - center.position) < 0)
                    {
                        playerMovement.AttractionForce = Vector3.zero;
                    }
                    else
                    {
                        // Force exponential depending on distance to center: d = radius -> modifier = 0; d = 0 -> modifier = 1
                        float forceModifier = (float)Math.Pow((Vector3.Distance(center.position, playerMovement.transform.position) - radius), 2f) / (radius * radius);
                        playerMovement.AttractionForce = (center.position - playerMovement.transform.position) * forceModifier * force;
                    }
                }
            }
        }
    }

    [ContextMenu("Activate")]
    public void Activate()
    {
        active = true;
        particles.Play();
        wall.SetActive(false);
        brokenWall.SetActive(true);
        audioSource.Play();

        StartCoroutine(DeactivateAfterDelay());
    }

    [ContextMenu("Deactivate")]
    public void Deactivate()
    {
        active = false;
        particles.Stop();
        wall.SetActive(true);
        brokenWall.SetActive(false);
        StartCoroutine(audioSource.FadeOut());

        // Remove movement modifier for all players that were hit last frame
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] && hits[i].TryGetComponent(out PlayerMovement playerMovement))
            {
                playerMovement.AttractionForce = Vector3.zero;
            }
        }
    }

    private IEnumerator DeactivateAfterDelay()
    {
        yield return new WaitForSeconds(duration);
        Deactivate();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center.position, radius);
    }
}
