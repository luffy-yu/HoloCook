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
    public class BowlTriggerArea : MonoBehaviour
    {
        public PCController pCController;
        public System.Action<string, Handness> AnimationTriggered;

        private GameObject _parent;

        private Handness _handness;
        private BowlGrabbable _bowlGrabbable;
        private HandPalmTracker _handPalmTracker;

        private void Start()
        {
            _parent = transform.parent.gameObject;
            _bowlGrabbable = _parent.GetComponent<BowlGrabbable>();
            _handness = _bowlGrabbable.handness;
            _handPalmTracker = _bowlGrabbable.handPalmTracker;
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

            if (name.StartsWith(_handness.ToString()))
            {
                var attached = _handPalmTracker.GetAttached(_handness);
                if (attached != null)
                {
                    go = attached;
                    name = attached.name;
                }
            }

            if (name.Equals("Egg1"))
            {
                // detach locally
                _handPalmTracker.DetachLocally(Handness.Right);
                go.SetActive(false);
                // this will detach automatically
                TriggerAnimation(name, Handness.Right);
                // detach egg1 on local hololens, notify remote hololens2 to hide egg1 mesh
                pCController.ShowEggAnimation(1);
                // send animation
                _handPalmTracker.SendAnimatorMessage(NetlyEventTypes.AnimationEgg1);
                _handPalmTracker.SendDeactivateMessage(Static.Stuff.Egg1);
            }
            else if (name.Equals("Egg2"))
            {
                // detach locally
                _handPalmTracker.DetachLocally(Handness.Right);
                go.SetActive(false);

                TriggerAnimation(name, Handness.Right);
                pCController.ShowEggAnimation(2);
                _handPalmTracker.SendAnimatorMessage(NetlyEventTypes.AnimationEgg2);
                _handPalmTracker.SendDeactivateMessage(Static.Stuff.Egg2);
            }
            else if (name.Equals("Knife"))
            {
                // hide banana slices
                var kc = go.GetComponent<KnifeController>();
                kc.EnableBananaSlices(false);
                // disable remotely
                _handPalmTracker.SendDeactivateMessage(Static.Stuff.BananaSlices);
                // make it detached when reaching the plane
                kc.canDisappear = true;
                pCController.ShowBananaAnimation();
                _handPalmTracker.SendAnimatorMessage(NetlyEventTypes.AnimationBanana);
                // disable the board
                _handPalmTracker.SendDeactivateMessage(Static.Stuff.Board);
            }
        }
    }
}