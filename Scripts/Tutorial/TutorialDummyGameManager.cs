using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tutorial
{
    public class TutorialDummyGameManager : GameManager
    {
        // Override all lifecycle
        private void Awake()
        {
            // Singleton
            if (Instance != null)
                throw new Exception("Several game managers in scene");
            else
                Instance = this;
        }

        private void OnEnable()
        {

        }
        private void OnDisable()
        {

        }
        private void Start()
        {

        }
        private void Update()
        {

        }
    }
}