//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.IO;
using Byter;
using Netly;
using Netly.Core;
using UnityEngine;
using HoloCook.Algorithm;
using HoloCook.Sync;
using HoloCook.Utility;

namespace HoloCook.Network
{
    [RequireComponent(typeof(NetlyTargetHost))]
    public class NetlyServer : MonoBehaviour
    {
        [Header("Try Open Connection On Game Start")]
        public bool connectOnStart = true;

        // public TcpServer server;
        // private NetlyHost _host;

        [Header("Synchronize Direction")] public bool serverToClient = false;


        [Header("Objects")] public SynchronizableObjectList objectsList;

        [Header("Fusion")] public RPCMessager rPCMessager;

        [Header("PC Controller")] public PCController pCController;

        [Space(30)] [Header("Threshold")] public float cThreshold = 20; // threshold for object-camera-hand angle


        [Space(30)] [Header("Data")] private Vector3 dataVector3 = new Vector3(-0.01f, -0.05f, 0.05f); // disable debug

        private string serverRole = LoginRole.Unknown.ToString();

        // [Header("Photon Fusion")] public BasicSpawner basicSpawner;

        private GUIStyle guiStyleLarge = null;


        public Action<string> requestStartGame;

        #region Action record

        private ActionRecordSyncer _recordSyncer;

        [HideInInspector]
        public ActionRecordSyncer recordSyncer
        {
            get
            {
                if (_recordSyncer == null)
                {
                    _recordSyncer = FindObjectOfType<ActionRecordSyncer>();
                }

                return _recordSyncer;
            }
        }

        #endregion

        #region Netly 2.0

        private TcpServer tcp = new TcpServer();

        // connected tcp client
        private TcpClient _tcpClient;


        private UdpServer _udpServer = new UdpServer();
        Host _udpServerHost = null;

        private UdpClient _udpClient = new UdpClient();
        private Host _udpClientHost = null;

        private Host host = null;

        private ConcurrentQueue<Action> _mainThreadWorkQueue = new ConcurrentQueue<Action>();

        private readonly string localIP = NetworkToolkit.GetWiFiIPv4();
        private string remoteIP;

        private bool debugMode = true;
        private int debugObjectID = 7;

        private int localPort = 8888;
        
        // synchronization from HoloLens 2 to PC
        [HideInInspector] public bool enableHL2PCSync = true;

