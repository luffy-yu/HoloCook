//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

#if UNITY_WSA
using UnityEngine;

namespace HoloCook.Others
{
    public class Player : MonoBehaviour
    {
    
    }
}
#else
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Player : NetworkBehaviour
{
    private NetworkCharacterControllerPrototype _cc;

    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterControllerPrototype>();
    }
    
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            data.direction.Normalize();
            _cc.Move(5*data.direction*Runner.DeltaTime);
        }
    }
}
#endif