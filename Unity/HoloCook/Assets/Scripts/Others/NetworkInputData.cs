//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

#if UNITY_ANDROID
using UnityEngine;

namespace HoloCook.Others
{
    public class NetworkInputData : MonoBehaviour
    {
    
    }
}
#else
using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector3 direction;
}
#endif
