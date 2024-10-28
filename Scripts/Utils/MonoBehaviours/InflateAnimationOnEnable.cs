using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// When this object is activated, instead of appearing instantly it will be animated
/// changing its scale from 0 to original scale
/// </summary>
public class InflateAnimationOnEnable : MonoBehaviour
{
    float _speed = 5f;
    Vector3 _originalScale;

    private void Awake()
    {
        _originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        StartCoroutine(Inflate());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator Inflate()
    {
        // For objects with rigidbody, make them kinematic it during the animation
        var rb = GetComponent<Rigidbody>();
        bool isKinematicOriginal = false;
        if (rb)
        {
            isKinematicOriginal = rb.isKinematic;
            rb.isKinematic = true;
        }

        transform.localScale = Vector3.zero;
        float t = 0f;

        while (t < 1f)
        {
            t += _speed * Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.zero, _originalScale, t);
            yield return null;
        }

        transform.localScale = _originalScale;

        if (rb) rb.isKinematic = isKinematicOriginal;
    }
}
