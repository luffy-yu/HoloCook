//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
// using Microsoft.MixedReality.Toolkit;
// using Microsoft.MixedReality.Toolkit.Input;
// using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

using HoloCook.Algorithm;
using HoloCook.Utility;

namespace HoloCook.HoloLens2
{
    public enum GrabStatus
    {
        Grabbed,
        Released,
        Unknown // default
    }

    public enum CapturingStatus
    {
        Required,
        Done,
        Unknown
    }

    public enum Handness
    {
        Left,
        Right,
    }

    public class CollisionDetector : MonoBehaviour
    {
        [Header("Hand Tracker")] public HandPalmTracker handPalmTracker;

        public bool grabbable = true;

        public Handness handness = Handness.Right;

        private float threshold = 130;

        private float cThreshold
        {
            get => handPalmTracker.cThreshold;
        }

        [HideInInspector] public GrabStatus status = GrabStatus.Unknown;
        [HideInInspector] public CapturingStatus cStatus = CapturingStatus.Unknown;

        [HideInInspector] public HashSet<string> enteredObjects = new HashSet<string>();

        private Quaternion initialRotation = Quaternion.identity;

        private GameObject fingersParent = null;

        private Transform cameraTransform = null;

        // last drop time stamp
        private long dropTimeStamp = 0;

        private long minTimeSpanms = 3000;


        [HideInInspector] public float planeY = 0.0f;

        // action recording
        private RecordObjectTransform transformRecorder;

        // backup before grabbing
        private Vector3 _backupPosition;
        private Quaternion _backupRotation;

        // Start is called before the first frame update
        void Start()
        {
            status = GrabStatus.Unknown;
            cStatus = CapturingStatus.Unknown;

            initialRotation = gameObject.transform.rotation;

            cameraTransform = Camera.main.transform;

            dropTimeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            // transformRecorder = GetComponent<RecordObjectTransform>();
        }

        // Update is called once per frame
        void FixedUpdate()
        {

        }

        public virtual bool CanGrab()
        {
            return grabbable;
        }

        public virtual void TriggerEnterHandler(Collider other)
        {
            var name = other.gameObject.name;
            
            // hand grab
            if (name.StartsWith(Utils.GetThumbJointName(handness)))
            {
                if (CanGrab() && handPalmTracker.CanAttach(handness))
                {
                    // backup
                    _backupPosition = transform.position;
                    _backupRotation = transform.rotation;

                    // attach and follow hand
                    handPalmTracker.AttachObject(gameObject, handness);
                    handPalmTracker.collisionDetector = this;
                    
                    AfterAttached();
                }
            }
        }

        public virtual void AfterAttached()
        {
            
        }

        private void OnTriggerEnter(Collider other)
        {
            TriggerEnterHandler(other);
        }

        private void OnTriggerExit(Collider other)
        {
            var name = other.gameObject.name;

            // hand grab
            if (name.StartsWith(handness.ToString()))
            {
                if (!handPalmTracker.CanAttach(handness))
                {
                    var go = handPalmTracker.GetAttached(handness);
                    enteredObjects.Add(go.name);
                }
            }
            else
            {
                enteredObjects.Add(name);
            }
        }

        public virtual void ReleaseFromHand()
        {
            // Utils.WriteLog($"Release {gameObject.name}");
            transform.rotation = _backupRotation;
            // update y only
            var pos = transform.position;
            pos.y = _backupPosition.y;
            transform.position = pos;
        }
    }
}