//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HoloCook.Network
{
    public class NetlyTargetHost : MonoBehaviour
    {
        public string ipaddress = "192.168.0.93";
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(NetlyTargetHost))]
    public class NetlyTargetHostEditor : Editor
    {
        private NetlyTargetHost m_host;

        private void OnEnable()
        {
            m_host = (NetlyTargetHost)target;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Netly Target Host");
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox("Supports IPv4 and IPv6 addresses", MessageType.None);
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("IP Address", GUILayout.MaxWidth(75));
                m_host.ipaddress = RemoveSpaces(EditorGUILayout.TextField(m_host.ipaddress));
            }
            EditorGUILayout.EndHorizontal();
        }

        private static string RemoveSpaces(string input)
        {
            return new string(input.ToCharArray().Where(c => !Char.IsWhiteSpace(c)).ToArray());
        }
    }
#endif
}