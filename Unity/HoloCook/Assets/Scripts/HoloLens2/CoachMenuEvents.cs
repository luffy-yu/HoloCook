//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

// #if !UNITY_WSA // not windows uwp
// using UnityEngine;
// public class CoachMenuEvents : MonoBehaviour
// {
//     
// }
// #else
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
//using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;
using Object = System.Object;
using HoloCook.Algorithm;
using HoloCook.Menu;
using HoloCook.Utility;
using HoloCook.Network;
using HoloCook.Sync;
using HoloCook.Static;
using SimulatedMenuAction = HoloCook.Network.NetlyEventTypes.SimulatedMenuAction;

namespace HoloCook.HoloLens2
{
    public class CoachMenuEvents : MonoBehaviour
    {
        [Space(30)] [Header("Panel Quad")] public GameObject panelQuad;

        [Header("Netly")] public NetlyClient netlyClient;

        [Header("Title Label")] public TextMeshPro titleLabel;

        [Header("IP Label")] public TextMeshPro ipLabel;

        // registration
        [Header("Registration")] public ImageSender imageSender;

        [Header("HandPalmTracker")] public HandPalmTracker handPalmTracker;
        public TextMeshPro toolStatusLabel;

        [Header("Camera Stream")] public GameObject cameraStream;
        [Header("PC Controller")] public PCController pCController;

        [Header("PlaneRef")] public GameObject planeRef;

        public GameObject pancakeRefRoot;
        public GameObject cocktailRefRoot;

        [Header("Bowl")] public BowlGrabbable bowlGrabbable;

        private ConcurrentQueue<Action> _mainThreadWorkQueue = new ConcurrentQueue<Action>();

        private string role = "";

        private string IP = "192.168.0.221";
        private int PORT = 8888;

        private LoginRole _role = LoginRole.Unknown;

        private bool tutoringStarted = false;

        #region Meta Quest Pro

        // adaption for Meta Quest Pro
        [Header("Meta Quest Pro")] [Tooltip("HandGrabInteractable (Left)")]
        public GameObject leftPlaneGrabbale;

        [Tooltip("HandGrabInteractable (Right)")]
        public GameObject rightPlaneGrabbale;

        void SetPlaneInteractable(bool enabled)
        {
            leftPlaneGrabbale.SetActive(enabled);
            rightPlaneGrabbale.SetActive(enabled);
        }
        #endregion

        private void Start()
        {
            InitializeFields();

            (IP, PORT) = Utils.LoadIPConfig();
            // update label and host
            ipLabel.text = $"Host: {IP}:{PORT}";

            // update tool status label
            SetToolStatusLabel(handPalmTracker.recordingAction);

            netlyClient.syncDirectionChanged += OnSyncDirectionChanged;
            netlyClient.ExecuteSimulation += ExecuteSimulation;
        }

        private void ExecuteSimulation(int action)
        {
            Utils.WriteLog($"ExecuteSimulation: {action}");
            SimulatedMenuAction sma = (SimulatedMenuAction)action;
            switch (sma)
            {
                case SimulatedMenuAction.Coach:
                    LoginAsCoach();
                    break;
                case SimulatedMenuAction.Cocktail:
                    MakeCocktail();
                    break;
                case SimulatedMenuAction.Lock:
                    DisablePlaneInteraction();
                    break;
                case SimulatedMenuAction.Pancake:
                    MakePancake();
                    break;
                case SimulatedMenuAction.Trainee:
                    LoginAsTrainee();
                    break;
                case SimulatedMenuAction.UnLock:
                    EnablePlaneInteraction();
                    break;
                case SimulatedMenuAction.Quit:
                    QuitApplication();
                    break;
                case SimulatedMenuAction.ShowUIs:
                    ShowUIs();
                    break;
                case SimulatedMenuAction.PC2HL:
                    netlyClient.RequestServerToClient(true);
                    break;
                case SimulatedMenuAction.HL2PC:
                    netlyClient.RequestClientToServer(true);
                    break;
                case SimulatedMenuAction.Debug:
                    VisualDebug();
                    break;
                case SimulatedMenuAction.Reset:
                    ResetStuff();
                    break;
                case SimulatedMenuAction.ToCocktail:
                    SwitchToCocktail();
                    break;
                default:
                    break;
            }
        }

        void ShowUIs()
        {
            pCController.SetUIs(true, !(_role != LoginRole.Trainee));
        }

        void SyncPancakeObjects()
        {
            var count = pancakeRefRoot.transform.childCount;
            for (var i = 0; i < count; i++)
            {
                var child = pancakeRefRoot.transform.GetChild(i);
                foreach (var obj in pCController.pancakeObjects)
                {
                    if (obj.name.Equals(child.gameObject.name))
                    {
                        // enable
                        obj.SetActive(true);
                        // enable meshrender
                        obj.GetComponentInChildren<MeshRenderer>(includeInactive: true).enabled = true;

                        obj.transform.position = child.position;
                        obj.transform.rotation = child.rotation;
                        break;
                    }
                }
            }
        }

