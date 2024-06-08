//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System;
using Ludiq;
using UnityEngine;

//
namespace HoloCook.Sync
{
    [Serializable, Inspectable]
    public class SynchronizableObject
    {
        [Inspectable]
        public int id;
        [Inspectable]
        public GameObject gameObject;
        [Inspectable]
        public bool enable;
    }
}
