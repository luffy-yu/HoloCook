//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System;
using HoloCook.HoloLens2;
using HoloCook.Utility;
using UnityEngine;
using Stuff = HoloCook.Menu.Static.Stuff;

namespace HoloCook.Menu
{
    public class BoardTrigger : MonoBehaviour
    {
        private bool bananaEntered = false;

        public HandPalmTracker handPalmTracker;

        public GameObject tearedBanana;

        private void Start()
        {
            EnableTearedBanana(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            var name = other.gameObject.name;

            var right = handPalmTracker.GetAttached(Handness.Right);

            var banana = Static.GetName(Stuff.Banana);

            if (name.Equals(banana) || (right != null && right.name.Equals(banana)))
            {
                bananaEntered = true;
                // detach it and format banana position
                handPalmTracker.DetachObject(false, Handness.Right);
                
                // BUG: sometimes the above code doesn't work
                // Force detach and set inactive
                handPalmTracker.DetachLocally(Handness.Right);
                right.SetActive(false);
                
                // show the teared banana
                EnableTearedBanana(true);
                handPalmTracker.SendActivateMessage(tearedBanana);
                // enable board
                handPalmTracker.SendActivateMessage(Stuff.Board);
                // disable banana
                handPalmTracker.SendDeactivateMessage(Stuff.Banana);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var name = other.gameObject.name;
            if (name.Equals(Static.GetName(Stuff.Knife)) && bananaEntered)
            {
                var rot = other.gameObject.transform.eulerAngles;
                var kc = other.gameObject.GetComponent<KnifeController>();
                if (rot.z > kc.movingThreshold)
                {
                    // flat to move banana to the bowl
                    // hide teared banana
                    EnableTearedBanana(false);
                    // show banana slices
                    kc.EnableBananaSlices(true);
                    bananaEntered = false;
                    // send messages
                    handPalmTracker.SendDeactivateMessage(tearedBanana);
                    handPalmTracker.SendActivateMessage(kc.bananaSlices);
                    // disable board
                    handPalmTracker.SendDeactivateMessage(Stuff.Board);
                    // enable bowl
                    handPalmTracker.SendActivateMessage(Stuff.Bowl);
                }
            }
        }

        void EnableTearedBanana(bool enable)
        {
            tearedBanana.SetActive(enable);
        }
    }
}