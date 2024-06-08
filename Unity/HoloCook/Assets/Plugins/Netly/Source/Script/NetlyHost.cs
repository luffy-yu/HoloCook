namespace Netly
{
    using System;
    using Netly.Core;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
#if UNITY_EDITOR
    using layout = UnityEngine.GUILayout;
    using editor = UnityEditor.EditorGUILayout;
#endif


    public class NetlyHost : MonoBehaviour
    {
        public string ipaddress = "127.0.0.1";
        public int port = 3000;
        public Host Host => new Host(ipaddress, port);
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(NetlyHost))]
    public class NetlyHostEditor : Editor
    {
        private NetlyHost m_host;

        private void OnEnable()
        {
            m_host = (NetlyHost)target;
        }

        public override void OnInspectorGUI()
        {
            editor.Space(5);
            editor.LabelField("Netly Host");
            editor.Space(10);

            editor.HelpBox("Choose a valid port 0~65535", MessageType.None);
            editor.BeginHorizontal();
            {
                editor.LabelField("Port", layout.MaxWidth(75));
                m_host.port = (int)editor.Slider(m_host.port, 0, 65535);
            }
            editor.EndHorizontal();

            editor.Space(10);

            editor.HelpBox("Supports IPv4 and IPv6 addresses", MessageType.None);
            editor.BeginHorizontal();
            {
                editor.LabelField("IP Address", layout.MaxWidth(75));
                m_host.ipaddress = RemoveSpaces(editor.TextField(m_host.ipaddress));
            }
            editor.EndHorizontal();
        }

        private static string RemoveSpaces(string input)
        {
            return new string(input.ToCharArray().Where(c => !Char.IsWhiteSpace(c)).ToArray());
        }
    }
#endif
}
