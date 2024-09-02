//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using Byter;
using HoloCook.Menu;
using HoloCook.Network;
using HoloCook.Utility;
// using Microsoft.MixedReality.Toolkit;
// using Microsoft.MixedReality.Toolkit.Input;
// using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using Stuff = HoloCook.Menu.Static.Stuff;

namespace HoloCook.HoloLens2
{
    public class HandPalmTracker : MonoBehaviour
    {
        [Header("Bowl")] public BowlTriggerArea bowlTrigger;
        [Header("Plate")] public PlateTriggerArea plateTrigger;
        [Header("BigCup")] public BigCupTriggerArea bigCupTrigger;
        [HideInInspector]public GameObject gameObject;

        [HideInInspector] public CollisionDetector collisionDetector;

        // rotation offset
        private float xOffset = 0;
        private float yOffset = -60;
        private float zOffset = 0;

        // hand cover object angle threshold
        [HideInInspector] public float cThreshold = 20;

        // whether available
        public bool available = false;

        public System.Action<string, Vector3, Vector3, Vector3> OnObjectWasReleased;

        // action recording status
        [HideInInspector] public bool recordingAction = false;
        [HideInInspector] public GameObject recordedGameObject; // last recorded gameobject

        // hand palm tracker
        private Transform _rightHand;
        private GameObject _attachedRight;

        private Transform _leftHand;
        private GameObject _attachedLeft;

        public GameObject Attached => _attachedRight; // right hand

        public System.Action<byte[]> MessageBridge;

        public System.Action<string> OnDetachObject;
        
        // last attaching time
        private float lastAttachingTime = 0;
        // minimum time span between attach and drop
        private float minimumTimeSpan = 3.0f;


        // Start is called before the first frame update
        void Start()
        {
            available = true;

            bowlTrigger.AnimationTriggered += OnAnimationTriggered;
            plateTrigger.AnimationTriggered += OnAnimationTriggered;
            bigCupTrigger.AnimationTriggered += OnAnimationTriggered;

            _rightHand = GetHandPalm(Handness.Right);
            _leftHand = GetHandPalm(Handness.Left);
        }

        private void OnAnimationTriggered(string name, Handness handness)
        {
            Utils.WriteLog($"OnAnimationTriggered: {name}");
            // hide that object locally and notify remote hololens to adjust
            DetachObject(false, handness);
        }

        // Update is called once per frame
        void Update()
        {
            // if (gameObject == null) return;
            // var t = GetHandPalm();
            // SetGameObjectTransform(gameObject, t);
        }

        // TODO: get hand palm
        // Get hand palm transform
        Transform GetHandPalm(Handness handedness)
        {
            Transform jointTransform = null;

            // var handJointService = CoreServices.GetInputSystemDataProvider<IMixedRealityHandJointService>();
            // if (handJointService != null)
            // {
            //     jointTransform = handJointService.RequestJointTransform(TrackedHandJoint.Palm, handedness);
            // }

            return jointTransform;
        }

        void SetGameObjectTransform(GameObject go, Transform t)
        {
            if (t == null) return;

            // get center
            Vector3 center = go.GetComponent<Renderer>().bounds.center;
            // get origin 
            Vector3 p = go.transform.position;
            // offset to center
            Vector3 pos = p - center + t.position;

            var rot = t.rotation * Quaternion.Euler(xOffset, yOffset, zOffset);

            go.transform.position = pos;
            go.transform.rotation = rot;
        }

        // object is released
        public void NotifyObjectReleasedEvent(string objname, Vector3 pos, Vector3 rot, Vector3 scale)
        {
            if (OnObjectWasReleased != null)
            {
                OnObjectWasReleased(objname, pos, rot, scale);
            }
        }

        #region Attach / detach object

        public GameObject GetAttached(Handness handness)
        {
            if (handness == Handness.Left)
                return _attachedLeft;
            return _attachedRight;
        }

