using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tutorial.Prompts
{
    public class TutorialDeliveryAreaPrompt : MonoBehaviour
    {
        public TutorialPlayerZone tutorial;
        public TutorialDeliveryArea deliveryArea;
        public GameObject visuals;

        private void Update()
        {
            if (tutorial.Player && tutorial.Player.ObjectHeld != null && deliveryArea.IsOpen)
            {
                if (tutorial.CurrentStage == 0 && tutorial.CurrentSubstage == 1)
                    visuals.SetActive(false);
                else
                    visuals.SetActive(true);
            }
            else
                visuals.SetActive(false);
        }
    }
}