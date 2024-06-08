//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

namespace HoloCook.Network
{
    public class NetlyEventTypes
    {
        public static readonly string RequestUdpHost = "RequestUdpHost";
        public static readonly string ReplyUdpHost = "ReplyUdpHost";
        public static readonly string SyncTransform = "SyncTransform";
        public static readonly string ChangeDirection = "ChangeDirection";

        public static readonly string LoginRole = "LoginRole";

        // registration
        public static readonly string Registration = "Registration";

        public static readonly string SyncRegistration = "SyncRegistration";

        // actions
        public static readonly string FieldSep = ",";
        public static readonly string Action = "Action";
        public static readonly string ActionCapture = "ActionCapture";
        public static readonly string ActionQuitApp = "ActionQuitApp";
        public static readonly string ActionStart = "ActionStart";
        public static readonly string ActionPC2HL = "ActionPC2HL2";
        public static readonly string ActionHL2PC = "ActionHL2PC";
        public static readonly string ActionThreshold = "ActionThreshold";
        public static readonly string ActionSwitchPlane = "ActionSwitchPlane";
        public static readonly string ActionSwitchAll = "ActionSwitchAll";
        public static readonly string ActionSwitchOne = "ActionSwitchOne";
        public static readonly string ActionEnablePancake = "ActionEnablePancake";
        public static readonly string ActionEnableCocktail = "ActionShowCocktail";
        public static readonly string ActionSwitchPancake = "ActionSwitchPancake";
        public static readonly string ActionReleaseHand = "ActionReleaseHand";

        public static readonly string ActionSwitchCocktail = "ActionSwitchCocktail";

        // show menu, 2nd menu, and the plane
        public static readonly string ActionShowUIs = "ActionShowUIs";

        // record & replay
        public static readonly string ActionRecord = "ActionRecord";

        public static readonly string ActionReplay = "ActionReplay";

        // hololens 2 message
        public static readonly string MsgHL2Header = "MsgHL2Header";

        // animation
        public static readonly string AnimationPrefix = "Animation";
        public static readonly string AnimationEgg1 = "AnimationEgg1";
        public static readonly string AnimationEgg2 = "AnimationEgg2";
        public static readonly string AnimationBanana = "AnimationBanana";
        public static readonly string AnimationICE = "AnimationICE";
        public static readonly string AnimationPlate = "AnimationPlate";

        // offset
        public static readonly string Data = "Data";
        public static readonly string ICEOffset = "ICEOffset";

        // simulated menu actions
        public static readonly string ActionSimulation = "ActionSimulation";

        public enum SimulatedMenuAction
        {
            Pancake,
            Cocktail,
            Lock,
            UnLock,
            Coach,
            Trainee,
            Quit,
            ShowUIs,
            // action without networking
            HL2PC,
            PC2HL,
            // debug for trainee
            Debug,
            Reset,
            // to
            ToCocktail,
            // animation without networking
            Banana, // show banana on board
            BananaSlices, // show banana slices on knife
            Egg1, // show egg animation 1
            Egg2, // show egg animation 2
            Cake, // show pancake dropping animation
            Lime, // show label above small cup
            Rum, // show label above small cup
            ICE, // show ice dropping above big cup
        }
    }
}
