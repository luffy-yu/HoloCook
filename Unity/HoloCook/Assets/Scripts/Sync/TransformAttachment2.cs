//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System;
using UnityEngine;

using HoloCook.Algorithm;
using HoloCook.Network;

namespace HoloCook.Sync
{
    public class TransformAttachment2 : MonoBehaviour
    {
        private SynchronizableObjectList _syncList;

        // [Header("Coordinate Transform")]
        public SynchronizableObjectList syncList
        {
            get
            {
                if (_syncList == null)
                {
                    _syncList = FindObjectOfType<SynchronizableObjectList>();
                }

                return _syncList;
            }
        }

        // [Header("Transform")] public Transform transform;
        [Header("Source Object")] public GameObject source;

        [Header("Setting")] public bool enablePosition = true;

        public bool enableRotation = true;

        private bool enableScale = false;


        // remote origin is set
        private bool remoteOriginSet = false;

        private GameObject localOrigin;
        private GameObject remoteOrigin;

        #region Action recording/mapping

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

        [HideInInspector]
        public bool enableMapping
        {
#if UNITY_WSA // HoloLens build, fix compilation error
            get => true;
#else
       get => spawner.enableMapping;
#endif
        }

        private bool enableSync = true;

        [HideInInspector] public Vector3 mappedStart;
        [HideInInspector] public Vector3 mappedEnd;
        [HideInInspector] public GameObjectRecorder recorder;

        #endregion

        #region ActionRecord

        public void EnableSync(bool enable)
        {
            enableSync = enable;
        }
        

        #endregion

        void FixedUpdate()
        {
            if (source == null || !enableSync) return;
            
            // return if pc to pc synchronization is disabled
            if(!syncList.enablePC2PCSync) return;

            if (!remoteOriginSet && syncList.remoteOrigin != null)
            {
                localOrigin = syncList.localOrigin;
                remoteOrigin = syncList.remoteOrigin;

                remoteOriginSet = true;
                
                #if UNITY_EDITOR
                            Debug.LogError(
                                $"local origin: {localOrigin.transform.position.ToString()} remote origin: {remoteOrigin.transform.position.ToString()}");
                #endif
            }
            
            var t = source.transform;

            var p = t.position;
            var r = t.rotation.eulerAngles;
            var s = t.localScale;

            // transform coordinate system only when the mapping is disabled
            if (!enableMapping)
            {
                var p1 = Vector3.zero;
                var r1 = Vector3.zero;
                var s1 = Vector3.zero;
                var flag = TransformCoordinateSystems(localOrigin, remoteOrigin, t, out p1, out r1, out s1);
                if (flag)
                {
                    p = p1;
                    r = r1;
                    s = s1;
                }
            }

            if (enablePosition) transform.position = p;
            if (enableRotation) transform.rotation = Quaternion.Euler(r);
            if (enableScale) transform.localScale = s;

        }

        private bool TransformCoordinateSystems(GameObject local, GameObject remote, Transform t,
            out Vector3 pos, out Vector3 rot, out Vector3 scale)
        {
            var p = t.position;
            var r = t.rotation.eulerAngles;
            var s = t.localScale;

            if (local == null || remote == null)
            {
                Debug.LogWarning("local or remote origin is null.");
                pos = p;
                rot = r;
                scale = s;
                return false;
            }
            // world -> local under remote origin

            // construct remote matrix using the same scale from local matrix
            // Photon doesn't transfer scale, so fixed the scale
            Matrix4x4 remoteMatrix = Matrix4x4.TRS(remote.transform.position,
                remote.transform.rotation, Vector3.one);

            p = remoteMatrix.inverse.MultiplyPoint(p);

            // local -> world under local origin
            Matrix4x4 localMatrix = Matrix4x4.TRS(local.transform.position, local.transform.rotation, Vector3.one);
            pos = localMatrix.MultiplyPoint(p);

            // process rotation
            Matrix4x4 objectTRS = Matrix4x4.TRS(Vector3.one, t.rotation, Vector3.one);
            Matrix4x4 tf = localMatrix * objectTRS;

            rot = tf.rotation.eulerAngles;

            scale = s;

            return true;
        }
    }
}