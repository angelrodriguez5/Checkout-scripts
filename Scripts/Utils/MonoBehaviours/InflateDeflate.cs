using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// When this object is activated, instead of appearing instantly it will be animated
/// changing its scale from 0 to original scale
/// </summary>
public class InflateDeflate : MonoBehaviour
{
    public bool startInflated;
    public bool isInflated;

    float _speed = 5f;
    Vector3 _originalScale;
    bool _previousInflated;

    private void Awake()
    {
        _originalScale = transform.localScale;

        // Set starting values of scale and flags
        isInflated = startInflated;
        _previousInflated = isInflated;
        if (!startInflated) transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        // Detect changes in isInflated value
        if (isInflated != _previousInflated)
        {
            _previousInflated = isInflated;
            if (isInflated)
            {
                Debug.Log("inflating");
                StartCoroutine(Inflate());
            }
            else
            {
                Debug.Log("deflating");
                StartCoroutine(Deflate());
            }
        }
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

    private IEnumerator Deflate()
    {
        // For objects with rigidbody, make them kinematic it during the animation
        var rb = GetComponent<Rigidbody>();
        bool isKinematicOriginal = false;
        if (rb)
        {
            isKinematicOriginal = rb.isKinematic;
            rb.isKinematic = true;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += _speed * Time.deltaTime;
            transform.localScale = Vector3.Lerp(_originalScale, Vector3.zero, t);
            yield return null;
        }

        transform.localScale = Vector3.zero;

        if (rb) rb.isKinematic = isKinematicOriginal;
    }
}
