//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

// #if !UNITY_WSA // not windows uwp
// using System.Collections.Generic;
// using UnityEngine;
// public class ImageSender : MonoBehaviour
// {
//     public Dictionary<string, object> additionalNameValues;
//
//     public void TakePicture()
//     {
//         
//     }
// }
// #else
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using UnityEngine;
using Object = System.Object;

using HoloCook.Network;
using HoloCook.Utility;

namespace HoloCook.Sync
{
    public class ImageSender : MonoBehaviour
    {
        private string port = "8080";
        private string hostIP = "192.168.0.221";

        
        private ImageFromVideoStream _imageFromVideoStream;

        public ImageFromVideoStream imageFromVideoStream
        {
            get
            {
                if (_imageFromVideoStream == null)
                {
                    // it's attached to MainCamera
                    _imageFromVideoStream = FindObjectOfType<ImageFromVideoStream>();
                }

                return _imageFromVideoStream;
            }
        }

        // additional value to send
        [HideInInspector]public Dictionary<string, Object> additionalNameValues = null;

        // action once registration result is returned
        public Action<string, string, Vector3> registrationResultReturned;

        private string serverURL;

        private void Start()
        {
            var (hostIP, _) = Utils.LoadIPConfig();
            serverURL = $"http://{hostIP}:{port}";
        }

        public void TakePicture()
        {
#if UNITY_EDITOR
            UnityTest();
            return;
#endif
            imageFromVideoStream.additionalNameValues = additionalNameValues;
            imageFromVideoStream.requestSendImage = true;
        }

        void UnityTest()
        {
            var imageFilename = "IMG_20230528_120207.jpg";
            var imageFullpath = @"D:\Projects\Code\XRRegistration\Python\img\IMG_20230528_120207.jpg";

            byte[] imageArray = File.ReadAllBytes(imageFullpath);
            string base64Image = Convert.ToBase64String(imageArray);

            byte[] response = null;

            var pos = Vector3.zero;
            var rot = Vector3.zero;
            var scale = Vector3.one;

            // objID, objName are from here
            NameValueCollection additional = null;

            if (additionalNameValues != null)
            {
                foreach (KeyValuePair<string, Object> kvp in additionalNameValues)
                {
                    var k = kvp.Key;
                    if (k == UnityOpenGLUtils.key_position)
                    {
                        pos = (Vector3)kvp.Value;
                    }
                    else if (k == UnityOpenGLUtils.key_rotation)
                    {
                        rot = (Vector3)kvp.Value;
                    }
                    else if (k == UnityOpenGLUtils.key_scale)
                    {
                        scale = (Vector3)kvp.Value;
                    }
                    else
                    {
                        if (additional == null)
                        {
                            additional = new NameValueCollection();
                        }

                        additional.Add(k, kvp.Value.ToString());
                    }

                }
            }

            using (WebClientCert client = new WebClientCert())
            {
                var data = new NameValueCollection();
                data["image"] = base64Image;
                data["filename"] = imageFilename;

                data["objPosition"] = pos.ToString(UnityOpenGLUtils.strFormat);
                data["objRotation"] = rot.ToString(UnityOpenGLUtils.strFormat);
                data["objScale"] = scale.ToString(UnityOpenGLUtils.strFormat);

                try
                {
                    response = client.UploadValues(serverURL, data);
                }
                catch (WebException we)
                {
                    Utils.WriteLog(we.ToString());
                }

                HttpStatusCode code = client.StatusCode;
                string description = client.StatusDescription;
                if (code == HttpStatusCode.OK)
                {
                    Utils.WriteLog("UnityTest Pass");
                }
            }
        }
    }
}
// #endif