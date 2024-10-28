using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class Timer : MonoBehaviour
{
    protected float _currentSeconds = 0f;

    public bool Active { get; set; }
    public bool Regressive { get; set; }
    public bool DestroyOnFinish { get; set; }
    public Action DoOnTimerFinish { get; set; }
    public float TargetSeconds { get; set; }
    public TMP_Text TextDisplay { get; set; }
    public Func<float, string> TextFormat { get; set; }

    public bool IsRunning => TargetSeconds != _currentSeconds;

    protected void Awake()
    {
        // Default values;
        Active = false;
        TextFormat = FormatMinutesSeconds;
    }

    protected void LateUpdate()
    {
        if (!Active) return;

        _currentSeconds += Time.deltaTime;
        if (_currentSeconds >= TargetSeconds)
        {
            _currentSeconds = TargetSeconds;
            Active = false;
        }

        UpdateUI();

        // Timer finished
        if (_currentSeconds == TargetSeconds)
        {
            if (DoOnTimerFinish != null)
                DoOnTimerFinish();

            if (DestroyOnFinish)
                Destroy(this);
        }
    }

    /// <summary>
    /// Resets current count to 0 and activates timer
    /// </summary>
    public void Restart()
    {
        _currentSeconds = 0;
        Active = true;
    }

    public void UpdateUI()
    {
        float printSecs = Regressive ? (TargetSeconds - _currentSeconds) : _currentSeconds;

        if (TextDisplay != null)
        {
            TextDisplay.text = TextFormat(printSecs);
        }
    }

    #region PreImplemented formats
    public static string FormatMinutesSeconds(float value)
    {
        return $"{(int)(value / 60):00}:{(int)(value % 60):00}";
    }

    public static string FormatIntSeconds(float value)
    {
        return $"{(int)Mathf.Ceil(value)}";
    }

    public static Func<float, string> WrapperFormatSecondsWithDecimals(int numDecimals = 2)
    {
        // for 2 numDecimals: format = 0.00
        var format = "0.";
        for (int i = 0; i < numDecimals; i++)
        {
            format += "0";
        }

        string Function(float value)
        {
            return value.ToString(format);
        }

        return Function;
    }
    #endregion
}
