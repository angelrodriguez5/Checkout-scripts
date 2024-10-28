using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tutorial
{
    public class TutorialPromptSprite : MonoBehaviour
    {
        public bool isInteraction;
        public bool isDash;
        public Image defaultImage;
        public Image pressedImage;

        public void ConfigurePrompt(PromptData data)
        {
            if (isInteraction)
            {
                defaultImage.sprite = data.interactDefault;
                pressedImage.sprite = data.interactPressed;
            }
            else if (isDash)
            {
                defaultImage.sprite = data.dashDefault;
                pressedImage.sprite = data.dashPressed;
            }
        }
    }
}