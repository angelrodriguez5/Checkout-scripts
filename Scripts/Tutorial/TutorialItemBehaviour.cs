using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tutorial
{

    /// <summary>
    /// The representation of an ItemAsset as a gameObject
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class TutorialItemBehaviour : ItemBehaviour
    {
        private static readonly float LOSE_OWNERSHIP_TIME = 2f;  // Amount of time that a thown object will be considered as owned by the player the threw it
        private static readonly float RETURN_TO_SHELF_TIME = 2f;  // Amount of time that an object needs to be idle to return to its shelf

        public TutorialPlayerZone tutorial;

        private void Update()
        {
            // Push cooldown
            if (_pushTimer > 0)
                _pushTimer -= Time.deltaTime;

            // Return to shelf when object is idle outside its shelf
            if (_checkReturnToShelf)
            {
                _returnToShelfTimer += Time.deltaTime;

                if (_returnToShelfTimer >= RETURN_TO_SHELF_TIME)
                {
                    // Tutorial items return to their shelf no matter what
                    tutorial.ReturnItemToShelf();

                    _checkReturnToShelf = false;
                    _returnToShelfTimer = 0f;
                }
            }
        }
    }
}