        private void Start()
        {
            // guiStyleLarge
            guiStyleLarge = Utils.GetLabelStyle();

#if UNITY_EDITOR
            remoteIP = gameObject.GetComponent<NetlyTargetHost>().ipaddress;
#else
            (remoteIP, localPort) = Utils.LoadHL2Config();
            // hololens2 use the same port 8888, this is to enable one pc connects n hololens2
            Debug.LogError($"LoadHL2Config: {remoteIP}:{localPort}");
#endif

            Debug.Log($"Local IP: {localIP}");
            Debug.Log($"Remote IP: {remoteIP}");

            // debug mode is always disabled in non-unity mode
#if !UNITY_EDITOR
        debugMode = false;
#endif

            host = new Host(localIP, localPort);
            // UDP
            _udpClient.OnOpen(() => { Debug.LogError("UdpClient: OnOpen: " + _udpClientHost.ToString()); });

            _udpClient.OnError((e) => { Debug.LogError("UdpClient: OnError: " + e.Message); });

            _udpClient.OnClose(() => { Debug.LogError("UdpClient: OnClose"); });

            _udpClient.OnEvent((name, data) => { Debug.LogError($"UdpClient: OnEvent {name}"); });
            // UDP
            _udpServer.OnOpen(() =>
            {
                _udpServerHost = _udpServer.Host;
                Debug.LogError("UdpServer: OnOpen: " + _udpServerHost.ToString());
            });

            _udpServer.OnModify((socket) => { Debug.LogError("UdpServer: OnModify"); });


            _udpServer.OnEvent((server, name, data) =>
            {
#if UNITY_EDITOR
                Debug.LogError($"UdpServer: OnEvent {name}");
#endif
                if (name == NetlyEventTypes.SyncTransform)
                {
                    _mainThreadWorkQueue.Enqueue(() => { SyncTransform(data); });
                }
            });


            // TCP
            tcp.OnOpen(() =>
            {
                Debug.LogError("TcpServer: OnOpen: " + tcp.Host.ToString());

                _udpServerHost = new Host(host.Address, 0);
                _udpServer.Open(_udpServerHost);
            });

            tcp.OnError((e) => { Debug.LogError("TcpServer: OnError: " + e.ToString()); });

            tcp.OnClose(() => { Debug.LogError("TcpServer: OnClose"); });

            tcp.OnEvent((client, name, data) =>
            {
                Debug.LogError($"[ Netly Server ] TcpServer: OnEvent {name}");
                if (name == NetlyEventTypes.RequestUdpHost)
                {
                    // read
                    using Reader r = new Reader(data);
                    int m_id = r.Read<int>();
                    string m_name = r.Read<string>();
                    if (r.Success is false) return;

                    Debug.LogError($"[ Netly Server ] TcpServer: New client [{m_id} - {m_name}]");

                    // udp: start listening
                    using Writer wUdp = new Writer();
                    wUdp.Write(_udpServerHost.Address.ToString());
                    wUdp.Write(_udpServerHost.Port);
                    client.ToEvent(NetlyEventTypes.ReplyUdpHost, wUdp.GetBytes());

                    _tcpClient = client;

                    Debug.LogError(
                        $"[ Netly Server ] UDPServer {_udpServerHost.Address.ToString()}:{_udpServerHost.Port}");
                }
                else if (name == NetlyEventTypes.ReplyUdpHost)
                {
                    // read request
                    using Reader r = new Reader(data);
                    string ip = r.Read<string>();
                    int port = r.Read<int>();

                    if (r.Success is false) return;

                    // open udp connection
                    _udpClientHost = new Host(ip, port);
                    _udpClient.Open(_udpClientHost);

                    Debug.LogError(
                        $"[ Netly Server ] UDPClient {_udpClientHost.Address.ToString()}:{_udpClientHost.Port}");
                }
                else if (name == NetlyEventTypes.ChangeDirection)
                {
                    _mainThreadWorkQueue.Enqueue(() => { Client_EventDirection(data); });
                }
                else if (name == NetlyEventTypes.LoginRole)
                {
                    _mainThreadWorkQueue.Enqueue(() => { Client_EventLoginRole(data); });
                }
                // registration
                else if (name == NetlyEventTypes.Registration)
                {
                    _mainThreadWorkQueue.Enqueue(() => { Client_Registration(data); });
                }
                // action recording
                else if (name == NetlyEventTypes.ActionRecord)
                {
                    _mainThreadWorkQueue.Enqueue(() => { Client_ActionRecord(data); });
                }
                else if (name == NetlyEventTypes.MsgHL2Header)
                {
                    _mainThreadWorkQueue.Enqueue(() => { Client_MsgHL2Header(data); });
                }
            });

            tcp.OnExit((client) => { Debug.LogError("server tcp OnExit"); });

            // OPEN
            tcp.Open(host);
        }

        void SyncTransform(byte[] data)
        {
            // return if not enabled
            if(!enableHL2PCSync) return;
            
            using Reader r = new Reader(data);

            int m_id = r.Read<int>();

            float positionX = r.Read<float>();
            float positionY = r.Read<float>();
            float positionZ = r.Read<float>();

            float rotationX = r.Read<float>();
            float rotationY = r.Read<float>();
            float rotationZ = r.Read<float>();

            float scaleX = r.Read<float>();
            float scaleY = r.Read<float>();
            float scaleZ = r.Read<float>();

            if (r.Success is false) return;

            foreach (var p in objectsList.objects)
            {
                if (p.id == m_id)
                {
                    p.gameObject.transform.position = new Vector3(positionX, positionY, positionZ);
                    p.gameObject.transform.rotation = Quaternion.Euler(rotationX, rotationY, rotationZ);
                    p.gameObject.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);


                    return;
                }
            }
        }

