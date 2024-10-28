using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HullBreachRandomEvent : RandomEventBase
{
    [SerializeField] int _breachesToActivateAtOnce;

    public override bool CanActivate => HullBreach.allBreaches.Count(x => !x.IsActive) >= _breachesToActivateAtOnce;

    public override void Activate()
    {
        var breaches = HullBreach.allBreaches.Shuffle().Take(_breachesToActivateAtOnce);

        foreach (var breach in breaches)
        {
            breach.Activate();
        }
    }
}
