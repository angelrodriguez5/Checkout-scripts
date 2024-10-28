using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Each time this is activated a counter is shutdown for a random amount of time.
/// At least one of the counters will be open at any given time
/// </summary>
public class CounterShutdownRandomEvent : RandomEventBase
{
    [Header("Counter shutdown config")]
    [SerializeField] float _minShutdownTime = 10;
    [SerializeField] float _maxShutdownTime = 20;
    //[Tooltip("The minumum time that has to pass before a counter can shut down again")]
    //[SerializeField] float _counterMinOpenTime = 10;

    public override bool CanActivate => DeliveryArea.allAreas.Where(counter => counter.IsOpen).Count() > 1;

    public override void Activate() => StartCoroutine(ShutdownCounter());

    private IEnumerator ShutdownCounter()
    {
        var counter = DeliveryArea.allAreas.Where(counter => counter.IsOpen).GetRandomElement();
        counter.IsOpen = false;
        // Wait for the counter to finish processing items
        yield return counter.ProcessQueueRoutine;
        // Then stay closed for a certain amount of time
        yield return new WaitForSeconds(Random.Range(_minShutdownTime, _maxShutdownTime));
        counter.IsOpen = true;
    }
}
