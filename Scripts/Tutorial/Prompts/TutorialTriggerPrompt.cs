using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Tutorial.Prompts
{
    public class TutorialTriggerPrompt : MonoBehaviour
    {
        public GameObject visuals;
        public Animator animator;

        private PlayerController player;

        private void OnEnable()
        {
            visuals.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent<PlayerController>(out player) && player.ObjectHeld != null)
            {
                visuals.SetActive(true);
                SetAnimatorParameter(true);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (player)
            {
                SetAnimatorParameter(player.ObjectHeld != null);
            }                        
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.TryGetComponent<PlayerController>(out player) && player.ObjectHeld != null)
            {
                visuals.SetActive(false);
                SetAnimatorParameter(false);
            }
        }

        public void SetAnimatorParameter(bool value)
        {
            animator.SetBool("playerPresent", value);
        }
    }
}
