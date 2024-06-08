//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using HoloCook.HoloLens2;

namespace HoloCook.Menu
{
    public class OilGrabbable : CollisionDetector
    {
        public override void AfterAttached()
        {
            // enable pan
            handPalmTracker.SendActivateMessage(Static.Stuff.Pan);
        }
    }
}