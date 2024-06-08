//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using Byter;
using UnityEngine;
using HoloCook.Network;
using HoloCook.Utility;
using HoloCook.Sync;

namespace HoloCook.Algorithm
{
    public class GameObjectRecorder
    {
        public int sid; // source id
        public string sname; // source name
        public int tid; // target id
        public string tname; // target name

        public List<Vector3> positions; // positions of recorded source object
        public List<Quaternion> rotations; // rotations of recorded source object

        public int index = 0;

        public GameObjectRecorder()
        {
            index = 0;
        }


        public void Append(Transform t)
        {
            Append(t.position, t.rotation);
        }

        public void Append(Vector3 position, Quaternion rotation)
        {
            if (positions == null)
            {
                positions = new List<Vector3>();
                rotations = new List<Quaternion>();
            }

            positions.Add(position);
            rotations.Add(rotation);
        }

        public Vector3 FirstPosition()
        {
            return positions[0];
        }
    
        public (Vector3, Quaternion) Pop()
        {
            var count = positions.Count;
            if (index < count)
            {
                var p = positions[index];
                var r = rotations[index];
                index += 1;
                return (p, r);
            }

            return (positions[count - 1], rotations[count - 1]);
        }

        public bool Looped()
        {
            return positions.Count != 0 && index == positions.Count;
        }

        public bool OnStart()
        {
            return positions.Count != 0 && index == 0;
        }

        public void Rewind()
        {
            index = 0;
        }

        public void Clear()
        {
            if (positions != null)
            {
                positions.Clear();
                rotations.Clear();
            }

            Rewind();
        }

        public Vector3 LastPosition()
        {
            var count = positions.Count;
            if (positions != null && positions.Count > 0)
            {
                return positions[count - 1];
            }

            return Vector3.zero;
        }

        public static GameObjectRecorder Map(GameObjectRecorder refer, TrajectoryFitting fitting, Vector3 start,
            Vector3 end)
        {
            GameObjectRecorder gor = new GameObjectRecorder();
            var positions = refer.positions;
            var rotations = refer.rotations;

            // calculate map

            var result = fitting.TransformTrajectory(positions, start, end);

            // add result
            for (var i = 0; i < result.Count; i++)
            {
                gor.Append(result[i], rotations[i]);
            }

            return gor;
        }

        public byte[] Serialize()
        {
            // sid, sname, tid, tname, count, [(xt,yt,zt), (xr, yr, zr, wr)], [(), ()]......
            using (Writer w = new Writer())
            {
                w.Write(sid);
                w.Write(sname);
                w.Write(tid);
                w.Write(tname);
                w.Write(positions.Count);

                for (var i = 0; i < positions.Count; i++)
                {
                    // position
                    w.Write(positions[i].x);
                    w.Write(positions[i].y);
                    w.Write(positions[i].z);
                    // rotation
                    w.Write(rotations[i].x);
                    w.Write(rotations[i].y);
                    w.Write(rotations[i].z);
                    w.Write(rotations[i].w);
                }

                return w.GetBytes();
            }
        }

        public static GameObjectRecorder Deserialize(byte[] data)
        {
            var reader = new Reader(data);
            var sid = reader.Read<int>();
            var sname = reader.Read<String>();
            var tid = reader.Read<int>();
            var tname = reader.Read<string>();
            var count = reader.Read<int>();

            GameObjectRecorder recorder = new GameObjectRecorder();
            for (var i = 0; i < count; i++)
            {
                var xt = reader.Read<float>();
                var yt = reader.Read<float>();
                var zt = reader.Read<float>();

                var xr = reader.Read<float>();
                var yr = reader.Read<float>();
                var zr = reader.Read<float>();
                var wr = reader.Read<float>();

                recorder.Append(new Vector3(xt, yt, zt), new Quaternion(xr, yr, zr, wr));
            }

            // update other contributes
            recorder.sid = sid;
            recorder.sname = sname;
            recorder.tid = tid;
            recorder.tname = tname;

            return recorder;
        }
    }

