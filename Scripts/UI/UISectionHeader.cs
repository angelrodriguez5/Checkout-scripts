using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The graphics indicated here will be selected as long as any of the selectables is selected
/// </summary>
public class UISectionHeader : Selectable
{
    [Space(20f)]
    [SerializeField] List<Selectable> _sectionSelectables;
    [SerializeField] Graphic[] _graphics;

    bool _selected;
    GameObject _oldSelectedObj;

    protected override void OnEnable()
    {
        base.OnEnable();

        foreach (var graphic in _graphics)
        {
            targetGraphic = graphic;
            base.OnDeselect(null);
        }
    }

    private void Update()
    {
        // Exit here when in Edit Mode
        if (!Application.isPlaying) return;

        // Detect event system selection change
        if (EventSystem.current.currentSelectedGameObject != _oldSelectedObj)
        {
            _oldSelectedObj = EventSystem.current.currentSelectedGameObject;

            var selectable = _sectionSelectables.Find(x => x.gameObject == EventSystem.current.currentSelectedGameObject);
            // The current selected gameobject is in our targets
            if (selectable)
            {
                // If we were already selected then do nothing
                if (_selected) return;
                // else select the graphics using the selectable's parameters
                else
                {
                    foreach (var graphic in _graphics)
                    {
                        targetGraphic = graphic;
                        base.OnSelect(null);
                    }
                    _selected = true;
                }
            }

            // The current selected gameobject is NOT in our targets
            else
            {
                // If selected: deselect
                if (_selected)
                {
                    foreach (var graphic in _graphics)
                    {
                        targetGraphic = graphic;
                        base.OnDeselect(null);
                    }
                    _selected = false;
                }
                // Else do nothing
                else return;
            }
        }
    }
}
