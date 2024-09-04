//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Byter;
using Netly;
using Netly.Core;
using UnityEngine;

using Object = System.Object;
using TcpClient = Netly.TcpClient;
using UdpClient = Netly.UdpClient;

using HoloCook.HoloLens2;
using HoloCook.Utility;
using HoloCook.Sync;

namespace HoloCook.Network
{
    public class Encode
    {
        public static byte[] GetBytes(string s)
        {
            return new byte[] { 0 };
        }

        public static string GetString(byte[] b)
        {
            return "0";
        }
    }

    public enum LoginRole
    {
        Coach,
        Trainee,
        Unknown, // default
    }

    public class NetworkToolkit
    {
        public static string GetWiFiIPv4()
        {
            return GetLocalIPv4(NetworkInterfaceType.Wireless80211);
        }

        // refer: https://stackoverflow.com/questions/6803073/get-local-ip-address
        // GetLocalIPv4(NetworkInterfaceType.Ethernet);
        // GetLocalIPv4(NetworkInterfaceType.Wireless80211);
        public static string GetLocalIPv4(NetworkInterfaceType _type)
        {
            string output = "";
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == _type && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            output = ip.Address.ToString();
                        }
                    }
                }
            }

            return output;
        }
    }

    [RequireComponent(typeof(NetlyTargetHost))]
    public class NetlyClient : MonoBehaviour
    {
        #region User

        [Header("User Info")] public int userid = 1;
        public string username = "HoloLens";

        #endregion

        [Header("Try Open Connection On Game Start")]
        public bool connectOnStart = true;

        // public TcpClient client;

        private bool isOpen = false;

        // private NetlyHost _host;


        [Header("Objects")] public SynchronizableObjectList objectsList;

        [Header("HandTracker")] public GameObject handTracker;

        // [HideInInspector] public bool isServerToClient = false;

        [Header("ImageSender")] public ImageSender imageSender;

        [Space(30)] [Header("Plane")] public Transform planeTransform; // for initializing gameobject for registration

        [Header("PC Controller")] public PCController pCController;

        internal bool isServerToClient
        {
            get { return objectsList.ServerToClient; }
        }

        internal HandPalmTracker handPalmTracker
        {
            get
            {
                if (handTracker == null) return null;
                return handTracker.GetComponent<HandPalmTracker>();
            }
        }


        public System.Action syncDirectionChanged;

        private bool preventServerStreaming = false;

        #region Netly 2.0

        private TcpClient tcp = new TcpClient();

        private UdpServer _udpServer = new UdpServer();
        Host _udpServerHost = null;

        private UdpClient _udpClient = new UdpClient();
        private Host _udpClientHost = null;


        // private Host udpHost = null;
        private Host tcpHost = null;

        private ConcurrentQueue<Action> _mainThreadWorkQueue = new ConcurrentQueue<Action>();

        private readonly string localIP = NetworkToolkit.GetWiFiIPv4();
        private string remoteIP = "192.168.0.221";
        private int remotePort = 9999;

        // execute action from other end
        public System.Action<int> ExecuteSimulation;

        private void Start()
        {
            // ip config
#if UNITY_EDITOR
            remoteIP = gameObject.GetComponent<NetlyTargetHost>().ipaddress;
#else
            (remoteIP, remotePort) = Utils.LoadIPConfig();
#endif
            // register object release event
            if (handPalmTracker != null)
            {
                // Disable registration related
                // handPalmTracker.OnObjectWasReleased += OnObjectWasReleased;
                handPalmTracker.MessageBridge += OnMessageBridge;
            }

            // register registration result event
            if (imageSender != null)
            {
#if UNITY_WSA //windows uwp
                imageSender.registrationResultReturned += OnRegistrationResultReturned;
#endif
            }

#if UNITY_EDITOR
            Debug.Log($"Local IP: {localIP}");
            Debug.Log($"Remote IP: {remoteIP}");
#endif

            tcpHost = new Host(remoteIP, remotePort);
            // UDP
            _udpClient.OnOpen(() =>
            {
#if UNITY_EDITOR
                Debug.LogError($"[ Netly Client ] UDPClient {_udpClientHost.Address.ToString()}:{_udpClientHost.Port}");
#endif
            });

            _udpClient.OnError((e) =>
            {
#if UNITY_EDITOR
                Debug.LogError("UdpClient: OnError: " + e.Message);
#endif
            });

            _udpClient.OnClose(() =>
            {
#if UNITY_EDITOR
                Debug.LogError("UdpClient: OnClose");
#endif
            });

            _udpClient.OnEvent((name, data) => { });
            // UDP
            _udpServer.OnOpen(() =>
            {
                _udpServerHost = _udpServer.Host;
#if UNITY_EDITOR
                Debug.LogError("UdpServer: OnOpen: " + _udpServerHost.ToString());
#endif

                using Writer wUdp = new Writer();
                wUdp.Write(_udpServerHost.Address.ToString());
                wUdp.Write(_udpServerHost.Port);
                tcp.ToEvent(NetlyEventTypes.ReplyUdpHost, wUdp.GetBytes());
#if UNITY_EDITOR
                Debug.LogError($"[ Netly Client ] UDPServer {_udpServerHost.Address.ToString()}:{_udpServerHost.Port}");
#endif
            });

            _udpServer.OnModify((socket) =>
            {
#if UNITY_EDITOR
                Debug.LogError("UdpServer: OnModify");
#endif
            });


            _udpServer.OnEvent((server, name, data) =>
            {
#if UNITY_EDITOR
                Debug.LogError($"UdpClient: OnEvent {name}");
#endif

                if (name == NetlyEventTypes.SyncTransform)
                {
                    if (isServerToClient && preventServerStreaming) return;

                    _mainThreadWorkQueue.Enqueue(() =>
                    {
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

                        // if (m_id == ID) return;

                        var p = objectsList.GetSynchronizableObjectByID(m_id);
                        if (p == null) return;

                        p.gameObject.transform.position = new Vector3(positionX, positionY, positionZ);
                        p.gameObject.transform.rotation = Quaternion.Euler(rotationX, rotationY, rotationZ);
                        p.gameObject.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
                    });
                }
            });

            // TCP
            tcp.OnOpen(() =>
            {
#if UNITY_EDITOR
                Debug.LogError("TcpClient: OnOpen: " + tcp.Host.ToString());
#endif
                // send login
                using Writer w = new Writer();
                w.Write(userid);
                w.Write(username);

                tcp.ToEvent(NetlyEventTypes.RequestUdpHost, w.GetBytes());
            });

            tcp.OnError((e) =>
            {
#if UNITY_EDITOR
                Debug.LogError("TcpClient: OnError: " + e.Message);
#endif
            });

            tcp.OnClose(() =>
            {
#if UNITY_EDITOR
                Debug.LogError("TcpClient: OnClose");
#endif
            });

            tcp.OnEvent((name, data) =>
            {
#if UNITY_EDITOR
                Debug.LogError($"TcpClient: OnEvent {name}");
#endif
                if (name == NetlyEventTypes.ReplyUdpHost)
                {
                    // read request
                    using Reader r = new Reader(data);
                    string ip = r.Read<string>();
                    int port = r.Read<int>();

                    if (r.Success is false) return;

                    // open udp connection
                    _udpClientHost = new Host(ip, port);
                    _udpClient.Open(_udpClientHost);

                    // set up udp server
                    _udpServerHost = new Host(localIP, 0);
                    _udpServer.Open(_udpServerHost);
                }
                else if (name == NetlyEventTypes.Action)
                {
                    _mainThreadWorkQueue.Enqueue(() =>
                    {
                        using Reader r = new Reader(data);
                        string s = r.Read<string>();
                        OnTcpEventAction(s);
                    });
                }
                // registration
                else if (name == NetlyEventTypes.SyncRegistration)
                {
                    _mainThreadWorkQueue.Enqueue(() =>
                    {
#if UNITY_EDITOR
                        Debug.LogWarning("TcpClient: SyncRegistration");
#endif
                        using Reader r = new Reader(data);
                        // id, name, x, y, z
                        int oid = r.Read<int>();
                        string name = r.Read<string>();
                        float x = r.Read<float>();
                        float y = r.Read<float>();
                        float z = r.Read<float>();

                        if (r.Success is false) return;

                        // update it
                        foreach (var p in objectsList.objects)
                        {
                            if (p.id == oid)
                            {
                                p.gameObject.transform.position = new Vector3(x, y, z);
                                return;
                            }
                        }
                    });
                }
                else if (name == NetlyEventTypes.MsgHL2Header)
                {
                    _mainThreadWorkQueue.Enqueue(() =>
                    {
#if UNITY_EDITOR
                        Debug.LogWarning("TcpClient: MsgHL2Header");
#endif
                        using Reader r = new Reader(data);
                        var name = r.Read<string>();
                        var flag = r.Read<bool>();
                        Utils.WriteLog($"MsgHL2Header: {name} {flag}");
                        pCController.ActivateObject(name, flag);
                    });
                }
                else if (name == NetlyEventTypes.Data)
                {
                    _mainThreadWorkQueue.Enqueue(() =>
                    {
#if UNITY_EDITOR
                        Debug.LogWarning("TcpClient: Data");
#endif
                        using Reader r = new Reader(data);
                        var name = r.Read<string>();
                        var x = r.Read<float>();
                        var y = r.Read<float>();
                        var z = r.Read<float>();
                        pCController.ProcessData(name, x, y, z);
                    });
                }
                else if (name == NetlyEventTypes.ActionSimulation)
                {
                    _mainThreadWorkQueue.Enqueue(() =>
                    {
                        Utils.WriteLog("TcpClient: ActionSimulation");
                        using Reader r = new Reader(data);
                        var simu = r.Read<int>();

                        // handle by pc first
                        if (!pCController.HandleSimulatedAction(simu))
                        {
                            // not handled, then use another handler
                            if (ExecuteSimulation != null)
                            {
                                ExecuteSimulation(simu);
                            }
                        }
                    });
                }
            });

            // OPEN
            tcp.Open(tcpHost);
        }

        private void OnMessageBridge(byte[] msg)
        {
            // send to PC
            if (!tcp.IsOpened) return;

            using Reader w = new Reader(msg);
            var name = w.Read<string>();
            var flag = w.Read<bool>();
            tcp.ToEvent(NetlyEventTypes.MsgHL2Header, msg);
            Utils.WriteLog($"OnMessageBridge: {name} {flag}");
        }

        private void OnTcpEventAction(string data)
        {
            Utils.WriteLog($"OnTcpEventAction: {data}");
            // process data
            if (data.Contains(NetlyEventTypes.FieldSep))
            {
                // take picture
                var arr = data.Split(NetlyEventTypes.FieldSep.ToCharArray());
                if (arr[0] == NetlyEventTypes.ActionCapture)
                {
                    var oid = int.Parse(arr[1]);
                    OnTcpEventActionTakePicture(oid);
                }

                else if (arr[0] == NetlyEventTypes.ActionThreshold)
                {
                    // set threshold
                    var cThreshold = float.Parse(arr[1]);
                    OnTcpEventActionSetThreshold(cThreshold);
                }

                // set single object
                else if (arr[0] == NetlyEventTypes.ActionSwitchOne)
                {
                    pCController.SwitchOne(arr[1]);
                }
            }
            else
            {
                if (data == NetlyEventTypes.ActionQuitApp)
                {
                    // quit application
                    Application.Quit(0);
                }
                else if (data == NetlyEventTypes.ActionStart)
                {
                    // start tutoring
                    LoginAs(LoginRole.Coach);
                    RequestClientToServer(true);
                }
                else if (data == NetlyEventTypes.ActionHL2PC)
                {
                    // start HL2PC
                    RequestClientToServer(true);
                }
                else if (data == NetlyEventTypes.ActionPC2HL)
                {
                    // start PC2HL
                    RequestServerToClient(true);
                }
                else if (data == NetlyEventTypes.ActionSwitchAll)
                {
                    pCController.SwitchAll();
                }
                else if (data == NetlyEventTypes.ActionEnableCocktail)
                {
                    // false if it's called from PC
                    pCController.EnableCocktail(false);
                }
                else if (data == NetlyEventTypes.ActionEnablePancake)
                {
                    // false if it's called from PC
                    pCController.EnablePancake(false);
                }
                else if (data == NetlyEventTypes.ActionSwitchCocktail)
                {
                    pCController.SwitchCocktail();
                }
                else if (data == NetlyEventTypes.ActionSwitchPancake)
                {
                    pCController.SwitchPancake();
                }
                else if (data == NetlyEventTypes.ActionSwitchPlane)
                {
                    pCController.SwitchPlane();
                }
                else if (data == NetlyEventTypes.ActionShowUIs)
                {
                    pCController.SetUIs(true, true);
                }
                else if (data == NetlyEventTypes.ActionReleaseHand)
                {
                    handPalmTracker.ForceRelease();
                }
            }
        }

        private void OnTcpEventActionTakePicture(int objid)
        {
            Utils.WriteLog($"OnTcpEventActionTakePicture({objid})");
            // query object
            var obj = objectsList.FindSynchronizableObjectByID(objid);
            var name = obj.gameObject.name;
            var t = obj.gameObject.transform;
            // call 
            OnObjectWasReleased(name, t.position, t.rotation.eulerAngles, t.localScale);
        }

        private void OnTcpEventActionSetThreshold(float cthreshold)
        {
            Utils.WriteLog($"OnTcpEventActionSetThreshold({cthreshold})");
            handPalmTracker.cThreshold = cthreshold;
        }

        private void OnRegistrationResultReturned(string objid, string objname, Vector3 translation)
        {
            // find object by id
            var so = objectsList.FindSynchronizableObjectByID(int.Parse(objid));
            if (so == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"Can not find object for ID {objid} of registration result.");
#endif
            }

            // update position
            _mainThreadWorkQueue.Enqueue(() =>
            {
                // only update x and z
                var pos = so.gameObject.transform.position;
                translation.y = pos.y;
                so.gameObject.transform.position = translation;
            });

        }

        private void OnObjectWasReleased(string objname, Vector3 pos, Vector3 rot, Vector3 scale)
        {
            var so = objectsList.FindSynchronizableObjectByName(objname);
            var objID = so.id;

            Dictionary<string, Object> nameValues = new Dictionary<string, Object>();
            nameValues[UnityOpenGLUtils.key_objName] = objname;
            nameValues[UnityOpenGLUtils.key_objID] = $"{objID}";

            nameValues[UnityOpenGLUtils.key_position] = pos;
            nameValues[UnityOpenGLUtils.key_rotation] = rot;
            nameValues[UnityOpenGLUtils.key_scale] = scale;
            // add plane position and plane rotation for initializing object registration
            nameValues[UnityOpenGLUtils.key_planePos] = planeTransform.position;
            nameValues[UnityOpenGLUtils.key_planeRot] = planeTransform.rotation.eulerAngles;
#if UNITY_WSA //windows uwp
            if (imageSender == null) return;
            // update additional
            imageSender.additionalNameValues = nameValues;
            // take picture and send data
            imageSender.TakePicture();
#endif
        }

        #region Sync Direction Setting

        public void RequestClientToServer(bool request)
        {
            objectsList.ServerToClient = false;
            if (objectsList._client == null)
            {
                objectsList._client = _udpClient;
            }

            objectsList.running = true;

            handTracker.SetActive(true);

            if (syncDirectionChanged != null) syncDirectionChanged();
            if (request) Client_RequestChangeDirection(true, false);
        }

        public void RequestServerToClient(bool request)
        {
            objectsList.ServerToClient = true;
            objectsList.running = false;

            handTracker.SetActive(false);

            if (syncDirectionChanged != null) syncDirectionChanged();
            if (request) Client_RequestChangeDirection(false, true);
        }

        void Update()
        {
            while (_mainThreadWorkQueue.TryDequeue(out Action workload))
            {
                workload();
            }
        }



        #endregion

        #region Streaming Start/Stop Setting

        public void StartStreaming()
        {
            if (isServerToClient)
            {
                // streaming if from server and can not stop on client side, prevent update
                preventServerStreaming = false;
            }
            else
            {
                objectsList.running = true;
            }
        }

        public void StopStreaming()
        {
            if (isServerToClient)
            {
                preventServerStreaming = true;
            }
            else
            {
                objectsList.running = false;
            }
        }


        #endregion

        #endregion


        private void OnApplicationQuit()
        {
            if (tcp.IsOpened) tcp.Close();
            if (_udpServer.IsOpened) _udpServer.Close();
            if (_udpClient.IsOpened) _udpClient.Close();
        }

        #region Netly

        public void LoginAs(LoginRole role)
        {
            if (!tcp.IsOpened) return;

            using Writer w = new Writer();
            w.Write(userid);
            w.Write(username);
            w.Write(role.ToString());

            tcp.ToEvent(NetlyEventTypes.LoginRole, w.GetBytes());
        }

        private void Client_RequestChangeDirection(bool c2s, bool s2c)
        {
            if (!tcp.IsOpened) return;

#if UNITY_EDITOR
            Debug.LogError("TcpClient: Request to Change Direction");
#endif
            using Writer w = new Writer();
            w.Write(userid);
            w.Write(username);
            w.Write(c2s);
            w.Write(s2c);

            tcp.ToEvent(NetlyEventTypes.ChangeDirection, w.GetBytes());
        }

        #endregion

        #region Action recording

        public void Client_RequestActionRecording(byte[] data)
        {
            if (!tcp.IsOpened) return;
            Utils.WriteLog($"Client_RequestActionRecording({data.Length})");

            tcp.ToEvent(NetlyEventTypes.ActionRecord, data);
        }

        #endregion

// #if UNITY_EDITOR
//         private void OnGUI()
//         {
//             GUILayout.BeginVertical();
//
//             if (GUILayout.Button("Coach"))
//             {
//                 LoginAs(LoginRole.Coach);
//             }
//
//             if (GUILayout.Button("Trainee"))
//             {
//                 LoginAs(LoginRole.Trainee);
//             }
//
//             if (GUILayout.Button("C2S"))
//             {
//                 RequestClientToServer(true);
//             }
//
//             if (GUILayout.Button("S2C"))
//             {
//                 RequestServerToClient(true);
//             }
//
//             GUILayout.EndVertical();
//         }
// #endif
    }
}