    public class RecordObjectTransform : MonoBehaviour
    {
        private GameObjectRecorder m_Recorder;

        private bool recording = false;
        private bool replaying = false;

        [Header("Mapping")] public GameObject mappingTarget;
        public TrajectoryFitting trajectoryFitting;

        #region Action record related

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

        private NetlyClient _client;

        [HideInInspector]
        public NetlyClient client
        {
            get
            {
                if (_client == null)
                {
                    _client = FindObjectOfType<NetlyClient>();
                }

                return _client;
            }
        }

        #endregion

        private bool showMapped = false;
        private GameObjectRecorder mappedRecorder;

        // Start is called before the first frame update
        void Start()
        {
            m_Recorder = new GameObjectRecorder();
        }

        void FixedUpdate()
        {
            if (recording)
            {
                m_Recorder.Append(transform);
            }

            if (replaying)
            {
                if (showMapped)
                {
                    var (t, r) = mappedRecorder.Pop();
                    mappingTarget.transform.position = t;
                    mappingTarget.transform.rotation = r;

                    if (mappedRecorder.Looped())
                    {
                        replaying = false;
                    }
                }
                else
                {
                    var (t, r) = m_Recorder.Pop();
                    // loop is not done
                    transform.position = t;
                    transform.rotation = r;

                    if (m_Recorder.Looped())
                    {
                        replaying = false;
                    }
                }
            }
        }

        public void StartRecording()
        {
            Utils.WriteLog("Start Recording");
            m_Recorder.Clear();
        
            recording = true;
            replaying = false;
        }

        public void StopRecording()
        {
            Utils.WriteLog("Stop Recording");
            recording = false;

            // serialize data and send to PC

            // update attributes first
            var (sid, sname) = objectsList.GetIDNameOfObject(gameObject);
            // TODO: assume the target is the plane (id: 1, name: "plane")
            m_Recorder.sid = sid;
            m_Recorder.sname = sname;
            m_Recorder.tid = 1;
            m_Recorder.tname = "plane";
        
            // var data = m_Recorder.Serialize();
            // // send to PC
            // client.Client_RequestActionRecording(data);
            StartCoroutine(SendActionRecordData());
        }
    
        // use coroutine to execute the send command
        private IEnumerator SendActionRecordData()
        {
            var data = m_Recorder.Serialize();
            // send to PC
            client.Client_RequestActionRecording(data);
            yield return null;
        }

        public void PlayRecording()
        {
            Utils.WriteLog("Play Recording");
            m_Recorder.Rewind();
            replaying = true;

            showMapped = false;
        }

        public void PlayMapping()
        {
            Utils.WriteLog("Play Mapping");

            if (mappedRecorder == null)
            {
                var start = mappingTarget.transform.position;
                var end = m_Recorder.LastPosition();
                mappedRecorder = GameObjectRecorder.Map(m_Recorder, trajectoryFitting, start, end);
            }

            mappedRecorder.Rewind();
            showMapped = true;
            replaying = true;
        }

#if UNITY_EDITOR

        private void OnGUI()
        {
            if (GUI.Button(new Rect(10, 10, 150, 50), "Start"))
            {
                StartRecording();
            }

            if (GUI.Button(new Rect(210, 10, 150, 50), "Stop"))
            {
                StopRecording();
            }

            if (GUI.Button(new Rect(410, 10, 150, 50), "Play"))
            {
                PlayRecording();
            }

            if (GUI.Button(new Rect(610, 10, 150, 50), "Map"))
            {
                PlayMapping();
            }

        }

        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.W))
            {
                transform.Translate(0, 0.1f, 0);
            }

            if (Input.GetKeyUp(KeyCode.S))
            {
                transform.Translate(0, -0.1f, 0);
            }

            if (Input.GetKeyUp(KeyCode.A))
            {
                transform.Translate(-0.1f, 0, 0);
            }

            if (Input.GetKeyUp(KeyCode.D))
            {
                transform.Translate(0.1f, 0, 0);
            }
        }

#endif
    }
}