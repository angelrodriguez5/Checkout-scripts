using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Addon to slider that allows you to divide the bar into discrete options,
/// then set the slider to WholeNumbersOnly and add the background image for the options
/// to this component. Now the slider will highlight the option that is selected
/// </summary>
[RequireComponent(typeof(Slider))]
public class EnumSlider : MonoBehaviour
{
    [SerializeField] Image[] _options;
    [SerializeField] Color _colorTint;
    Slider _slider;

    Image _selectedOption;
    Color _originalColor;

    public static int GetSliderValueFromEnum<T>(T enumValue) where T : Enum
    {
        var enumValues = Enum.GetValues(typeof(T)).Cast<T>().ToList();

        //Debug.Log(String.Join(" ", enumValues));
        //Debug.Log(enumValue);
        //Debug.Log(enumValues.IndexOf(enumValue));

        return enumValues.IndexOf(enumValue);
    }

    public static T GetEnumValueFromSlider<T>(int idx) where T : Enum
    {
        var enumValues = Enum.GetValues(typeof(T)).Cast<T>().ToList();
        return enumValues[idx];
    }

    private void Awake()
    {
        _slider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        _slider.onValueChanged.AddListener(SelectOption);
    }

    private void OnDisable()
    {
        _slider.onValueChanged.RemoveListener(SelectOption);
    }

    private void Start()
    {
        // Highlight the default option corresponding to slider serialized value
        SelectOption(_slider.value);
    }

    private void SelectOption(float sliderValue)
    {
        int i = (int)sliderValue;

        if (_options.Length <= i) return;

        if (_selectedOption != null)
        {
            _selectedOption.color = _originalColor;
        }

        _selectedOption = _options[i];
        _originalColor = _selectedOption.color;
        _selectedOption.color += _colorTint;
    }
}
