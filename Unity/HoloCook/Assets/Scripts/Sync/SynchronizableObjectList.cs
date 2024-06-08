//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Byter;
using Ludiq;
using Netly;
using UnityEngine;

using HoloCook.HoloLens2;
using HoloCook.Network;

namespace HoloCook.Sync
{
    [Serializable, Inspectable]
    public class SynchronizableObjectList : MonoBehaviour
    {
        [HideInInspector] public float syncDelay = 0.1f;
        [HideInInspector] public bool running = false;


        [Header("Plane")] public int planeID;

        // public Transform origin;
        [HideInInspector] public bool ServerToClient;


        // origin gameobject on Netly Server part
        [Header("Origin")] public String originName = "Origin";
        public GameObject localOrigin;
        public GameObject remoteOrigin;

        [Inspectable] public SynchronizableObject[] objects;
        private float _delay;
        private float _time;

        private Dictionary<int, SynchronizableObject> objectDict;

        // internal object client { get; set; } = null;

        [HideInInspector] public UdpClient _client = null;

        #region Synchronization control

        // synchronization from PC (coach) to PC (trainee)
        [HideInInspector] public bool enablePC2PCSync = true;

        // synchronization from PC to HoloLens 2
        [HideInInspector] public bool enablePC2HLSync = true;


        #endregion

        private void Start()
        {
            _time = 0f;

            ServerToClient = false; // default mode

            objectDict = new Dictionary<int, SynchronizableObject>();

            if (objects != null)
            {
                foreach (var obj in objects)
                {
                    objectDict.Add(obj.id, obj);
                }
            }
        }

        public SynchronizableObject GetSynchronizableObjectByID(int id)
        {
            if (objectDict == null || !objectDict.ContainsKey(id)) return null;
            return objectDict[id];
        }

        public GameObject FindObjectByName(string n)
        {
            var so = FindSynchronizableObjectByName(n);
            if (so != null) return so.gameObject;
            return null;
        }

        public SynchronizableObject FindSynchronizableObjectByName(string n)
        {
            if (objects == null) return null;
            foreach (var obj in objects)
            {
                if (obj.gameObject.name == n) return obj;
            }

            return null;
        }

        public bool IsSynchronizableObjectGrabbed(SynchronizableObject so)
        {
            // return true if it's server to client mode
            if (ServerToClient) return true;

            CollisionDetector cd = null;
            if (so.gameObject.TryGetComponent<CollisionDetector>(out cd))
            {
                if (cd.status == GrabStatus.Grabbed) return true;
            }

            return false;
        }

        public SynchronizableObject FindSynchronizableObjectByID(int n)
        {
            if (objects == null) return null;
            foreach (var obj in objects)
            {
                if (obj.id == n) return obj;
            }

            return null;
        }

        public (int, string) GetIDNameOfObject(GameObject go)
        {
            if (objects == null) return (-1, "");
            foreach (var obj in objects)
            {
                if (obj.gameObject == go) return (obj.id, obj.gameObject.name);
            }

            return (-1, "");
        }

        private void SendObjectsTransformation()
        {
            if (!running) return;

            // return if not enabled
            if (!enablePC2HLSync) return;

            if (_client == null || (_client != null && !_client.IsOpened)) return;

            _time += Time.deltaTime;

            if (_time > _delay)
            {
                _time = 0f;

                foreach (var so in objects)
                {
                    // plane is always enabled, and the hand touched object is enabled
                    // var grabbed = IsSynchronizableObjectGrabbed(so);
                    // Debug.LogError($"Grabbed - {so.id} - {grabbed}");
                    // if (so.id == planeID || (so.enable && IsSynchronizableObjectGrabbed(so)))
                    // {
                    var p = so.gameObject.transform.position;
                    var r = so.gameObject.transform.rotation.eulerAngles;
                    var s = so.gameObject.transform.localScale;

                    using Writer w = new Writer();
                    w.Write(so.id);

                    w.Write(p.x);
                    w.Write(p.y);
                    w.Write(p.z);

                    w.Write(r.x);
                    w.Write(r.y);
                    w.Write(r.z);

                    w.Write(s.x);
                    w.Write(s.y);
                    w.Write(s.z);

                    _client.ToEvent(NetlyEventTypes.SyncTransform, w.GetBytes());
                }
            }
        }

        private void Update()
        {
            SendObjectsTransformation();
        }

        private void OnApplicationQuit()
        {
            // Close();
            if (_client != null && _client.IsOpened) _client.Close();
        }
    }
}