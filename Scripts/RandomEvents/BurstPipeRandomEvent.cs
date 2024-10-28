using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class BurstPipeRandomEvent : RandomEventBase
{
    public override bool CanActivate => PipeHazard.allPipeHazards.Any(x => !x.IsActive);

    public override void Activate()
    {
        foreach(var pipe in PipeHazard.allPipeHazards)
        {
            pipe.ActivateHazard();
        }
    }

}
