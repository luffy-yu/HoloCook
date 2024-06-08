//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using HoloCook.HoloLens2;
using HoloCook.Utility;
using UnityEngine;
using Stuff = HoloCook.Menu.Static.Stuff;

namespace HoloCook.Menu
{
    public class SpoonGrabbable : CollisionDetector
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
        //             // deactivate board
        //             handPalmTracker.SendDeactivateMessage(Stuff.Board);
        //         }
        //     }
        // }

        public override void AfterAttached()
        {
            // deactivate board
            handPalmTracker.SendDeactivateMessage(Stuff.Board);
        }
    }
}