        void SyncCocktailObjects()
        {
            var count = cocktailRefRoot.transform.childCount;
            for (var i = 0; i < count; i++)
            {
                var child = cocktailRefRoot.transform.GetChild(i);
                foreach (var obj in pCController.cocktailObjects)
                {
                    if (obj.name.Equals(child.gameObject.name))
                    {
                        // enable
                        obj.SetActive(true);
                        // enable meshrender
                        obj.GetComponentInChildren<MeshRenderer>(includeInactive: true).enabled = true;

                        obj.transform.position = child.position;
                        obj.transform.rotation = child.rotation;

                        #region Meta Quest Fix
                        // For some reason, it will trigger the collision especially ONLY for the small cup.
                        // Back up the transform, so it can be restored after collision with the plane.
                        
                        // for small cup
                        if (obj.name.Equals(Menu.Static.GetName(Menu.Static.Stuff.SmallCup)))
                        {
                            // backup
                            obj.GetComponent<SmallCupGrabbable>().BackupTransform();
                        }
                        #endregion
                        
                        break;
                    }
                }
            }
        }

        private void OnSyncDirectionChanged()
        {
#if UNITY_EDITOR
            Debug.LogError("OnSyncDirectionChanged");
#endif
            if (netlyClient.isServerToClient)
            {
                SetTitle(role, "PC->HL2");
            }
            else
            {
                SetTitle(role, "HL2->PC");
            }
        }

        public void EnablePlaneInteraction()
        {
            // var om = GetObjectManipulatorScript();
            // om.enabled = true;

            SetPlaneInteractable(true);

            _mainThreadWorkQueue.Enqueue(() =>
            {
                // specifically enable plane for trainee
                if (_role == LoginRole.Trainee)
                {
                    // this will ignore the planeRef
                    pCController.SetPlane(true);
                }

                planeRef.SetActive(true);

                // tutoringStarted to false
                tutoringStarted = false;
            });
        }

        public void DisablePlaneInteraction()
        {
            // var om = GetObjectManipulatorScript();
            // om.enabled = false;

            SetPlaneInteractable(false);

            _mainThreadWorkQueue.Enqueue(() => { planeRef.SetActive(false); });
        }

        public void EnableStreamingTransforms()
        {
            _mainThreadWorkQueue.Enqueue(() => { netlyClient.StartStreaming(); });
        }

        public void DisableStreamingTransforms()
        {
            _mainThreadWorkQueue.Enqueue(() => { netlyClient.StopStreaming(); });
        }

        public void SetClientToServer()
        {
            // only valid when role is set
            if (role == "") return;

            _mainThreadWorkQueue.Enqueue(() => { netlyClient.RequestClientToServer(true); });
        }

        public void SetServerToClient()
        {
            if (role == "") return;

            _mainThreadWorkQueue.Enqueue(() => { netlyClient.RequestServerToClient(true); });
        }

        private void SetTitle(string role, string syncdirection)
        {
            titleLabel.text = $"HoloCook - {role} - {syncdirection}";
        }


        public void LoginAsCoach()
        {
            _role = LoginRole.Coach;
            netlyClient.LoginAs(LoginRole.Coach);
            role = LoginRole.Coach.ToString();
            SetClientToServer();
        }

        public void LoginAsTrainee()
        {
            _role = LoginRole.Trainee;
            // disable handpalm tracker to disable grabble
            handPalmTracker.DisableForTrainee();

            netlyClient.LoginAs(LoginRole.Trainee);
            role = LoginRole.Trainee.ToString();
            SetClientToServer();
        }

        public void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit(0);
        }

        #region Initialize

        void InitializeFields()
        {
            panelQuad = GameObject.FindGameObjectWithTag(Tags.PlaneQuad);
            netlyClient = FindObjectOfType<NetlyClient>();
            titleLabel = GameObject.FindGameObjectWithTag(Tags.TitleLabel).GetComponent<TextMeshPro>();
            ipLabel = GameObject.FindGameObjectWithTag(Tags.IPLabel).GetComponent<TextMeshPro>();
            imageSender = FindObjectOfType<ImageSender>();
            handPalmTracker = FindObjectOfType<HandPalmTracker>();
            // toolStatusLabel = GameObject.FindGameObjectWithTag(Tags.StatusLabel).GetComponent<TextMeshPro>();
            cameraStream = GameObject.FindGameObjectWithTag(Tags.CameraStream);
            pCController = GameObject.FindGameObjectWithTag(Tags.PCController).GetComponent<PCController>();
        }

        #endregion

        #region Tools Menu

        public void RecordAction()
        {
            handPalmTracker.recordingAction = !handPalmTracker.recordingAction;
            SetToolStatusLabel(handPalmTracker.recordingAction);
        }

        void SetToolStatusLabel(bool b)
        {
            if (toolStatusLabel == null || cameraStream == null) return;

            toolStatusLabel.text = $"VideoSteam: {cameraStream.activeSelf} Recording: {b}";
        }

        public void ReplayAction()
        {
            if (handPalmTracker.recordedGameObject != null)
            {
                var go = handPalmTracker.recordedGameObject;
                var rot = go.GetComponent<RecordObjectTransform>();
                rot.PlayRecording();
            }
        }

