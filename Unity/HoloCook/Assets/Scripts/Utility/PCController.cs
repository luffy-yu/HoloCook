//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
#if !UNITY_WSA // not windows uwp
using Fusion;
#endif
using HoloCook.HoloLens2;
using HoloCook.Menu;
using HoloCook.Network;
using UnityEngine;
using SimulatedMenuAction = HoloCook.Network.NetlyEventTypes.SimulatedMenuAction;

namespace HoloCook.Utility
{
    public class PCController : MonoBehaviour
    {
        [Header("Netly")] public NetlyServer server;
        [Header("Visibility Control")] public string objectName = null;

        // [Header("Plane")] public GameObject plane;

        [Header("Pancake")] public List<GameObject> pancakeObjects;
        public GameObject tearedBanana;
        public GameObject slicedBanana;

        [Header("Bowl Related Animation")] public GameObject bowlGameObject;
        public Animator eggAnimator;
        public Animator bananaAnimator;

        [Header("Plate Related Animation")] public GameObject plateGameObject;
        public Animator pancakeAnimator;

        [Header("ICE Related Animation")] public GameObject bigCupGameObject;
        public Animator iceAnimator;

        private bool egg1AnimShowed = false;
        private bool egg2AnimShowed = false;
        private bool bananaAnimShowed = false;
        private bool pancakeAnimShowed = false;


        [Header("Cocktail")] public List<GameObject> cocktailObjects;

        [Header("UI")] public GameObject mainMenu;
        public GameObject secondMenu;
        public GameObject plane;
        public GameObject planeRef;

        // enable small cup and related text
        [Header("Cocktail Interaction")] public HandPalmTracker handPlamTracker;
        public GameObject smallCup;
        public MeshRenderer smallCupRender;
        public GameObject limeJuice;
        public GameObject rum;
        public GameObject ice;

        [HideInInspector] public bool cocktailMode = false;


        [Header("Plane Visual")] public List<MeshRenderer> planeVisuals;

        private bool debugUI = false;

        #region Multiplayer control

        private BasicSpawner _spawner;

        [HideInInspector]
        public BasicSpawner spawner
        {
            get
            {
                if (_spawner == null)
                {
                    _spawner = FindObjectOfType<BasicSpawner>();
                }

                return _spawner;
            }
        }

        #endregion

        private void Start()
        {
            if (handPlamTracker != null)
            {
                handPlamTracker.OnDetachObject += OnDetachObject;
            }
        }

        // local process
        private void OnDetachObject(string name)
        {
            // special logic
            if (name.Equals(limeJuice.name) || name.Equals(rum.name))
            {
                // disable the text
                smallCup.GetComponent<SmallCupGrabbable>().DisableText();
            }
            // // TODO: check
            // else if (name.Equals(smallCup.name))
            // {
            //     var scg = smallCup.GetComponent<SmallCupGrabbable>();
            //     var flag = scg.CanDisappear();
            //     // local process
            //     smallCup.SetActive(!flag);
            //     smallCupRender.enabled = !flag;
            // }
        }

        #region Control

        public void SetPlane(bool enable)
        {
            // ignore lights
            var count = plane.transform.childCount;
            for (var i = 0; i < count; i++)
            {
                var child = plane.transform.GetChild(i).gameObject;
                if (child.name.EndsWith("Light")) continue;
                if (child.Equals(planeRef)) continue;

                child.SetActive(enable);
            }
        }

        public void SetMenus(bool enable)
        {
            if (mainMenu != null)
            {
                mainMenu.SetActive(enable);
                secondMenu.SetActive(enable);
            }
        }

        public void SetUIs(bool enable, bool coach)
        {
            SetMenus(enable);
            if (!coach)
            {
                SetPlane(enable);
            }
            else
            {
                // always enabled for coach
                SetPlane(true);
            }
        }

        void SendAction(string data)
        {
            if (server == null) return;
            server.SendAction(data);
        }

        void SendSimulationAction(SimulatedMenuAction action)
        {
            if (server == null) return;
            server.SendSimulationAction(action);
        }

        public void SwitchAll()
        {
            Switch(true, true);
        }

        void Switch(bool pancake, bool cocktail)
        {
            var f = pancakeObjects[0].activeSelf;
            // set local
            if (pancake)
            {
                ActivatePancake(!f);
            }

            if (cocktail)
            {
                ActivateCocktail(!f);
            }

            // notify remotely
            SendAction(NetlyEventTypes.ActionSwitchAll);
        }

