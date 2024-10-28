using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine.UI;

[CustomEditor(typeof(LanguageButton), true)]
/// <summary>
///   Custom Editor for the Button Component.
///   Extend this class to write a custom editor for an Button-derived component.
/// </summary>
public class LanguageButtonEditor : ButtonEditor
{
    SerializedProperty localeProperty;

    protected override void OnEnable()
    {
        base.OnEnable();
        localeProperty = serializedObject.FindProperty("locale");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(localeProperty);
        serializedObject.ApplyModifiedProperties();

        base.OnInspectorGUI();
    }
}
