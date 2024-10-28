using TMPro;
using UnityEngine;

public class BuildVersionUI : MonoBehaviour
{
    [SerializeField] TMP_Text buildVersionText;

    private void Awake()
    {
        buildVersionText.text = $"Version: {Application.version}";
    }
}