        public void SwitchPlane()
        {
            var f = plane.activeSelf;
            SetPlane(!f);
            SendAction(NetlyEventTypes.ActionSwitchPlane);
        }

        public void SwitchOne(string name)
        {
            bool flag = false;
            foreach (var obj in pancakeObjects)
            {
                if (obj.name.Equals(name))
                {
                    obj.SetActive(!obj.activeSelf);
                    flag = true;
                    break;
                }
            }

            if (!flag)
            {
                foreach (var obj in cocktailObjects)
                {
                    if (obj.name.Equals(name))
                    {
                        obj.SetActive(!obj.activeSelf);
                        break;
                    }
                }
            }

            var data = $"{NetlyEventTypes.ActionSwitchPlane}{NetlyEventTypes.FieldSep}{objectName}";
            SendAction(data);
        }

        void ActivateCocktail(bool enabled)
        {
            foreach (var obj in cocktailObjects)
            {
                obj.SetActive(enabled);
            }
        }

        public void SwitchVisual(bool cocktail)
        {
            if (cocktail)
            {
                var flag = cocktailObjects[0].activeSelf;
                ActivateCocktail(!flag);
                // update plane
                SetPlane(!flag);
            }
            else
            {
                var flag = pancakeObjects[0].activeSelf;
                ActivatePancake(!flag);
                // update plane
                SetPlane(!flag);
            }
        }

        void ActivatePancake(bool enabled)
        {
            foreach (var obj in pancakeObjects)
            {
                obj.SetActive(enabled);
            }
        }

        void SetVisibility(List<GameObject> objs, bool enabled)
        {
            foreach (var obj in objs)
            {
                var mr = obj.GetComponentInChildren<MeshRenderer>(includeInactive: true);
                mr.enabled = enabled;
            }
        }

        void SwitchVisibility(List<GameObject> objs)
        {
            bool enabled = objs[0].GetComponentInChildren<MeshRenderer>(includeInactive: true).enabled;
            SetVisibility(objs, !enabled);
        }

        public void SwitchPancakeVisibility()
        {
            SwitchVisibility(pancakeObjects);
        }

        public void SwitchCocktailVisibility()
        {
            SwitchVisibility(cocktailObjects);
        }

        public void SwitchPlaneVisibility()
        {
            if (planeVisuals == null) return;
            var flag = planeVisuals[0].enabled;
            SetPlaneVisibility(!flag);
        }

        public void SetPlaneVisibility(bool flag)
        {
            foreach (var mr in planeVisuals)
            {
                mr.enabled = flag;
            }
        }

        public void SetVisibility(bool cocktail, bool pancake)
        {
            SetVisibility(pancakeObjects, pancake);
            SetVisibility(cocktailObjects, cocktail);
        }

        #region Small cup interaction

        GameObject GetHandHeldObject()
        {
            // loop left and right
            var (left, right) = handPlamTracker.GetAttached();

            if (left == null) return right;

            return left;
        }

        public void HandleSmallCup(bool lime, bool enable)
        {
            // enable small cup to synchronize transform
            smallCup.SetActive(true);
            var scg = smallCup.GetComponent<SmallCupGrabbable>();
            if (enable)
            {
                scg.ShowTextFor(lime);
            }
            else
            {
                // disable text
                scg.DisableText();
            }
        }


        #endregion

        #region Animated interactions

        // teared banana is on the board, enable board also
        void SetTearedBanana(bool enable)
        {
            tearedBanana.SetActive(enable);
            if (enable)
            {
                var parent = tearedBanana.transform.parent.gameObject;
                if (!parent.activeSelf)
                {
                    parent.SetActive(true);
                }
            }
        }

        void SetSmallCup(bool enable)
        {
            smallCup.SetActive(enable);
        }



        #endregion

        public void ProcessData(string name, float x, float y, float z)
        {
            if (name == NetlyEventTypes.ICEOffset)
            {
                var pos = new Vector3(x, y, z);
                ice.transform.localPosition = pos;
            }
        }

