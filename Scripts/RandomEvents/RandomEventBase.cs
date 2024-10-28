using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Random supermarket event triggered autonomously
/// </summary>
public abstract class RandomEventBase : MonoBehaviour
{
    [Header("Base event config")]
    [Tooltip("Time from the beginning of the match to the first triggering of this event")]
    [SerializeField] protected float matchStartGracePeriod;
    [SerializeField] protected float timeBetweenEvents;
    [SerializeField] protected float varianceBetweenEvents;

    #region Abstract part
    public abstract bool CanActivate { get; }

    public abstract void Activate();
    #endregion

    protected void OnEnable()
    {
        GameManager.onMatchStarted += StartTriggeringEvents;
        GameManager.onMatchFinished += StopTriggeringEvents;
    }

    private void OnDisable()
    {
        GameManager.onMatchStarted -= StartTriggeringEvents;
        GameManager.onMatchFinished -= StopTriggeringEvents;
    }

    protected void StartTriggeringEvents() => StartCoroutine(EventTriggeringRoutine());
    protected void StopTriggeringEvents() => StopAllCoroutines();

    protected IEnumerator EventTriggeringRoutine()
    {
        yield return new WaitForSeconds(matchStartGracePeriod);

        while(true)
        {
            if (CanActivate)
                Activate();

            float waitTime = timeBetweenEvents + Random.Range(-varianceBetweenEvents, varianceBetweenEvents);
            yield return new WaitForSeconds(waitTime);
        }
    }
}