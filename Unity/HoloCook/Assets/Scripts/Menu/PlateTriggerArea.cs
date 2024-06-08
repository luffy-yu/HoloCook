//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System;
using HoloCook.HoloLens2;
using HoloCook.Network;
using HoloCook.Utility;
using UnityEngine;

namespace HoloCook.Menu
{
    public class PlateTriggerArea : MonoBehaviour
    {
        public PCController pCController;

        public Handness handness;

        public HandPalmTracker handPalmTracker;

        public System.Action<string, Handness> AnimationTriggered;

        private bool animated = false;

        private void Start()
        {
            animated = false;
        }

        void TriggerAnimation(string name, Handness handness)
        {
            if (AnimationTriggered != null)
            {
                AnimationTriggered(name, handness);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var go = other.gameObject;
            var name = go.name;

            if (name.StartsWith(Utils.GetThumbJointName(handness)))
            {
                go = handPalmTracker.GetAttached(handness);
                name = go.name;
            }

            if (!animated && name.Equals(Static.GetName(Static.Stuff.Pan)))
            {
                // update status to make it animate once 
                animated = true;

                // don't detach pan here
                // update pan canDrop flag
                go.GetComponent<PanGrabbable>().canDrop = true;

                pCController.ShowPancakeAnimation();
                handPalmTracker.SendAnimatorMessage(NetlyEventTypes.AnimationPlate);
                // handPalmTracker.DetachObject(false, handness);
            }
        }
    }
}