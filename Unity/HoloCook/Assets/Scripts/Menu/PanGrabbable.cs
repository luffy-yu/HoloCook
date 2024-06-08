//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//


using HoloCook.HoloLens2;
using UnityEngine;

namespace HoloCook.Menu
{
    public class PanGrabbable : CollisionDetector
    {
        // pan can only drop after pancake animation is showed
        [HideInInspector] public bool canDrop = false;

        public override bool CanGrab()
        {
            var right = handPalmTracker.GetAttached(Handness.Right);

            if (right != null)
            {
                // name must be pan
                var name = Static.GetName(Static.Stuff.Turner);
                return grabbable && name.Equals(right.name);
            }

            return false;
        }
    }
}