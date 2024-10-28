using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractionSystem : MonoBehaviour
{
    [Header("Interaction box config")]
    [SerializeField] private Vector3 _interactionBoxOffset;
    [SerializeField] private Vector3 _interactionBoxSize;

    private List<IInteractive> _interactivesInRange = new List<IInteractive>();

    public IInteractive Selected { get; private set; }

    private void Update()
    {
        // Overlap box and check for IInteractive inside it
        var colliders = Physics.OverlapBox(
            transform.TransformPoint(_interactionBoxOffset),
            _interactionBoxSize / 2,
            transform.rotation
            );

        _interactivesInRange.Clear();
        foreach(var collider in colliders)
        {
            if(collider.gameObject.TryGetComponent<IInteractive>(out var interactive))
            {
                if (interactive.CanInteract(gameObject))
                    _interactivesInRange.Add(interactive);
            }
        }

        // If we have a selected object and its still available, dont select another of the same priority
        // keep the previously selected instead
        var newSelected = _interactivesInRange.OrderByDescending(x => x.Priority).FirstOrDefault();
        if (Selected != null && _interactivesInRange.Contains(Selected))
        {
            if (Selected.Priority < newSelected.Priority)
                ChangeSelection(newSelected);
        }
        else
        {
            ChangeSelection(newSelected);
        }
    }

    private void ChangeSelection(IInteractive newSelected)
    {
        if (Selected == newSelected) return;

        if (Selected != null)
            Selected.Deselect();
        
        Selected = newSelected;

        if (Selected != null)
            Selected.Select();
    }

    public void PerformInteraction()
    {
        if (Selected != null && Selected.CanInteract(gameObject))
            Selected.Interact(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // Select color
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);
        Gizmos.color = Selected != null ? transparentGreen : transparentRed;

        // Set origin and rotation of the gizmo to be the same as gameobject's transform
        Gizmos.matrix = transform.localToWorldMatrix;
        
        // Draw cube
        Gizmos.DrawCube(_interactionBoxOffset, _interactionBoxSize);
    }
}
