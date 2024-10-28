using System.Collections;
using System.Collections.Generic;
using Tutorial;
using UnityEngine;

public class TutorialSubstagePrompt : MonoBehaviour
{
    public int stage;
    public int substage;
    public GameObject visuals;
    public TutorialPlayerZone tutorial;

    private void Update()
    {
        if (tutorial.CurrentStage == stage && tutorial.CurrentSubstage == substage)
            visuals.SetActive(true);
        else
            visuals.SetActive(false);
    }
}