        private void SetClientToServer()
        {
            objectsList.ServerToClient = false;
            objectsList.running = false;
        }

        private void SetServerToClient()
        {
            objectsList.ServerToClient = true;
            if (objectsList._client == null)
            {
                objectsList._client = _udpClient;
            }

            objectsList.running = true;
        }

        private void Client_EventDirection(byte[] data)
        {
            try
            {
                using Reader r = new Reader(data);

                var userid = r.Read<int>();
                var username = r.Read<string>();
                var c2s = r.Read<bool>();
                var s2c = r.Read<bool>();

                if (s2c && !c2s)
                {
                    Debug.LogError($"[ Netly Server ] Sync Mode: Server to Client");
                    SetServerToClient();
                }
                else if (c2s && !s2c)
                {
                    Debug.LogError($"[ Netly Server ] Sync Mode: Client to Server");
                    SetClientToServer();
                }
            }

            catch (Exception e)
            {
                Debug.LogError($"[ Zenet Client ]{nameof(Client_EventDirection)} : {e}");
            }
        }

        private void SendAction(TcpClient client, string data)
        {
            Debug.Log($"SendAction: {data}");
            using Writer w = new Writer();
            w.Write(data);
            client.ToEvent(NetlyEventTypes.Action, w.GetBytes());
        }

        public void SendData(Vector3 data)
        {
            if (_tcpClient == null || !_tcpClient.IsOpened)
            {
                Debug.LogError("TcpClient is not ready.");
                return;
            }

            Debug.Log($"SendData: {data}");
            using Writer w = new Writer();
            w.Write(NetlyEventTypes.ICEOffset);
            w.Write(data.x);
            w.Write(data.y);
            w.Write(data.z);
            _tcpClient.ToEvent(NetlyEventTypes.Data, w.GetBytes());
        }

        public void SendAction(string data)
        {
            if (_tcpClient == null || !_tcpClient.IsOpened)
            {
                Debug.LogError("TcpClient is not ready.");
                return;
            }

            Debug.Log($"SendAction: {data}");
            using Writer w = new Writer();
            w.Write(data);
            _tcpClient.ToEvent(NetlyEventTypes.Action, w.GetBytes());
        }

        public void SendSimulatedCoach(string name, bool flag)
        {
            if (_tcpClient == null || !_tcpClient.IsOpened)
            {
                Debug.LogError("TcpClient is not ready.");
                return;
            }

            Debug.Log($"SendSimulatedCoach: {name} {flag}");
            using Writer w = new Writer();
            w.Write(name);
            w.Write(flag);
            _tcpClient.ToEvent(NetlyEventTypes.MsgHL2Header, w.GetBytes());
        }

        public void SendSimulationAction(NetlyEventTypes.SimulatedMenuAction action)
        {
            if (_tcpClient == null || !_tcpClient.IsOpened)
            {
                Debug.LogError("TcpClient is not ready.");
                return;
            }

            Debug.Log($"SendSimulationAction: {action}");

            using Writer w = new Writer();
            w.Write((int)action);
            _tcpClient.ToEvent(NetlyEventTypes.ActionSimulation, w.GetBytes());
        }

