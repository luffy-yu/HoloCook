//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using HoloCook.HoloLens2;
using UnityEngine;

namespace HoloCook.Utility
{
    public static class Utils
    {
        public static List<GameObject> GetChildren(GameObject go)
        {
            List<GameObject> list = new List<GameObject>();
            return GetChildrenHelper(go, list);
        }

        private static List<GameObject> GetChildrenHelper(GameObject go, List<GameObject> list)
        {
            if (go == null || go.transform.childCount == 0)
            {
                return list;
            }

            foreach (Transform t in go.transform)
            {
                list.Add(t.gameObject);
                GetChildrenHelper(t.gameObject, list);
            }

            return list;
        }

        public static List<GameObject> GetChildren<T>(GameObject go)
        {
            List<GameObject> list = new List<GameObject>();
            list = GetChildrenHelper(go, list);
            List<GameObject> result = new List<GameObject>();
            foreach (var item in list)
            {
                if (item.GetComponent<T>() != null)
                {
                    result.Add(item);
                }
            }

            return result;
        }

        public static (string ip, int port) LoadHL2Config()
        {
            string IP = "192.168.0.163";
            int PORT = 8888;
            string filename = "hololens2.ip";
            try
            {
                // the file is under the same folder with the exe
                string path = Path.Combine(System.IO.Directory.GetCurrentDirectory(), filename);
                if (File.Exists(path))
                {
                    // read file
                    string text = File.ReadAllText(path).Trim();
                    var list = text.Split(':');
                    IP = list[0];
                    PORT = Int32.Parse(list[1]);
                }
            }
            catch (Exception e)
            {

            }

            return (IP, PORT);
        }

        public static (string ip, int port) LoadIPConfig()
        {
            string IP = "192.168.0.221";
            int PORT = 8888;
            string filename = "host.ip";
            try
            {
                string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
                if (File.Exists(path))
                {
                    // read file
                    string text = File.ReadAllText(path).Trim();
                    var list = text.Split(':');
                    IP = list[0];
                    PORT = Int32.Parse(list[1]);
                }
            }
            catch (Exception e)
            {

            }

            return (IP, PORT);
        }

        public static void WriteLog(string message)
        {
#if UNITY_EDITOR
            Debug.Log(message);
            return;
#endif
            var date = DateTime.Now.ToString("yyyyMMdd");
            var filename = string.Format(@"data_{0}.txt", date);
            string filepath = System.IO.Path.Combine(Application.persistentDataPath, filename);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            using (TextWriter writer = File.AppendText(filepath))
            {
                // write text
                writer.WriteLine(string.Format(@"{0},{1}", timestamp, message));
            }
        }

        // to make the collision more accurate, using the ThumbTip of both hands
        public static string GetThumbJointName(Handness handness)
        {
            // return $"{handness.ToString()} ThumbTip";
            
            #region Meta Quest Pro
        
            // add a rigidbody and a sphere collider to r_thumb_finger_tip_marker under RightHandGrabUseSynthetic,
            // check IsTrigger, set radius to 0.005, and do the same for the left counter part
            
            var prefix = handness == Handness.Left ? "l" : "r";
            return $"{prefix}_thumb_finger_tip_marker";
            
            #endregion
        }

        private static GUIStyle labelStyle = null;

        public static GUIStyle GetLabelStyle()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle();
                labelStyle.fontSize = 20;
                labelStyle.fontStyle = new FontStyle();
                labelStyle.normal.textColor = Color.blue;
            }

            return labelStyle;
        }
    }
}