//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

#if !UNITY_WSA // not windows uwp
using UnityEngine;
public class ImageFromVideoStream : MonoBehaviour
{
    
}
#else
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using CamStream.Scripts;
using HoloLensCameraStream;
using UnityEngine;
using UnityEngine.Networking;
using Object = System.Object;

using HoloCook.Network;
using HoloCook.Utility;

#if WINDOWS_UWP && XR_PLUGIN_OPENXR
using Windows.Perception.Spatial;
#endif

namespace HoloCook.Sync
{
    public class ImageFromVideoStream : MonoBehaviour
    {
        // additional value to send
        [HideInInspector]public Dictionary<string, Object> additionalNameValues = null;

        // default server settings
        private string port = "8080";
        private string hostIP = "192.168.0.221";
        private string serverURL;

        private WebClientCert client;

        private bool _connReady = false;


        private bool _requestSendImage = false;

        public bool requestSendImage
        {
            get => _requestSendImage;
            set => _requestSendImage = value;
        }

        #region From ImageSender Class

        IEnumerator TestImageUploadService()
        {
            UnityWebRequest www = UnityWebRequest.Get(serverURL);
            yield return www.SendWebRequest();

            if (www.responseCode == 200)
            {
                _connReady = true;
                Utils.WriteLog("Image Upload Service is ready");
            }
            else
            {
                Utils.WriteLog(www.error);
            }
        }

        (string, string) GetImageFilename()
        {
            var dt = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = string.Format(@"IMG_{0}.jpg", dt);
            string fullpath = System.IO.Path.Combine(Application.persistentDataPath, filename);
            return (filename, fullpath);
        }