        public (GameObject, GameObject) GetAttached()
        {
            return (_attachedLeft, _attachedRight);
        }

        public void AttachObject(GameObject obj, Handness handness)
        {
            if (handness == Handness.Right)
            {
                obj.transform.parent = _rightHand;
                _attachedRight = obj;
            }
            else
            {
                obj.transform.parent = _leftHand;
                _attachedLeft = obj;
            }
            // update time
            lastAttachingTime = Time.time;
            SendActivateMessage(obj);
        }

        public void DetachLocally(Handness handness)
        {
            if (handness == Handness.Right)
            {
                if (_attachedRight != null)
                {
                    _attachedRight.transform.parent = null;
                    _attachedRight = null;
                }
            }
            else
            {
                if (_attachedLeft != null)
                {
                    _attachedLeft.transform.parent = null;
                    _attachedLeft = null;
                }
            }
        }

        public void DisableForTrainee()
        {
            available = false;
        }

        public void DetachObject(bool enabled, Handness handness)
        {
            if (Time.time - lastAttachingTime < minimumTimeSpan)
            {
                // Utils.WriteLog("Dropping is too fast");
                return;
            }
            
            string name = null;
            
            if (handness == Handness.Right)
            {
                if (_attachedRight != null)
                {
                    name = _attachedRight.name;
                    SendDeactivateMessage(_attachedRight);

                    _attachedRight.transform.parent = null;
                    _attachedRight.SetActive(enabled);
                    _attachedRight = null;
                }
            }
            else
            {
                if (_attachedLeft != null)
                {
                    name = _attachedLeft.name;
                    SendDeactivateMessage(_attachedLeft);
                    _attachedLeft.transform.parent = null;
                    _attachedLeft.SetActive(enabled);
                    _attachedLeft = null;
                }
            }

            if (name != null && OnDetachObject != null)
            {
                // update locally
                OnDetachObject(name);
            }
        }

        public void ForceRelease()
        {
            if (_attachedLeft != null)
            {
                _attachedLeft.transform.parent = null;
                _attachedLeft = null;
            }

            if (_attachedRight != null)
            {
                _attachedRight.transform.parent = null;
                _attachedRight = null;
            }

            if (collisionDetector != null)
            {
                collisionDetector = null;
            }
        }

        public bool CanAttach(Handness handness)
        {
            // always return false when unavailable [trainee side]
            if (!available) return false;
            
            if (handness == Handness.Right)
            {
                return _attachedRight == null;
            }

            return _attachedLeft == null;
        }

        byte[] FormatMessage(GameObject obj, bool activate)
        {
            using Writer r = new Writer();
            r.Write(obj.name);
            r.Write(activate);
            
            return r.GetBytes();
        }
        
        byte[] FormatMessage(Stuff s, bool activate)
        {
            using Writer r = new Writer();
            r.Write(s.ToString());
            r.Write(activate);
            
            return r.GetBytes();
        }

        public void SendActivateMessage(GameObject obj)
        {
            var msg = FormatMessage(obj, true);
            SendMessageToBridge(msg);
        }

        public void SendActivateMessage(Stuff stuff)
        {
            var msg = FormatMessage(stuff, true);
            SendMessageToBridge(msg);
        }

        public void SendDeactivateMessage(GameObject obj)
        {
            var msg = FormatMessage(obj, false);
            SendMessageToBridge(msg);
        }

        public void SendDeactivateMessage(Stuff stuff)
        {
            var msg = FormatMessage(stuff, false);
            SendMessageToBridge(msg);
        }

        public void SendMessageToBridge(byte[] msg)
        {
            if (MessageBridge != null)
            {
                MessageBridge(msg);
            }
        }

        public void SendAnimatorMessage(string name)
        {
            using Writer r = new Writer();
            r.Write(name);
            r.Write(true);
            SendMessageToBridge(r.GetBytes());
        }

        #endregion
    }
}