        public void ActivateObject(string name, bool enable)
        {
            if (name.StartsWith(NetlyEventTypes.AnimationPrefix))
            {
                ActivateAnimation(name);
                return;
            }

            if (name.Equals(smallCup.name))
            {
                SetSmallCup(enable);
                return;
            }

            if (name.Equals(tearedBanana.name))
            {
                SetTearedBanana(enable);
                return;
            }

            if (name.Equals(slicedBanana.name))
            {
                slicedBanana.SetActive(enable);
            }

            if (name.Equals(limeJuice.name))
            {
                // extra process for disabling lime juice / rum 
                // from remote, show cup render and text
                HandleSmallCup(true, enable);
            }

            if (name.Equals(rum.name))
            {
                HandleSmallCup(false, enable);
            }


            foreach (var obj in pancakeObjects)
            {
                if (obj.name.Equals(name))
                {
                    obj.SetActive(enable);
                    break;
                }
            }

            foreach (var obj in cocktailObjects)
            {
                if (obj.name.Equals(name))
                {
                    obj.SetActive(enable);
                    break;
                }
            }
        }

        public void ActivateAnimation(string name)
        {
            Utils.WriteLog($"ActivateAnimation {name}");
            if (name.Equals(NetlyEventTypes.AnimationEgg1))
            {
                ShowEggAnimation(1);
            }
            else if (name.Equals(NetlyEventTypes.AnimationEgg2))
            {
                ShowEggAnimation(2);
            }
            else if (name.Equals(NetlyEventTypes.AnimationBanana))
            {
                ShowBananaAnimation();
            }
            else if (name.Equals(NetlyEventTypes.AnimationPlate))
            {
                ShowPancakeAnimation();
            }
            else if (name.Equals(NetlyEventTypes.AnimationICE))
            {
                ShowICEAnimation();
            }
        }

        #region Handler of remote message

        public bool HandleSimulatedAction(int action)
        {
            Utils.WriteLog($"HandleSimulatedAction: {action}");
            SimulatedMenuAction sma = (SimulatedMenuAction)action;

            bool handled = true;

            switch (sma)
            {
                case SimulatedMenuAction.Banana:
                    break;
                case SimulatedMenuAction.BananaSlices:
                    break;
                case SimulatedMenuAction.Egg1:
                    break;
                case SimulatedMenuAction.Egg2:
                    break;
                case SimulatedMenuAction.Cake:
                    break;
                case SimulatedMenuAction.Lime:
                    break;
                case SimulatedMenuAction.Rum:
                    break;
                case SimulatedMenuAction.ICE:
                    break;
                default:
                    handled = false;
                    break;
            }

            return handled;
        }

        #endregion

        public void EnablePancake(bool trainee)
        {
            // trainee will hide all
            ActivateCocktail(false);
            ActivatePancake(!trainee);
            ResetAnimation();
            SendAction(NetlyEventTypes.ActionEnablePancake);
        }

        public void SwitchToCocktail(bool trainee)
        {
            // trainee will hide all
            ActivatePancake(false);
            ActivateCocktail(!trainee);
        }

        public void EnableCocktail(bool trainee)
        {
            // trainee will hide all
            ActivatePancake(false);
            ActivateCocktail(!trainee);

            ResetAnimation();
            SendAction(NetlyEventTypes.ActionEnableCocktail);
        }

        public void SwitchPancake()
        {
            if (server == null)
            {
                SwitchVisibility(pancakeObjects);
            }

            ResetAnimation();
            SendAction(NetlyEventTypes.ActionSwitchPancake);
        }

        public void SwitchCocktail()
        {
            if (server == null)
            {
                SwitchVisibility(cocktailObjects);
            }

            SendAction(NetlyEventTypes.ActionSwitchCocktail);
        }

        void PrepareAnimation(GameObject go)
        {
            // enable parent
            if (!go.activeSelf)
            {
                go.SetActive(true);
            }
        }

        void PrepareBowlAnimations()
        {
            PrepareAnimation(bowlGameObject);
        }

        void PreparePlateAnimation()
        {
            PrepareAnimation(plateGameObject);
        }

        void PrepareICEAnimation()
        {
            PrepareAnimation(bigCupGameObject);
        }

        public void ShowEggAnimation(int stage)
        {
            PrepareBowlAnimations();

            if (stage == 1)
            {
                // if (egg1AnimShowed) return;

                // egg1AnimShowed = true;
                eggAnimator.SetTrigger("Start");
            }
            else if (stage == 2)
            {
                // if (egg2AnimShowed) return;

                // egg2AnimShowed = true;
                eggAnimator.SetTrigger("Start2");
            }
        }

        public void ShowBananaAnimation()
        {
            PrepareBowlAnimations();

            bananaAnimator.SetTrigger("Start");
        }

