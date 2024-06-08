//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

#if UNITY_WSA // windows uwp
using UnityEngine;

namespace HoloCook.Network
{
    public class RPCMessager : MonoBehaviour
    {

    }
}
#else

using Fusion;

namespace HoloCook.Network
{
    public class RPCMessager : NetworkBehaviour
    {
        private NetlyServer _server;

        public NetlyServer server
        {
            get
            {
                if (_server == null)
                {
                    _server = FindObjectOfType<NetlyServer>();
                }

                return _server;
            }
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_MsgHL2Header(byte[] data, RpcInfo info = default)
        {
            if (info.IsInvokeLocal)
            {
                print("Server: RPC_MsgHL2Header");
            }
            else
            {
                print("Client: RPC_MsgHL2Header");
                server.RPC_MsgHL2Header(data);
            }
        }
    }
}
#endif