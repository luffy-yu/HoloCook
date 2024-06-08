//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using HoloCook.Utility;
using UnityEngine;
using UnityEngine.Events;

namespace HoloCook.HoloLens2
{
    public class VoiceCommandHandler : MonoBehaviour
    {
        public void HandleSpoon()
        {
            Utils.WriteLog("HandleSpoon");
        }

        public void HandleSelect()
        {
            Utils.WriteLog("HandleSelect");
        }
    }
}