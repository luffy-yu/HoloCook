//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

#if UNITY_ANDROID
using UnityEngine;

namespace HoloCook.Sync
{
    public class TransformAttachment : MonoBehaviour
    {
    
    }
}
#else
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class TransformAttachment : NetworkBehaviour
{
    // TODO: disabled on Oct 27, 2023, needs testing
    // [Header("Transform")] public Transform transform;
    [Header("Source Object")] [HideInInspector]
    public GameObject source;

    [Header("Setting")] public bool enablePosition = true;

    public bool enableRotation = true;

    public bool enableScale = true;

    #region Action mapping

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

    [HideInInspector] public bool enableSync = true;

    #endregion

    public override void FixedUpdateNetwork()
    {
        if (source == null || transform == null || !enableSync) return;

        var t = source.transform;
        if (enablePosition) transform.position = t.position;
        if (enableRotation) transform.rotation = t.rotation;
        if (enableScale) transform.localScale = t.localScale;
    }
}
#endif
