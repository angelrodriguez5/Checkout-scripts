using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tutorial
{
    [RequireComponent(typeof(Collider))]
    public class TutorialExitTrigger : MonoBehaviour
    {
        public int countdownTime;
        public Image mask;

        public List<Image> playerImages;
        public Color playerPresentColor;
        public Color playerOutColor;

        public Animator animator;

        bool isTimerActive;
        bool blockEmptyingMask;
        float timer;
        Dictionary<GameObject, bool> playersPresent = new();

        private void Start()
        {
            foreach (var zone in TutorialPlayerZone.allZones)
            {
                playersPresent.Add(zone.Player.gameObject, false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (playersPresent.ContainsKey(other.gameObject))
            {
                playersPresent[other.gameObject] = true;
            }

            UpdateAvatarImages();

            if (playersPresent.All(kv => kv.Value == true))
            {
                timer = countdownTime;
                isTimerActive = true;
            }

            SetAnimatorParameter(playersPresent.Any(kv => kv.Value == true));
        }


        private void OnTriggerExit(Collider other)
        {
            if (playersPresent.ContainsKey(other.gameObject))
            {
                playersPresent[other.gameObject] = false;
            }

            UpdateAvatarImages();

            SetAnimatorParameter(playersPresent.Any(kv => kv.Value == true));
            isTimerActive = false;
        }

        private void UpdateAvatarImages()
        {
            // Get the number of present players
            int playerPresentCount = playersPresent.Values.Count(present => present);

            // For each image update its color
            for (int i = 0; i < playerImages.Count; i++)
            {
                playerImages[i].color = i < playerPresentCount ? playerPresentColor : playerOutColor;
            }
        }

        public void SetAnimatorParameter(bool value)
        {
            animator.SetBool("playerPresent", value);
        }

        private void Update()
        {
            if (isTimerActive)
            {
                timer -= Time.deltaTime;

                // Fill mask
                mask.fillAmount = 1 - (timer / countdownTime);

                if (timer <= 0f)
                {
                    isTimerActive = false;
                    blockEmptyingMask = true;
                    StartCoroutine(ReturnToMainMenu());
                }
            }
            else if (!blockEmptyingMask && mask.fillAmount > 0f)
            {
                // Animate emptying out the mask
                mask.fillAmount -= (1 / 0.3f) * Time.deltaTime;
            }
        }

        private IEnumerator ReturnToMainMenu()
        {
            // Exit tutorial
            PlayerPrefs.SetInt("isTutorialCompleted", 1);
            yield return TutorialUI.Instance.loadingScreen.FadeIn();
            SceneManager.LoadScene("MainMenu");
        }
    }
}