        void SendImageBase64Data(byte[] imageArray, string filename,
            Matrix4x4 c2w,
            Matrix4x4 projection)
        {
            string base64Image = Convert.ToBase64String(imageArray);

            byte[] response = null;

            var pos = Vector3.zero;
            var rot = Vector3.zero;
            var scale = Vector3.one;

            var planepos = Vector3.zero;
            var planerot = Vector3.zero;

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
                    else if (k == UnityOpenGLUtils.key_planePos)
                    {
                        planepos = (Vector3)kvp.Value;
                    }
                    else if (k == UnityOpenGLUtils.key_planeRot)
                    {
                        planerot = (Vector3)kvp.Value;
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

            var (ct, cr) = UnityOpenGLUtils.GetCameraTRFromC2W(c2w);

            var data = new NameValueCollection();
            data["image"] = base64Image;
            data["imageWidth"] = $"{_resolution.width}";
            data["imageHeight"] = $"{_resolution.height}";
            data["filename"] = filename;
            data["cameraToWorldMatrix"] = c2w.ToString(UnityOpenGLUtils.strFormat);
            data["projectionMatrix"] = projection.ToString(UnityOpenGLUtils.strFormat);

            data["objPosition"] = pos.ToString(UnityOpenGLUtils.strFormat);
            data["objRotation"] = rot.ToString(UnityOpenGLUtils.strFormat);
            data["objScale"] = scale.ToString(UnityOpenGLUtils.strFormat);
            data["planePosition"] = planepos.ToString(UnityOpenGLUtils.strFormat);
            data["planeRotation"] = planerot.ToString(UnityOpenGLUtils.strFormat);

            data["camPosition"] = ct.ToString(UnityOpenGLUtils.strFormat);
            data["camRotation"] = cr.ToString(UnityOpenGLUtils.strFormat);

            // add additional values
            if (additional != null)
            {
                foreach (var key in additional.AllKeys)
                {
                    data.Add(key, additional.Get(key));
                }
            }

            // count time
            var startTimestampMS = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            try
            {
                response = client.UploadValues(serverURL, data);
            }
            catch (WebException we)
            {
                Utils.WriteLog(we.ToString());
            }

            long timeUsed = DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTimestampMS;

            HttpStatusCode code = client.StatusCode;
            string description = client.StatusDescription;
            if (code == HttpStatusCode.OK)
            {
                // comment out for debug purpose

                // // parse result
                // Encoding encoding = Encoding.ASCII;
                // string s = encoding.GetString(response);
                // WriteLog($"Get registration result for {filename} after {timeUsed} ms: {s}");
                // // id, name, x, y, z
                // var arr = s.Split(',');
                // var obj_id = arr[0];
                // var obj_name = arr[1];
                // var t = new Vector3(float.Parse(arr[2]), float.Parse(arr[3]), float.Parse(arr[4]));
                //
                // // trigger action
                // if (registrationResultReturned != null)
                // {
                //     registrationResultReturned(obj_id, obj_name, t);
                // }

            }

            additionalNameValues = null;
        }



        #endregion


        #region Original Variables from HoloLensCameraStream

        byte[] _latestImageBytes;
        HoloLensCameraStream.Resolution _resolution;
        VideoCapture _videoCapture;
        Texture2D _videoTexture;
        IntPtr _spatialCoordinateSystemPtr;

        #endregion

#if WINDOWS_UWP && XR_PLUGIN_OPENXR
    SpatialCoordinateSystem _spatialCoordinateSystem;
#endif

        void Start()
        {

            var (hostIP, _) = Utils.LoadIPConfig();
            serverURL = $"http://{hostIP}:{port}";

            client = new WebClientCert();

            // test upload service
            Utils.WriteLog($"Image Server:{serverURL}");
            StartCoroutine(TestImageUploadService());

            //Fetch a pointer to Unity's spatial coordinate system if you need pixel mapping
#if WINDOWS_UWP

#if XR_PLUGIN_WINDOWSMR

        _spatialCoordinateSystemPtr = UnityEngine.XR.WindowsMR.WindowsMREnvironment.OriginSpatialCoordinateSystem;

#elif XR_PLUGIN_OPENXR

        _spatialCoordinateSystem =
 Microsoft.MixedReality.OpenXR.PerceptionInterop.GetSceneCoordinateSystem(UnityEngine.Pose.identity) as SpatialCoordinateSystem;

#elif BUILTIN_XR

#if UNITY_2017_2_OR_NEWER
        _spatialCoordinateSystemPtr = UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();
#else
        _spatialCoordinateSystemPtr = UnityEngine.VR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();
#endif

#endif

#endif

            //Call this in Start() to ensure that the CameraStreamHelper is already "Awake".
            CameraStreamHelper.Instance.GetVideoCaptureAsync(OnVideoCaptureCreated);
            //You could also do this "shortcut":
            //CameraStreamManager.Instance.GetVideoCaptureAsync(v => videoCapture = v);

            _resolution = new HoloLensCameraStream.Resolution(1920, 1080);
        }

        private void OnDestroy()
        {
            if (_videoCapture != null)
            {
                _videoCapture.FrameSampleAcquired -= OnFrameSampleAcquired;
                _videoCapture.Dispose();
            }
        }

        void OnVideoCaptureCreated(VideoCapture videoCapture)
        {
            Utils.WriteLog($"OnVideoCaptureCreated");
            if (videoCapture == null)
            {
                // Debug.LogError("Did not find a video capture object. You may not be using the HoloLens.");
                Utils.WriteLog("Did not find a video capture object. You may not be using the HoloLens.");
                return;
            }

            this._videoCapture = videoCapture;

            //Request the spatial coordinate ptr if you want fetch the camera and set it if you need to 
#if WINDOWS_UWP

#if XR_PLUGIN_OPENXR
        CameraStreamHelper.Instance.SetNativeISpatialCoordinateSystem(_spatialCoordinateSystem);
#elif XR_PLUGIN_WINDOWSMR || BUILTIN_XR
        CameraStreamHelper.Instance.SetNativeISpatialCoordinateSystemPtr(_spatialCoordinateSystemPtr);
#endif

#endif

            // _resolution = CameraStreamHelper.Instance.GetLowestResolution();
            float frameRate = CameraStreamHelper.Instance.GetLowestFrameRate(_resolution);
            videoCapture.FrameSampleAcquired += OnFrameSampleAcquired;

            //You don't need to set all of these params.
            //I'm just adding them to show you that they exist.
            CameraParameters cameraParams = new CameraParameters();
            cameraParams.cameraResolutionHeight = _resolution.height;
            cameraParams.cameraResolutionWidth = _resolution.width;
            cameraParams.frameRate = Mathf.RoundToInt(frameRate);
            cameraParams.pixelFormat = CapturePixelFormat.BGRA32;
            cameraParams.rotateImage180Degrees = false;
            cameraParams.enableHolograms = false;

            UnityEngine.WSA.Application.InvokeOnAppThread(
                () =>
                {
                    _videoTexture = new Texture2D(_resolution.width, _resolution.height, TextureFormat.BGRA32, false);
                }, false);

            videoCapture.StartVideoModeAsync(cameraParams, OnVideoModeStarted);
        }

        void OnVideoModeStarted(VideoCaptureResult result)
        {
            if (result.success == false)
            {
                Utils.WriteLog("Could not start video mode.");
                return;
            }

            Utils.WriteLog("Video capture started.");
        }

        void OnFrameSampleAcquired(VideoCaptureSample sample)
        {
            //When copying the bytes out of the buffer, you must supply a byte[] that is appropriately sized.
            //You can reuse this byte[] until you need to resize it (for whatever reason).
            if (_latestImageBytes == null || _latestImageBytes.Length < sample.dataLength)
            {
                _latestImageBytes = new byte[sample.dataLength];
            }

            sample.CopyRawImageDataIntoBuffer(_latestImageBytes);

            //If you need to get the cameraToWorld matrix for purposes of compositing you can do it like this
            float[] cameraToWorldMatrixAsFloat;
            if (sample.TryGetCameraToWorldMatrix(out cameraToWorldMatrixAsFloat) == false)
            {
                //return;
            }

            //If you need to get the projection matrix for purposes of compositing you can do it like this
            float[] projectionMatrixAsFloat;
            if (sample.TryGetProjectionMatrix(out projectionMatrixAsFloat) == false)
            {
                //return;
            }

            sample.Dispose();

            // Right now we pass things across the pipe as a float array then convert them back into UnityEngine.Matrix using a utility method
            Matrix4x4 cameraToWorldMatrix = LocatableCameraUtils.ConvertFloatArrayToMatrix4x4(cameraToWorldMatrixAsFloat);
            Matrix4x4 projectionMatrix = LocatableCameraUtils.ConvertFloatArrayToMatrix4x4(projectionMatrixAsFloat);

            //This is where we actually use the image data
            //TODO: Create a class like VideoPanel for the next code
            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                // send data to server
                if (requestSendImage)
                {
                    _videoTexture.LoadRawTextureData(_latestImageBytes);
                    _videoTexture.wrapMode = TextureWrapMode.Clamp;
                    _videoTexture.Apply();

                    var (imageFilename, imageFullpath) = GetImageFilename();
                    // NOTE: it's better to use _videoTexture.EncodeToJPG() rather than _latestImageBytes. Otherwise, the hologram will disappear and show.
                    SendImageBase64Data(_videoTexture.EncodeToJPG(), imageFilename, cameraToWorldMatrix, projectionMatrix);
                    // update flag
                    requestSendImage = false;
                }

#if XR_PLUGIN_WINDOWSMR || XR_PLUGIN_OPENXR
                // It appears that the Legacy built-in XR environment automatically applies the Holelens Head Pose to Unity camera transforms,
                // but not to the new XR system (XR plugin management) environment.
                // Here the cameraToWorldMatrix is applied to the camera transform as an alternative to Head Pose,
                // so the position of the displayed video panel is significantly misaligned. If you want to apply a more accurate Head Pose, use MRTK.

                // Camera unityCamera = Camera.main;
                // Matrix4x4 invertZScaleMatrix = Matrix4x4.Scale(new Vector3(1, 1, -1));
                // Matrix4x4 localToWorldMatrix = cameraToWorldMatrix * invertZScaleMatrix;
                // unityCamera.transform.localPosition = localToWorldMatrix.GetColumn(3);
                // unityCamera.transform.localRotation =
                //     Quaternion.LookRotation(localToWorldMatrix.GetColumn(2), localToWorldMatrix.GetColumn(1));
#endif

            }, false);
        }
    }
}
#endif