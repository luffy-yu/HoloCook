//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

namespace HoloCook.HoloLens2
{
    public class InitializationHelper : MonoBehaviour
    {
        [Header("MainCamera")] public GameObject mainCamera;

        [Header("Main Menu Buttons")] public ButtonConfigHelper mButton1;
        public ButtonConfigHelper mButton4;
        public ButtonConfigHelper mButton7;
        public ButtonConfigHelper mButton2;
        public ButtonConfigHelper mButton5;
        public ButtonConfigHelper mButton8;
        public ButtonConfigHelper mButton3;
        public ButtonConfigHelper mButton6;
        public ButtonConfigHelper mButton9;

        [Header("Secondary Menu Buttons")] public ButtonConfigHelper sButton1;
        public ButtonConfigHelper sButton4;
        public ButtonConfigHelper sButton7;
        public ButtonConfigHelper sButton2;
        public ButtonConfigHelper sButton5;


        private void Start()
        {
        }
    }
}