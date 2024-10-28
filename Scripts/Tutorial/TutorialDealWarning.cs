using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tutorial
{
    [RequireComponent(typeof(Animator))]
    public class TutorialDealWarning : MonoBehaviour
    {
        Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public void ShowGraphic() => _animator.SetTrigger("Trigger");

    }
}