        void Update()
        {
#if UNITY_EDITOR

            if (Input.GetKeyUp(KeyCode.B))
            {
                debugMode = !debugMode;
            }

            if (Input.GetKeyUp(KeyCode.Alpha0))
            {
                debugObjectID = 0;
            }

            if (Input.GetKeyUp(KeyCode.Alpha1))
            {
                debugObjectID = 1;
            }

            if (Input.GetKeyUp(KeyCode.Alpha2))
            {
                debugObjectID = 2;
            }


            if (Input.GetKeyUp(KeyCode.Alpha3))
            {
                debugObjectID = 3;
            }

            if (Input.GetKeyUp(KeyCode.Alpha4))
            {
                debugObjectID = 4;
            }

            if (Input.GetKeyUp(KeyCode.Alpha5))
            {
                debugObjectID = 5;
            }

            if (Input.GetKeyUp(KeyCode.Alpha6))
            {
                debugObjectID = 6;
            }

            if (Input.GetKeyUp(KeyCode.Alpha7))
            {
                debugObjectID = 7;
            }

            if (_tcpClient != null && !_tcpClient.IsOpened)
            {
                Debug.LogError($"TCP client is not open.");
            }

            if (_tcpClient != null && _tcpClient.IsOpened && debugMode)
            {
                // send capture image event
                if (Input.GetKeyUp(KeyCode.C))
                {
                    var data = $"{NetlyEventTypes.ActionCapture}{NetlyEventTypes.FieldSep}{debugObjectID}";
                    SendAction(_tcpClient, data);
                }
                // quit application event
                else if (Input.GetKeyUp(KeyCode.Q))
                {
                    SendAction(_tcpClient, NetlyEventTypes.ActionQuitApp);
                    // Quit application
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#endif
                    Application.Quit(0);
                }
                // start application
                else if (Input.GetKeyUp(KeyCode.S))
                {
                    SendAction(_tcpClient, NetlyEventTypes.ActionStart);
                }
                // hl2 -> pc (default)
                else if (Input.GetKeyUp(KeyCode.H))
                {
                    SendAction(_tcpClient, NetlyEventTypes.ActionHL2PC);
                }
                // pc -> hl2
                else if (Input.GetKeyUp(KeyCode.P))
                {
                    SendAction(_tcpClient, NetlyEventTypes.ActionPC2HL);
                }

                // set cThreshold
                else if (Input.GetKeyUp(KeyCode.U))
                {
                    var data = $"{NetlyEventTypes.ActionThreshold}{NetlyEventTypes.FieldSep}{cThreshold}";
                    SendAction(_tcpClient, data);
                }
                // send data
                else if (Input.GetKeyUp(KeyCode.D))
                {
                    SendData(dataVector3);
                }
            }
#endif
            while (_mainThreadWorkQueue.TryDequeue(out Action workload))
            {
                workload();
            }
        }

        #endregion


        private void OnApplicationQuit()
        {
            if (tcp.IsOpened) tcp.Close();
            if (_udpServer.IsOpened) _udpServer.Close();
            if (_udpClient.IsOpened) _udpClient.Close();
        }

        private void Client_Registration(byte[] data)
        {
            Debug.LogWarning("[ Netly Server ] TcpServer: Client_Registration");
            using Reader r = new Reader(data);
            // id, name, x, y, z
            int oid = r.Read<int>();
            string name = r.Read<string>();
            float x = r.Read<float>();
            float y = r.Read<float>();
            float z = r.Read<float>();

            if (r.Success is false) return;

            Debug.Log($"Succeed to parse registration data {oid} - {name} - ({x}, {y}, {z})");

            if (r.Success is false) return;

            // synchronization direction is server to client
            if (objectsList.ServerToClient)
            {
                foreach (var p in objectsList.objects)
                {
                    if (p.id == oid)
                    {
                        p.gameObject.transform.position = new Vector3(x, y, z);
                        return;
                    }
                }
            }
            else
            {
                if (_tcpClient.IsOpened)
                {
                    // sync registration to HoloLens 2
                    _tcpClient.ToEvent(NetlyEventTypes.SyncRegistration, data);
                }
            }
        }

        #region Action record

        public void Client_ActionRecord(byte[] data)
        {
            Debug.LogWarning("[ Netly Server ] TcpServer: Client_ActionRecord");

            // pass to ActionRecordSyncer to handle it
            recordSyncer.Deserialize(data);
        }

        #endregion

        #region HoloLens2 message

