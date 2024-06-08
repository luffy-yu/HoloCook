//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using HoloCook.HoloLens2;
using HoloCook.Utility;
using UnityEngine;

namespace HoloCook.Menu
{
    public class RumGrabbable : CollisionDetector
    {
        public PCController pCController;


        // public override bool CanGrab()
        // {
        //     return true;
        // }
        //
        // public override void TriggerEnterHandler(Collider other)
        // {
        //     var go = other.gameObject;
        //
        //     var name = go.name;
        //
        //     if (name.StartsWith(Utils.GetThumbJointName(handness)))
        //     {
        //         if (CanGrab() && handPalmTracker.CanAttach(handness))
        //         {
        //             // attach and follow hand
        //             handPalmTracker.AttachObject(gameObject, handness);
        //             handPalmTracker.collisionDetector = this;
        //
        //             // handle locally
        //             pCController.HandleSmallCup(false, true);
        //
        //             handPalmTracker.SendActivateMessage(Static.Stuff.SmallCup);
        //         }
        //     }
        // }

        public override void AfterAttached()
        {
            // handle locally
            pCController.HandleSmallCup(false, true);

            handPalmTracker.SendActivateMessage(Static.Stuff.SmallCup);
        }
    }
}