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
    public class BigCupTriggerArea : MonoBehaviour
    {

        public PCController pCController;
        public System.Action<string, Handness> AnimationTriggered;

        public BowlGrabbable _bowlGrabbable;

        private GameObject _parent;

        private CollisionDetector _collisionDetector;

        private Handness _handness;
        private HandPalmTracker _handPalmTracker;

        private void Start()
        {
            _parent = transform.parent.gameObject;
            _collisionDetector = _parent.GetComponent<CollisionDetector>();
            _handness = _collisionDetector.handness;
            _handPalmTracker = _collisionDetector.handPalmTracker;
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

            if (name.Equals("SmallCup"))
            {
                var scg = go.GetComponent<SmallCupGrabbable>();
                scg.UpdatePourFlag();
            }
            else if (name.Equals("Beer"))
            {
                // TriggerAnimation(name, Handness.Right);
            }
            else if (name.Equals("Ice"))
            {
                // TriggerAnimation(name, Handness.Right);
                // restore it
                _bowlGrabbable.RestoreICE();

                pCController.ShowICEAnimation();
                // send animation
                _handPalmTracker.SendAnimatorMessage(NetlyEventTypes.AnimationICE);
                // disable it
                _handPalmTracker.SendDeactivateMessage(Static.Stuff.Ice);
            }
        }
    }
}