using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// If we disable the object during an animation, the property values will remain as they were in the frame that the object was deactivated.
/// Then when the object is reactivated this (wrong) values will be considered the default state of the object.
/// This script saves and restores certain default values regarless of the animator to fix this problem
/// </summary>
public class ButtonAnimatorFix : MonoBehaviour
{
    public TMP_Text text;
    public Image image;

    private Color _textColor;
    private Sprite _imageSprite;

    private void OnEnable()
    {
        // Save default values
        _textColor = text.color;
        _imageSprite = image.sprite;
    }

    private void OnDisable()
    {
        // Restore defaults
        text.color = _textColor;
        image.sprite = _imageSprite;
    }
}