        // test mapping for now
        public void EnableMapping()
        {
            if (handPalmTracker.recordedGameObject != null)
            {
                var go = handPalmTracker.recordedGameObject;
                var rot = go.GetComponent<RecordObjectTransform>();
                rot.PlayMapping();
            }
        }

        public void DisableMapping()
        {
        }

        public void SwitchVideoStream()
        {
            cameraStream.SetActive(!cameraStream.activeSelf);
            SetToolStatusLabel(handPalmTracker.recordingAction);
        }

        #endregion

        #region Menu

        public void VisualDebug()
        {
            _mainThreadWorkQueue.Enqueue(() =>
            {
                if (_role == LoginRole.Trainee)
                {
                    pCController.SwitchVisual(pCController.cocktailMode);
                }
            });
        }

        public void ResetStuff()
        {
            _mainThreadWorkQueue.Enqueue(() =>
            {
                if (pCController.cocktailMode)
                {
                    SyncCocktailObjects();
                }
                else
                {
                    SyncPancakeObjects();
                }
            });
        }

        public void SwitchToCocktail()
        {
            _mainThreadWorkQueue.Enqueue(() =>
            {
                // sync objects
                SyncCocktailObjects();

                pCController.cocktailMode = true;
                pCController.SetUIs(false, _role == LoginRole.Coach);
                pCController.SwitchToCocktail(_role == LoginRole.Trainee);
            });
        }

        void SwitchVisibility(bool cocktail, bool coach)
        {
            // coach will change the meshrender
            if (coach)
            {
                if (cocktail)
                {
                    pCController.SwitchCocktailVisibility();
                }
                else
                {
                    pCController.SwitchPancakeVisibility();
                }

                // switch plane visual
                pCController.SwitchPlaneVisibility();
            }
            else // trainee with change the activeness 
            {
                pCController.SwitchVisual(cocktail);
            }

            // hide menu at last
            pCController.SetMenus(false);
        }

        public void MakeCocktail()
        {
            _mainThreadWorkQueue.Enqueue(() =>
            {
                if (tutoringStarted && pCController.cocktailMode)
                {
                    SwitchVisibility(true, _role == LoginRole.Coach);
                    return;
                }

                // sync objects
                SyncCocktailObjects();

                // set plane visibility
                pCController.SetPlaneVisibility(true);

                // reset handpalmtracker
                handPalmTracker.ForceRelease();

                pCController.cocktailMode = true;
                pCController.SetUIs(false, _role == LoginRole.Coach);
                pCController.EnableCocktail(_role == LoginRole.Trainee);

                tutoringStarted = true;
            });
        }

        public void MakePancake()
        {
            _mainThreadWorkQueue.Enqueue(() =>
            {
                if (tutoringStarted && !pCController.cocktailMode)
                {
                    SwitchVisibility(false, _role == LoginRole.Coach);
                    return;
                }

                SyncPancakeObjects();

                // set plane visibility
                pCController.SetPlaneVisibility(true);

                // reset bowl
                if (bowlGrabbable != null)
                {
                    // reset bowl
                    bowlGrabbable.ResetEnteredObjects();
                }

                // reset handpalmtracker
                handPalmTracker.ForceRelease();

                pCController.cocktailMode = false;
                pCController.SetUIs(false, _role == LoginRole.Coach);
                pCController.EnablePancake(_role == LoginRole.Trainee);
                tutoringStarted = true;
            });
        }

        private void Update()
        {
            while (_mainThreadWorkQueue.TryDequeue(out Action workload))
            {
                workload();
            }
        }

        #endregion

        #region Debug

#if UNITY_EDITOR

        private void OnGUI()
        {
            if (GUI.Button(new Rect(300, 0, 100, 50), "Sync Pancake"))
            {
                SyncPancakeObjects();
            }

            if (GUI.Button(new Rect(300, 50, 100, 50), "Sync Cocktail"))
            {
                SyncCocktailObjects();
            }
        }

#endif

        void DebugTakePicture()
        {
            try
            {
                // TODO: for debug camera feature
                Dictionary<string, Object> nameValues = new Dictionary<string, Object>();

                // query object
                var obj = netlyClient.objectsList.FindSynchronizableObjectByID(4);

                if (obj != null)
                {
                    nameValues[UnityOpenGLUtils.key_objName] = obj.gameObject.name;
                    nameValues[UnityOpenGLUtils.key_objID] = $"{obj.id}";

                    var t = obj.gameObject.transform;
                    nameValues[UnityOpenGLUtils.key_position] = t.position;
                    nameValues[UnityOpenGLUtils.key_rotation] = t.rotation.eulerAngles;
                    nameValues[UnityOpenGLUtils.key_scale] = t.localScale;
                }

                // update additional
                imageSender.additionalNameValues = nameValues;

                imageSender.TakePicture();
            }
            catch (Exception e)
            {
                Utils.WriteLog(e.Message);
            }
        }



        #endregion
    }
}
// #endif