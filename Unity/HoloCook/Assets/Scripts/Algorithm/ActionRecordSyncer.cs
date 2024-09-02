//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

#if UNITY_ANDROID
using UnityEngine;

namespace HoloCook.Algorithm
{
    public class ActionRecordSyncer : MonoBehaviour
    {
        // to avoid compilation errors
        public void Deserialize(byte[] data)
        {
        }
    }
}
#else
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using HoloCook.Algorithm;
using HoloCook.Sync;

public class ActionRecordSyncer : NetworkBehaviour
{
    private GameObjectRecorder recorder;
    private GameObjectRecorder mapRecorder;

    private GameObjectRecorder recorderInUse;

    private bool replaying = false;

    // the target to replay this animation
    [HideInInspector] public GameObject localObject;
    [HideInInspector] public GameObject mapToObject;
    private GameObject replayTarget;


    #region Action recording/mapping

    private TrajectoryFitting _fitting;

    private TrajectoryFitting fitting
    {
        get
        {
            if (_fitting == null)
            {
                _fitting = GetComponent<TrajectoryFitting>();
            }

            return _fitting;
        }
    }

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
        get => spawner.enableMapping;
    }

    #endregion

    private bool isHost
    {
        get
        {
            // non-network case, return true
            if (Runner == null || !Runner.isActiveAndEnabled) return true;

            // network case, return true if it's server
            if (Runner.IsServer && Runner.IsRunning) return true;

            // otherwise, return false
            return false;
        }
    }

    // to get the ID and the name of the object
    private SynchronizableObjectList _objectsList;

    [HideInInspector]
    public SynchronizableObjectList objectsList
    {
        get
        {
            if (_objectsList == null)
            {
                _objectsList = FindObjectOfType<SynchronizableObjectList>();
            }

            return _objectsList;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    #region Local replaying (host side)

    public void Deserialize(byte[] data)
    {
        print($"Deserialize: {data.Length}");
        recorder = GameObjectRecorder.Deserialize(data);

        // get sid
        var sid = recorder.sid;
        // set recording object
        localObject = objectsList.FindSynchronizableObjectByID(sid).gameObject;
    }

    // replay first position for 1 second
    public override void FixedUpdateNetwork()
    {
        if (replaying && recorderInUse != null)
        {
            var (t, r) = recorderInUse.Pop();
            // loop is not done
            replayTarget.transform.position = t;
            replayTarget.transform.rotation = r;

            if (recorderInUse.Looped())
            {
                replaying = false;
                mapToObject.GetComponent<TransformAttachment>().enableSync = true;
            }
        }
    }

    #endregion

    #region Remote only replaying

    // map replay only for remote side, not visible on local side
    public void MapReplay()
    {
        // find the server network object on server side
        var sngo = spawner.FindNetworkObjectClone(true, true);
        if (sngo == null)
        {
            Debug.LogError("Can not find server network object");
            return;
        }

        print($"server no id: {sngo.Id}");

        var sid = recorder.sid;
        var sname = recorder.sname;
        var tid = recorder.tid;
        var tname = recorder.tname;


        // set mapTo object
        mapToObject = spawner.FindGameObjectInNetworkObject(sngo, sname);
        print($"mapToObject: {mapToObject.name}");

        // // map it
        // mapRecorder = GameObjectRecorder.Map(recorder, fitting, tgo.transform.position, end.transform.position);
        RPC_RequestTargetInfo(sid, sname, tid, tname);
    }

    #endregion

    #region Debug

    private void OnGUI()
    {
        if (GUI.Button(new Rect(200, 10, 100, 50), "Map Replay"))
        {
            MapReplay();
        }

        if (GUI.Button(new Rect(300, 10, 100, 50), "Locally"))
        {
            recorderInUse = recorder;
            if (recorderInUse != null)
            {
                recorderInUse.Rewind();
                replayTarget = localObject;
            }

            replaying = true;
        }

        if (GUI.Button(new Rect(400, 10, 100, 50), "Remotely"))
        {
            recorderInUse = mapRecorder;

            if (recorderInUse != null)
            {
                recorderInUse.Rewind();
                replayTarget = mapToObject;
                // disable TA
                mapToObject.GetComponent<TransformAttachment>().enableSync = false;
            }

            replaying = true;
        }

        if (GUI.Button(new Rect(500, 10, 100, 50), "RPC"))
        {
            RPC_RequestTargetInfo(7, "Cube", 1, "Plane");
        }
    }

    #endregion

    #region RPC Message

    // It can not reply too fast
    private IEnumerator ReplyTargetInfo(int sid, Vector3 s, Vector3 t)
    {
        yield return new WaitForSeconds(1);
        // reply
        RPC_ReturnTargetInfo(s, t);
        yield return new WaitForSeconds(1);
        // enable sync
        var ta2 = objectsList.FindSynchronizableObjectByID(sid).gameObject.GetComponent<TransformAttachment2>();
        ta2.mappedStart = s;
        ta2.mappedEnd = t;
        ta2.EnableSync(true); // BUG: this will sync those transform not in the mapped squence
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_RequestTargetInfo(int sid, string sname, int tid, string tname, RpcInfo info = default)
    {
        if (info.IsInvokeLocal)
        {
            print($"Server: RPC_RequestTargetInfo {sid}, {sname}, {tid}, {tname}");
        }
        else
        {
            print($"Client: RPC_RequestTargetInfo {sid}, {sname}, {tid}, {tname}");
            Vector3 src = objectsList.FindSynchronizableObjectByID(sid).gameObject.transform.position;
            Vector3 tgt = objectsList.FindSynchronizableObjectByID(tid).gameObject.transform.position;

            print($"src: {src.ToString("f5")} tgt: {tgt.ToString("f5")}");

            StartCoroutine(ReplyTargetInfo(sid, src, tgt));
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ReturnTargetInfo(Vector3 src, Vector3 tgt, RpcInfo info = default)
    {
        if (info.IsInvokeLocal)
        {
            print("Client: RPC_ReturnTargetInfo");
        }
        else
        {
            print("Server: RPC_ReturnTargetInfo");
            // map it
            mapRecorder = GameObjectRecorder.Map(recorder, fitting, src, tgt);
        }
    }

    #endregion
}
#endif