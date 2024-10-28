using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class LanguageButton : Button
{
    public Locale locale;

    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);
        LocalizationSettings.SelectedLocale = locale;
    }
}
