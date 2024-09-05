//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System.Collections;
using System.Collections.Generic;
using HoloCook.HoloLens2;
using HoloCook.Utility;
using UnityEngine;
using Stuff = HoloCook.Menu.Static.Stuff;

namespace HoloCook.Menu
{
    public class BowlGrabbable : CollisionDetector
    {
        public GameObject iceObject;

        public PCController pCController;

        private Vector3 icePosition;
        private Quaternion iceRotation;
        private bool iceBackuped = false;

        #region Meta Quest Adaption
        
        // // ice offset for better visualization, debug via NetlyEventTypes.Data
        // private Vector3 iceLocalPosition = new Vector3(-0.01f, -0.05f, 0.05f);

        // ice offset for better visualization, debug via UnityEditor (Nice)
        private Vector3 iceLocalPosition = new Vector3(-0.1f, -0.08f, 0f);
        
        #endregion

        public override bool CanGrab()
        {
            return enteredObjects.Contains(Static.GetName(Stuff.Whisk));
        }

        public void ResetEnteredObjects()
        {
            if (enteredObjects == null)
            {
                enteredObjects = new HashSet<string>();
            }

            enteredObjects.Clear();
        }

        public override void TriggerEnterHandler(Collider other)
        {
            var name = other.gameObject.name;

            // hand grab
            if (name.StartsWith(Utils.GetThumbJointName(handness)))
            {
                // can grab in pancake mode
                if (!pCController.cocktailMode && CanGrab() && handPalmTracker.CanAttach(handness))
                {
                    // attach and follow hand
                    handPalmTracker.AttachObject(gameObject, handness);
                    handPalmTracker.collisionDetector = this;
                    // enable pan
                    handPalmTracker.SendActivateMessage(Stuff.Pan);
                    return;
                }

                // cocktail mode
                if (pCController.cocktailMode && handPalmTracker.CanAttach(handness))
                {
                    // backup first
                    if (!iceBackuped)
                    {
                        icePosition = iceObject.transform.position;
                        iceRotation = iceObject.transform.rotation;
                        iceBackuped = true;
                    }
                    
                    // activate self
                    handPalmTracker.SendActivateMessage(Stuff.Bowl);

                    iceObject.SetActive(true);
                    // attach ice to it
                    handPalmTracker.AttachObject(iceObject, handness);
                    // update local position
                    iceObject.transform.localPosition = iceLocalPosition;
                    // enable big up
                    handPalmTracker.SendActivateMessage(Stuff.BigCup);
                }
            }
        }

        public void RestoreICE()
        {
            handPalmTracker.DetachLocally(handness);
            // restore position
            iceObject.transform.position = icePosition;
            iceObject.transform.rotation = iceRotation;
            // hide it
            iceObject.SetActive(false);
        }
    }
}