using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The order in which the objects will be interacted with
// first item has the least priority
public enum EInteractivePriorityType
{
    Lowest,
    Item,
    Highest
}

public interface IInteractive
{
    public EInteractivePriorityType Priority { get;}

    public bool CanInteract(GameObject interactor);
    public void Select();
    public void Deselect();
    public void Interact(GameObject interactor);
}
