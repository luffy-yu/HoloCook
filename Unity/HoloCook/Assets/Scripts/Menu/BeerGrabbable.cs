//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using HoloCook.HoloLens2;
using HoloCook.Utility;
using UnityEngine;

namespace HoloCook.Menu
{
    public class BeerGrabbable : CollisionDetector
    {
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
        //             // disable ice-related
        //             handPalmTracker.SendDeactivateMessage(Static.Stuff.Bowl);
        //         }
        //     }
        // }

        public override void AfterAttached()
        {
            // disable ice-related
            handPalmTracker.SendDeactivateMessage(Static.Stuff.Bowl);
        }
    }
}