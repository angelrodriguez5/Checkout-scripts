using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tutorial.Prompts
{
    public class TutorialDealCounterPrompt : MonoBehaviour
    {
        public TutorialDealCounter dealCounter;
        public GameObject visuals;

        private void Update()
        {
            visuals.SetActive(dealCounter.IsDealPresent);
        }
    }
}