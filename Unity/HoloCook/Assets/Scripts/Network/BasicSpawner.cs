//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

#if UNITY_WSA
using UnityEngine;

namespace HoloCook.Network
{
    public class BasicSpawner : MonoBehaviour
    {
    
    }
}
#else

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fusion;
using Fusion.Sockets;
using HoloCook.Network;
using HoloCook.Sync;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SyncObject
{
    public string name;
    public Vector3 position;
    public Quaternion rotation;
}

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Player Prefab")] [Tooltip("Spawned object through the network, e.g., CookPrefab")]
    public string prefabName = "CookPrefab";

    public NetworkObject playerPrefab;

    [Space(30)] [Header("Setting")] public bool hideSelf = false;


    [Space(30)] [Header("Mapping")] [HideInInspector]
    public bool enableMapping = false;


    private SynchronizableObjectList _snycList;

    public SynchronizableObjectList snycList
    {
        get
        {
            if (_snycList == null)
            {
                _snycList = FindObjectOfType<SynchronizableObjectList>();
            }

            return _snycList;
        }
    }

    private NetlyServer _netlyServer;

    public NetlyServer netlyServer
    {
        get
        {
            if (_netlyServer == null)
            {
                _netlyServer = FindObjectOfType<NetlyServer>();
            }

            return _netlyServer;
        }
    }

    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private bool isServer = false;
    private bool transform2Set = false;

    private bool remoteOriginSet = false;

    private NetworkRunner _runner;

    #region Action recording


    private ActionRecordSyncer actionRecordSyncer;

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        _runner = gameObject.GetComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        transform2Set = false;

        netlyServer.requestStartGame += OnRequestStartGame;
    }

    private void OnRequestStartGame(string obj)
    {
        if (obj == "Host")
        {
            StartGame(GameMode.Host);
        }
        else if (obj == "Client")
        {
            StartGame(GameMode.Client);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!isServer && !transform2Set)
        {
            var no = FindServerNetworkObjectClone();
            AttachSourceTransforms2(no);
        }

        if (!isServer && snycList.remoteOrigin == null)
        {
            var no = FindServerNetworkObjectClone();
            SetRemoteOriginOnTraineeSide(no);
        }
    }

    private IEnumerable<GameObject> GetAllClones()
    {
        var name = prefabName + "(Clone)";
        // hide client self in client view
        var objects = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == name);
        return objects;
    }

    private void HideSelfNetworkObject()
    {
        var objects = GetAllClones();
        foreach (var go in objects)
        {
            var nwo = go.GetComponent<NetworkObject>();
            if (nwo.HasInputAuthority)
            {
                go.SetActive(false);
            }
        }
    }

    private NetworkObject FindServerNetworkObjectClone()
    {
        var objects = GetAllClones();
        foreach (var go in objects)
        {
            var nwo = go.GetComponent<NetworkObject>();
            if (!nwo.HasInputAuthority)
            {
                return nwo;
            }
        }

        return null;
    }

    #region Action mapping

    public NetworkObject FindNetworkObjectClone(bool serverside, bool server)
    {
        var objects = GetAllClones();
        foreach (var go in objects)
        {
            var nwo = go.GetComponent<NetworkObject>();
            // server on server side
            if (isServer && serverside && server && nwo.HasInputAuthority) return nwo;
            // client on server side
            if (isServer && serverside && !server && !nwo.HasInputAuthority) return nwo;
            // server on client side
            if (!isServer && !serverside && server && !nwo.HasInputAuthority) return nwo;
            // client on client side
            if (!isServer && !serverside && !server && nwo.HasInputAuthority) return nwo;
        }

        return null;
    }

    public GameObject FindGameObjectInNetworkObject(NetworkObject nwo, string name)
    {
        var go = nwo.gameObject;
        var childCount = go.transform.childCount;
        for (var i = 0; i < childCount; i++)
        {
            var g = go.transform.GetChild(i).gameObject;
            if (g.name == name) return g;
        }

        return null;
    }



    #endregion

    // Coach side
    private void AttachSourceTransforms(NetworkObject nwo)
    {
        if (nwo == null) return;

        var go = nwo.gameObject;
        var childCount = go.transform.childCount;
        for (var i = 0; i < childCount; i++)
        {
            var g = go.transform.GetChild(i).gameObject;
            TransformAttachment ta;
            if (g.TryGetComponent<TransformAttachment>(out ta))
            {
                var sg = snycList.FindObjectByName(g.name);
                if (sg != null)
                {
                    ta.source = sg;
                    Debug.LogWarning($"[Coach]Attached transform for {g.name}");
                }
            }
        }
    }

    // Trainee side 
    private void AttachSourceTransforms2(NetworkObject nwo)
    {
        if (nwo == null || transform2Set) return;
        foreach (var obj in snycList.objects)
        {
            var name = obj.gameObject.name;
            var go = nwo.gameObject;
            var childCount = go.transform.childCount;
            for (var i = 0; i < childCount; i++)
            {
                var g = go.transform.GetChild(i).gameObject;
                if (name == g.name)
                {
                    TransformAttachment2 ta;
                    if (obj.gameObject.TryGetComponent<TransformAttachment2>(out ta))
                    {
                        ta.source = g;
                        transform2Set = true;
                        Debug.LogWarning($"[Trainee]Attached transform2 for {name}");
                    }
                }
            }
        }
    }

    // set remote origin on trainee side
    private void SetRemoteOriginOnTraineeSide(NetworkObject nwo)
    {
        if (nwo == null) return;
        GameObject objInClone = null;

        var go = nwo.gameObject;
        var childCount = go.transform.childCount;
        for (var i = 0; i < childCount; i++)
        {
            var g = go.transform.GetChild(i).gameObject;
            if (g.name == snycList.originName)
            {
                objInClone = g;
                break;
            }
        }

        if (objInClone == null)
        {
            Debug.LogError("Failed to find Origin GameObject");
            return;
        }

        snycList.remoteOrigin = objInClone;
        remoteOriginSet = true;
        Debug.LogWarning("Set remote origin");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.LogError("OnPlayerJoined");
        if (runner.IsServer)
        {
            isServer = true;
            // Create a unique position for the player
            // Vector3 spawnPosition =
            // new Vector3((player.RawEncoded % runner.Config.Simulation.DefaultPlayers) * 3, 1, 0);
            NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, player);
            // Keep track of the player avatars so we can remove it when they disconnect
            _spawnedCharacters.Add(player, networkPlayerObject);

            // set player object for updating character
            runner.SetPlayerObject(player, networkPlayerObject);

            if (networkPlayerObject.HasInputAuthority)
            {
                // attach transform
                AttachSourceTransforms(networkPlayerObject);
            }
        }
        else
        {
            isServer = false;
            // find server (coach)
            var no = FindServerNetworkObjectClone();
            AttachSourceTransforms2(no);
        }

        if (hideSelf)
        {
            HideSelfNetworkObject();
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // Find and remove the players avatar
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // var data = new NetworkInputData();
        //
        // if (Input.GetKey(KeyCode.W))
        //     data.direction += Vector3.forward;
        //
        // if (Input.GetKey(KeyCode.S))
        //     data.direction += Vector3.back;
        //
        // if (Input.GetKey(KeyCode.A))
        //     data.direction += Vector3.left;
        //
        // if (Input.GetKey(KeyCode.D))
        //     data.direction += Vector3.right;
        //
        // input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    public async void StartGame(GameMode mode)
    {
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "Co0k",
            CustomLobbyName = "Co0k",
            Scene = SceneManager.GetActiveScene().buildIndex,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            // PlayerCount = 2,
            // DisableClientSessionCreation = true
        });

        if (result.Ok)
        {
            // all good
        }
        else
        {
            Debug.LogError($"Failed to Start: {result.ShutdownReason}");
        }
    }

    public string GetDebugInfo()
    {
        if (isServer) return "";
        var text = $"Transform2: {transform2Set} RemoteOrigin: {remoteOriginSet}";
        return text;
    }

    public bool Started()
    {
        return _runner != null;
    }

    // moved to PCController.cs
    // private void OnGUI()
    // {
    //     if (_runner == null)
    //     {
    //         if (GUI.Button(new Rect(200, 0, 100, 40), "Host"))
    //         {
    //             StartGame(GameMode.Host);
    //         }
    //
    //         if (GUI.Button(new Rect(300, 0, 100, 40), "Join"))
    //         {
    //             StartGame(GameMode.Client);
    //         }
    //     }
    //     else
    //     {
    //         if (!isServer)
    //         {
    //             GUI.Label(new Rect(600, 100, 300, 40), $"Transform2: {transform2Set} RemoteOrigin: {remoteOriginSet}");
    //         }
    //     }
    // }

    // #region Load Prefab by name
    //
    // public static NetworkObject GetPrefab(string name)
    // {
    //     var path = $"Assets/Prefabs/{name}.prefab";
    //     GameObject go = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
    //     var no = go.GetComponent<NetworkObject>();
    //     return no;
    // }
    //
    // #endregion
}
#endif