        public void ShowPancakeAnimation()
        {
            PreparePlateAnimation();
            pancakeAnimator.SetTrigger("Start");
        }

        public void ShowICEAnimation()
        {
            PrepareICEAnimation();
            iceAnimator.SetTrigger("Start");
        }

        void ResetAnimation()
        {
            egg1AnimShowed = false;
            egg2AnimShowed = false;
            bananaAnimShowed = false;
            pancakeAnimShowed = false;
        }

        public void ShowUIs()
        {
            SetUIs(true, true);
            SendAction(NetlyEventTypes.ActionShowUIs);
        }

        public void ReleaseHand()
        {
            SendAction(NetlyEventTypes.ActionReleaseHand);
        }

        public void StartPancake()
        {
            SendSimulationAction(SimulatedMenuAction.Pancake);
        }

        public void StartCocktail()
        {
            SendSimulationAction(SimulatedMenuAction.Cocktail);
        }

        #endregion

#if UNITY_EDITOR || !UNITY_WSA
        private void OnGUI()
        {
            var style = Utils.GetLabelStyle();
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();

            if (GUILayout.Button("Switch Debug"))
            {
                debugUI = !debugUI;
            }

            if (!debugUI)
            {
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                return;
            }

            if (GUILayout.Button("Show all"))
            {
                // show/hide all models
                Switch(true, true);
            }

            if (GUILayout.Button("Show/hide plane"))
            {
                // show/hide plane
                SwitchPlane();
            }


            if (objectName != null)
            {
                if (GUILayout.Button($"Show/hide {objectName}"))
                {
                    // show/hide {objectName}
                    SwitchOne(objectName);
                }
            }

            if (GUILayout.Button("Enable Pancake"))
            {
                // enable pancake
                EnablePancake(false);
            }

            if (GUILayout.Button("Switch Pancake"))
            {
                // switch pancake
                SwitchPancake();
            }

            if (GUILayout.Button("Enable Cocktail"))
            {
                // enable cocktail
                EnableCocktail(false);
            }

            if (GUILayout.Button("Switch Cocktail"))
            {
                // show cocktail
                SwitchCocktail();
            }

            if (GUILayout.Button("Egg Animation 1"))
            {
                // show egg animation
                ShowEggAnimation(1);
            }

            if (GUILayout.Button("Egg Animation 2"))
            {
                // show egg animation
                ShowEggAnimation(2);
            }

            if (GUILayout.Button("Banana Animation"))
            {
                ShowBananaAnimation();
            }

            if (GUILayout.Button("Pancake Animation"))
            {
                ShowPancakeAnimation();
            }

            if (GUILayout.Button("ICE Animation"))
            {
                ShowICEAnimation();
            }

            if (GUILayout.Button("Show UIs"))
            {
                ShowUIs();
            }

            if (GUILayout.Button("Show Menus"))
            {
                SetMenus(true);
            }

            GUILayout.EndVertical();

            // 2nd column
            GUILayout.BeginVertical();
            GUILayout.Label("HL2", style);

            if (GUILayout.Button("Lock"))
            {
                SendSimulationAction(SimulatedMenuAction.Lock);
            }

            if (GUILayout.Button("Unlock"))
            {
                SendSimulationAction(SimulatedMenuAction.UnLock);
            }

            if (GUILayout.Button("Coach"))
            {
                SendSimulationAction(SimulatedMenuAction.Coach);
            }

            if (GUILayout.Button("Trainee"))
            {
                SendSimulationAction(SimulatedMenuAction.Trainee);
            }

            if (GUILayout.Button("Pancake"))
            {
                SendSimulationAction(SimulatedMenuAction.Pancake);
            }

            if (GUILayout.Button("Cocktail"))
            {
                SendSimulationAction(SimulatedMenuAction.Cocktail);
            }

            if (GUILayout.Button("Show UIs"))
            {
                SendSimulationAction(SimulatedMenuAction.ShowUIs);
            }

            if (GUILayout.Button("Debug"))
            {
                SendSimulationAction(SimulatedMenuAction.Debug);
            }

            if (GUILayout.Button("Reset"))
            {
                SendSimulationAction(SimulatedMenuAction.Reset);
            }

            if (GUILayout.Button("To Cocktail"))
            {
                SendSimulationAction(SimulatedMenuAction.ToCocktail);
            }

            if (GUILayout.Button("Release Hand"))
            {
                ReleaseHand();
            }

            if (GUILayout.Button("Quit"))
            {
                SendSimulationAction(SimulatedMenuAction.Quit);
            }

            GUILayout.EndVertical();

            // 3nd column
            GUILayout.BeginVertical();
            GUILayout.Label("Local", style);
            if (GUILayout.Button("HL2 -> PC"))
            {
                SendSimulationAction(SimulatedMenuAction.HL2PC);
            }

            if (GUILayout.Button("PC -> HL2"))
            {
                SendSimulationAction(SimulatedMenuAction.PC2HL);
            }

            GUILayout.EndVertical();

            // 4th column
            GUILayout.BeginVertical();

#if !UNITY_WSA // not windows uwp, server is null in HL2 mode
            GUILayout.Label("Animations", style);

            if (GUILayout.Button("Knife"))
            {
                // SendSimulationAction(SimulatedMenuAction.Banana);
                server.SendSimulatedCoach("Knife", true);
            }

            if (GUILayout.Button("Banana"))
            {
                // SendSimulationAction(SimulatedMenuAction.Banana);
                server.SendSimulatedCoach("TearedBanana", true);
            }

            if (GUILayout.Button("Banana 2"))
            {
                // SendSimulationAction(SimulatedMenuAction.Banana);
                server.SendSimulatedCoach("TearedBanana", false);
            }

            if (GUILayout.Button("Banana Slices"))
            {
                // SendSimulationAction(SimulatedMenuAction.BananaSlices);
                server.SendSimulatedCoach("BananaSlices", true);
            }

            if (GUILayout.Button("Egg 1"))
            {
                // SendSimulationAction(SimulatedMenuAction.Egg1);
                server.SendSimulatedCoach("AnimationEgg1", true);
            }

            if (GUILayout.Button("Egg 2"))
            {
                // SendSimulationAction(SimulatedMenuAction.Egg2);
                server.SendSimulatedCoach("AnimationEgg2", true);
            }

            if (GUILayout.Button("Banana Dropping"))
            {
                server.SendSimulatedCoach("AnimationBanana", true);
            }

            if (GUILayout.Button("Cake"))
            {
                // SendSimulationAction(SimulatedMenuAction.Cake);
                server.SendSimulatedCoach("AnimationPlate", true);
            }

            if (GUILayout.Button("Lime"))
            {
                // SendSimulationAction(SimulatedMenuAction.Lime);
                server.SendSimulatedCoach("LimeJuice", true);
            }

            if (GUILayout.Button("Rum"))
            {
                // SendSimulationAction(SimulatedMenuAction.Rum);
                server.SendSimulatedCoach("Rum", true);
            }

            if (GUILayout.Button("ICE"))
            {
                // SendSimulationAction(SimulatedMenuAction.ICE);
                server.SendSimulatedCoach("AnimationICE", true);
            }

            GUILayout.EndVertical();

            // 5th column
            // control from NetlyServer
            GUILayout.BeginVertical();
            GUILayout.Label("Sync", style);

            var (p2p, p2h, h2p) = server.GetSyncStatus();

            if (GUILayout.Button($"H2P: {h2p}"))
            {
                server.SetSyncStatus(p2p, p2h, !h2p);
            }

            if (GUILayout.Button($"P2H: {p2h}"))
            {
                server.SetSyncStatus(p2p, !p2h, h2p);
            }

            if (GUILayout.Button($"P2P: {p2p}"))
            {
                server.SetSyncStatus(!p2p, p2h, h2p);
            }

            GUILayout.EndVertical();

            // 6th column
            GUILayout.BeginVertical();

            GUILayout.Label("Info", style);
            var text = server.GetDebugInfo();
            var labelStyle = server.GetLabelStyle();
            GUILayout.Label(text, labelStyle);

            if (spawner != null && spawner.Started())
            {
                GUILayout.Label(spawner.GetDebugInfo());
            }
#endif

            GUILayout.EndVertical();

            // // 7 th column
            // GUILayout.BeginVertical();
            // GUILayout.Label("Tutoring", style);
            // if (!spawner.Started())
            // {
            //
            //     if (GUILayout.Button($"Host"))
            //     {
            //         spawner.StartGame(GameMode.Host);
            //     }
            //
            //     if (GUILayout.Button($"Join"))
            //     {
            //         spawner.StartGame(GameMode.Client);
            //     }
            // }
            // else
            // {
            //     GUILayout.Label(spawner.GetDebugInfo());
            // }

            // GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }
#endif
    }
}