//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

#if UNITY_WSA // windows uwp
using UnityEngine;

namespace HoloCook.Utility
{
    public class CoachMenuEvents : MonoBehaviour
    {

    }
}
#else
using Fusion;

namespace HoloCook.Utility
{
    public class RPCTest: NetworkBehaviour
    {
        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_EnableTransformAttachment2(RpcInfo info = default)
        {
            if (info.IsInvokeLocal)
            {
                print("Server: RPC_EnableTransformAttachment2");
            }
            else
            {
                print("Client: RPC_EnableTransformAttachment2");
            }
        }

        private void OnGUI()
        {
            // if (GUI.Button(new Rect(500, 10, 100, 50), "RPC"))
            // {
            //     RPC_EnableTransformAttachment2();
            // }
        }
    }
}
#endif