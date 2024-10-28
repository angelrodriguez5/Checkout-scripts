using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tutorial.Prompts
{
    public class TutorialShelfPrompt : MonoBehaviour
    {
        public Shelf shelf;
        public GameObject visuals;

        private void Update()
        {
            visuals.SetActive(shelf.HasItems);
        }
    }
}