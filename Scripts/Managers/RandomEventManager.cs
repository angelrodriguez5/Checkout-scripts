using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Manager that activates random map events based on timing and probability.
/// After the game starts it waits minTimeBetweenEvents before trying to launch the first event.
/// The amount of events per match is fixed and they will trigger in regular intervals with a 
/// small time variance.
/// </summary>
public class RandomEventManager : MonoBehaviour
{
    [Serializable]
    private struct EventData
    {
        public RandomEventBase randomEvent;
        public float frequency;
    }

    [SerializeField] int _eventsPerMatch = 6;
    [SerializeField] float _eventTimingVariance = 2f;
    [SerializeField] List<EventData> _events = new List<EventData>();

    List<RandomEventBase> _matchEvents = new List<RandomEventBase>();

    int _eventsActivated = 0;

    private void Start()
    {
        foreach (var eventData in _events)
        {
            var eventInstances = Mathf.Floor(_eventsPerMatch * eventData.frequency);
            for (int i = 0; i < eventInstances; i++)
            {
                _matchEvents.Add(eventData.randomEvent);
            }
        }

        if (_matchEvents.Count() < _eventsPerMatch)
        {
            EventData fillerEvent = _events.First();
            for (int i = 0; i < _eventsPerMatch - _matchEvents.Count(); i++)
            {
                _matchEvents.Add(fillerEvent.randomEvent);
            }
        }
    }

    private void OnEnable()
    {
        GameManager.onMatchStarted += MatchStarted;
        GameManager.onMatchFinished += MatchFinished;
    }

    private void OnDisable()
    {
        GameManager.onMatchStarted -= MatchStarted;
        GameManager.onMatchFinished -= MatchFinished;
    }

    private void MatchFinished()
    {
        StopAllCoroutines();
    }

    private void MatchStarted()
    {
        StartCoroutine(LaunchRandomEvents());
    }

    private IEnumerator LaunchRandomEvents()
    {
        float beginningRelaxTime = 10f;
        float timeBetweenEvents;
        if (GameManager.Instance.GameSettings.matchDuration == EMatchDuration.Unlimited)
            timeBetweenEvents = 20f;
        else
            timeBetweenEvents = ((int)GameManager.Instance.GameSettings.matchDuration - beginningRelaxTime) / _eventsPerMatch;

        // At the beginning of the match wait for a grace period
        yield return new WaitForSeconds(beginningRelaxTime);

        // Continue until the maximum number of events have been activated during the match
        while (_eventsActivated < _eventsPerMatch)
        {
            var randomEvent = _matchEvents.GetRandomElement();
            _matchEvents.Remove(randomEvent);
            if (randomEvent != null)
            {
                // Launch random event
                randomEvent.Activate();
                _eventsActivated++;
            }
            // Generate new timer
            var  timeToNextEvent = timeBetweenEvents + Random.Range(-_eventTimingVariance, _eventTimingVariance);
            yield return new WaitForSeconds(timeToNextEvent);
           
        }
    }
}