        // HL2 -> PC
        public void Client_MsgHL2Header(byte[] data)
        {
            using Reader r = new Reader(data);
            var name = r.Read<string>();
            var flag = r.Read<bool>();

            print($"[NetlyServer] Client_MsgHL2Header: {name} {flag}");
            // process animation locally if needed
            // pCController.ActivateAnimation(name);
#if !UNITY_ANDROID // not windows uwp
            rPCMessager.RPC_MsgHL2Header(data);
            print($"[NetlyServer] RPC_MsgHL2Header");
#endif
        }

#if !UNITY_WSA // not windows uwp
        public void RPC_MsgHL2Header(byte[] data)
        {
            _tcpClient.ToEvent(NetlyEventTypes.MsgHL2Header, data);
        }
#endif

        public void SimulateMsgHL2Header(string name, bool flag)
        {
            using Writer w = new Writer();
            w.Write(name);
            w.Write(flag);
            _tcpClient.ToEvent(NetlyEventTypes.MsgHL2Header, w.GetBytes());
        }

        #endregion

        private void Client_EventLoginRole(byte[] data)
        {
            using Reader r = new Reader(data);
            int m_id = r.Read<int>();
            string m_name = r.Read<string>();
            string role = r.Read<string>();

            if (r.Success is false) return;

            // update server role
            serverRole = role;

            // start photon
            StartPhotonGame(role);

            if (serverToClient)
            {
                SetServerToClient();
            }
            else
            {
                SetClientToServer();
            }
        }

        public void StartPhotonGame(string role)
        {
            if (requestStartGame == null) return;
            if (role == LoginRole.Coach.ToString())
            {
                requestStartGame("Host");
            }
            else if (role == LoginRole.Trainee.ToString())
            {
                requestStartGame("Client");
            }
        }

        public (bool, bool, bool) GetSyncStatus()
        {
            return (objectsList.enablePC2PCSync, objectsList.enablePC2HLSync, enableHL2PCSync);
        }

        public void SetSyncStatus(bool pc2pc, bool pc2hl, bool hl2pc)
        {
            objectsList.enablePC2PCSync = pc2pc;
            objectsList.enablePC2HLSync = pc2hl;
            enableHL2PCSync = hl2pc;
        }

        public string GetDebugInfo()
        {
            var text = "HoloCook " +
                       $"\nRole: {serverRole}" +
                       $"\nPC: {host.Address}:{host.Port}" +
                       $"\nHoloLens 2: {remoteIP}:{8888}\n" +
                       $"\nDe(B)ug: {debugMode}" +
                       $"\n(C)apture" +
                       $"\n(Q)uit App" +
                       $"\n(S)tart" +
                       $"\n(H)L2PC" +
                       $"\n(P)C2HL" +
                       $"\n(D)Data" +
                       // $"\n(U)pdate Thresholds [Current:{cThreshold}]" +
                       // $"\nDebugObjectID: {debugObjectID}" +
                       "";
            return text;
        }

        public GUIStyle GetLabelStyle()
        {
            return guiStyleLarge;
        }

        // moved to PCController.cs
        // private void OnGUI()
        // {
        //     // role
        //     GUI.Label(new Rect(600, 0, 100, 40), $"HoloCook " +
        //                                          $"\nRole: {serverRole}" +
        //                                          $"\nPC: {host.Address}:{host.Port}" +
        //                                          $"\nHoloLens 2: {remoteIP}:{8888}"
        //         , guiStyleLarge);
        //     // debug info
        //     GUI.Label(new Rect(600, 150, 100, 40), $"De(B)ug: {debugMode}" +
        //                                            $"\n(C)apture" +
        //                                            $"\n(Q)uit App" +
        //                                            $"\n(S)tart" +
        //                                            $"\n(H)L2PC" +
        //                                            $"\n(P)C2HL" +
        //                                            $"\n(D)Data" +
        //                                            // $"\n(U)pdate Thresholds [Current:{cThreshold}]" +
        //                                            // $"\nDebugObjectID: {debugObjectID}" +
        //                                            "",
        //         guiStyleLarge);
        // }
    }
}