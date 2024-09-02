//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

#if UNITY_ANDROID
using UnityEngine;

namespace HoloCook.Registration
{
    public class RegistrationNetwork : MonoBehaviour
    {
    
    }
}
#else

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bolt;
using Byter;
using HoloCook.Network;
using HoloCook.Sync;
using Ludiq;
using UnityEngine;

using Netly;
using Netly.Core;

using WebSocketSharp;
using WebSocketSharp.Server;

using OpenCvSharp;
using UnityEngine.UIElements;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;
using Rect = UnityEngine.Rect;
using Vector3 = UnityEngine.Vector3;

// Get registration data from python client
// Basically, it's a Netly TCP Server
// data includes: 
//  - object id, object name
//  - object position, rotation, and scale
//  - camera position, camera rotation, camera projection matrix
//  - captured image segmentation result
//      - segmentation regions count
//      - min segmentation value id, max segmentation value id
//      - different object using different values(e.g.) rendered image with the same size with captured image
//      - the above image will be used for processing via openCV

internal class ReceivingServer : WebSocketBehavior
{
    public static System.Action<string> OnGetData;
    protected override void OnMessage (MessageEventArgs e)
    {
        var msg = e.Data == "BALUS"
            ? "Are you kidding?"
            : "I'm not available now.";
        Send (msg);

        OnGetData(e.Data);
    }

    protected override void OnClose(CloseEventArgs e)
    {
        Debug.LogWarning("ReceivingServer OnClose");
    }

    protected override void OnError(ErrorEventArgs e)
    {
        Debug.LogError($"ReceivingServer OnClose: {e.Message}");
    }

    protected override void OnOpen()
    {
        Debug.LogWarning("ReceivingServer OnOpen");
    }
}

internal class DecodedData
{
    public string filename;
    public int objID;
    public string objName;
    public Vector3 objPosition;
    public Vector3 objRotation;
    public Vector3 objScale;
    // add plane position and rotation
    public Vector3 planePosition;
    public Vector3 planeRotation;
    public Vector3 camPosition;
    public Vector3 camRotation;
    public Matrix4x4 projectionMatrix;
    public int minPixel;
    public int maxPixel;
    public int segCount;
    public List<uint> areas;
    public Mat image;
    public Size imageSize;
    public Vector3 optimizedPosition; // optimization result

    public DecodedData()
    {
        filename = "";
        objID = 0;
        objName = "";
        objPosition = Vector3.zero;
        objRotation = Vector3.zero;
        objScale = Vector3.one;
        planePosition = Vector3.zero;
        planeRotation = Vector3.zero;
        camPosition = Vector3.zero;
        camRotation = Vector3.zero;
        projectionMatrix = Matrix4x4.identity;
        minPixel = 1;
        maxPixel = 255;
        segCount = 0;
        areas = new List<uint>();
        image = new Mat(1, 1, MatType.CV_8U);
    }

    public void DecodeInt(ref int value, string s)
    {
        value = Int32.Parse(s);
    }

    public void DecodeString(ref string value, string s)
    {
        value = s;
    }

    public void DecodeVector3(ref Vector3 vec, string v1, string v2, string v3)
    {
        vec.x = float.Parse(v1);
        vec.y = float.Parse(v2);
        vec.z = float.Parse(v3);
    }

    public void DecodeProjectionMatrix(ref Matrix4x4 mat, string v11, string v12, string v13, string v14,
        string v21, string v22, string v23, string v24,
        string v31, string v32, string v33, string v34,
        string v41, string v42, string v43, string v44)
    {
        mat.m00 = float.Parse(v11);
        mat.m10 = float.Parse(v12);
        mat.m20 = float.Parse(v13);
        mat.m30 = float.Parse(v14);

        mat.m01 = float.Parse(v21);
        mat.m11 = float.Parse(v22);
        mat.m21 = float.Parse(v23);
        mat.m31 = float.Parse(v24);

        mat.m02 = float.Parse(v31);
        mat.m12 = float.Parse(v32);
        mat.m22 = float.Parse(v33);
        mat.m32 = float.Parse(v34);

        mat.m03 = float.Parse(v41);
        mat.m13 = float.Parse(v42);
        mat.m23 = float.Parse(v43);
        mat.m33 = float.Parse(v44);
    }

    public void DecodeList(ref List<uint> list, string[] arr, int start, int count)
    {
        list.Clear();
        for (int i = 0; i < count; i++)
        {
            list.Add(uint.Parse(arr[start + i]));
        }
    }

    public void DecodeImage(string data)
    {
        byte[] vec = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            vec[i] = Convert.ToByte(data[i]);
        }

        image = Cv2.ImDecode(vec, ImreadModes.Grayscale);
        imageSize = image.Size();
    }

    public void ShowImage()
    {
        using (var t = new ResourcesTracker())
        {
            t.T(new Window("Decoded Image", image));
            Cv2.WaitKey();
        }
    }

    public string ToString(string format)
    {
        return $"filename: {filename}\n" +
               $"obj_id: {objID}\n" +
               $"obj_name: {objName}\n" +
               $"obj_position: {objPosition.ToString(format)}\n" +
               $"obj_rotation: {objRotation.ToString(format)}\n" +
               $"obj_scale: {objScale.ToString(format)}\n" +
               $"plane_position: {planePosition.ToString(format)}\n" +
               $"plane_rotation: {planeRotation.ToString(format)}\n" +
               $"cam_position: {camPosition.ToString(format)}\n" +
               $"cam_rotation: {camRotation.ToString(format)}\n" +
               $"projection_mat: {projectionMatrix.ToString(format)}\n" +
               $"min_pix: {minPixel}\n" +
               $"max_pix: {maxPixel}\n" +
               $"seg_count: {segCount}\n" +
               $"areas count: {areas.Count}\n" +
               $"Segmentation: {image.Size().ToString()} Channels: {image.Channels()}";
    }
}

public class RegistrationNetwork : MonoBehaviour
{
    #region Receiving Data from Python

    [Header("Receiving - Python")] [SerializeField]
    private bool enableIncoming = true;

    [SerializeField] private string incomingIP = "localhost";

    [SerializeField] private int incomingPort = 7777;

    // this tcp server is to let python client to connect, i.e., receiving the data
    private TcpServer incoming = new TcpServer();

    private Host incomingHost = null;

    private WebSocketServer webSocketServer;

    #endregion

    #region Sending Registration Result to Unity

    [Header("Sending - Unity")] [SerializeField]
    private bool enableOutgoing = false;

    [SerializeField] private string outgoingIP = "localhost";

    [SerializeField] private int outgoingPort = 8888;

    [Header("Optimization")] [SerializeField] [Tooltip("Save medium result to image, e.g., best match, etc.")]
    private bool saveMedium;

    // this tcp client is connect the PC which is to send the registration result
    private TcpClient outgoing = null;

    private Host outgoingHost = null;

    #endregion

    private ConcurrentQueue<Action> _mainThreadWorkQueue = new ConcurrentQueue<Action>();

    #region Decoded Data

    // sep for splitting data, the same as in python
    private char sep = Convert.ToChar(300);
    private DecodedData _decodedData;

    #endregion

    [Inspectable] public SynchronizableObject[] objects;

    #region Initialization

    // for the 1st registration result, assume that it is for all objects' registration
    private bool registrationInitialized = false;

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        var localip = NetworkToolkit.GetWiFiIPv4();
        if (incomingIP == "localhost")
        {
            incomingIP = localip;
        }

        if (outgoingIP == "localhost")
        {
            outgoingIP = localip;
        }

        incomingHost = new Host(incomingIP, incomingPort);
        outgoingHost = new Host(outgoingIP, outgoingPort);

        if (enableIncoming)
        {
            Debug.LogWarning($"Incoming Server: {incomingHost.ToString()}");
            InitializeWebsocketServer();
            ReceivingServer.OnGetData += OnGetData;
            // InitializeIncomingServer();   
        }

        if (enableOutgoing)
        {
            InitializeOutgoingClient();
            Debug.LogWarning($"Outgoing Client: {outgoingHost.ToString()}");
        }

        // var name = "event";
        // var msg = "Hello World";
        // var data = NE.GetBytes(msg);
        // data = MessageParser.Create(name, data);
        //
        // using (Writer writer = new Writer())
        // {
        //     writer.Write("Ny://", Encoding.ASCII);
        //     writer.Write(name, Encoding.UTF8);
        //     writer.Write(msg);
        //     data = writer.GetBytes();
        // }
        //
        // Debug.Log(data);
        //
        // Debug.Log(data.Length);

        return;

        using (var t = new ResourcesTracker())
        {
            var src = t.T(new Mat(@"D:\Projects\Code\HoloCook\Python\data\test.png",
                ImreadModes.Grayscale));
            t.T(new Window("src image", src));

            var low = new Scalar(121);
            var high = new Scalar(121);

            using var dst = new Mat();

            Cv2.InRange(src, low, high, dst);

            t.T(new Window("dst image", dst));

            Cv2.WaitKey();
        }

    }

    private void OnGetData(string msg)
    {
        _mainThreadWorkQueue.Enqueue(() =>
        {
            // decode data
            DecodeData(msg);
        });
    }

    #region Display via OpenCV

    void ShowFile(string path, string title)
    {
        using (var t = new ResourcesTracker())
        {
            var src = t.T(new Mat(path, ImreadModes.Unchanged));
            t.T(new Window(title, src));
            Cv2.WaitKey();
        }
    }

    void ShowMat(string title, Mat m)
    {
        using (var t = new ResourcesTracker())
        {
            t.T(new Window(title, m));
            Cv2.WaitKey();
        }
    }

    #endregion

    #region Decode Data from Python

    void DecodeData(string msg)
    {

        var arr = msg.Split(sep);

        // obj_id, obj_name, obj_position, obj_rotation, obj_scale, cam_position, cam_rotation,
        // projection_mat,
        // min_pix, max_pix, seg_count, areas, segmentation
        if (_decodedData == null)
        {
            _decodedData = new DecodedData();
        }

        var idx = 0;

        _decodedData.DecodeString(ref _decodedData.filename, arr[idx]);

        var offset = 1;
        idx += offset;
        _decodedData.DecodeInt(ref _decodedData.objID, arr[idx]);

        offset = 1;
        idx += offset;
        _decodedData.DecodeString(ref _decodedData.objName, arr[idx]);

        offset = 1;
        idx += offset;
        _decodedData.DecodeVector3(ref _decodedData.objPosition, arr[idx], arr[idx + 1], arr[idx + 2]);

        offset = 3;
        idx += offset;
        _decodedData.DecodeVector3(ref _decodedData.objRotation, arr[idx], arr[idx + 1], arr[idx + 2]);

        offset = 3;
        idx += offset;
        _decodedData.DecodeVector3(ref _decodedData.objScale, arr[idx], arr[idx + 1], arr[idx + 2]);

        // add plane position and plane rotation
        offset = 3;
        idx += offset;
        _decodedData.DecodeVector3(ref _decodedData.planePosition, arr[idx], arr[idx + 1], arr[idx + 2]);

        offset = 3;
        idx += offset;
        _decodedData.DecodeVector3(ref _decodedData.planeRotation, arr[idx], arr[idx + 1], arr[idx + 2]);

        offset = 3;
        idx += offset;
        _decodedData.DecodeVector3(ref _decodedData.camPosition, arr[idx], arr[idx + 1], arr[idx + 2]);

        offset = 3;
        idx += offset;
        _decodedData.DecodeVector3(ref _decodedData.camRotation, arr[idx], arr[idx + 1], arr[idx + 2]);

        offset = 3;
        idx += offset;
        _decodedData.DecodeProjectionMatrix(ref _decodedData.projectionMatrix,
            arr[idx], arr[idx + 1], arr[idx + 2], arr[idx + 3],
            arr[idx + 4], arr[idx + 5], arr[idx + 6], arr[idx + 7],
            arr[idx + 8], arr[idx + 9], arr[idx + 10], arr[idx + 11],
            arr[idx + 12], arr[idx + 13], arr[idx + 14], arr[idx + 15]);

        offset = 16;
        idx += offset;
        _decodedData.DecodeInt(ref _decodedData.minPixel, arr[idx]);

        offset = 1;
        idx += offset;
        _decodedData.DecodeInt(ref _decodedData.maxPixel, arr[idx]);

        offset = 1;
        idx += offset;
        _decodedData.DecodeInt(ref _decodedData.segCount, arr[idx]);

        offset = 1;
        idx += offset;
        _decodedData.DecodeList(ref _decodedData.areas, arr, idx, _decodedData.segCount);

        offset = _decodedData.segCount;
        idx += offset;
        _decodedData.DecodeImage(arr[idx]);

        // // enable the object having the objID, and disable other objects
        // SetObjectsActiveness(_decodedData.objID);

        //initialize optimization result
        _decodedData.optimizedPosition = _decodedData.planePosition;

        // save image
        // Cv2.ImWrite(@"D:\Projects\Code\HoloCook\Python\data\test-unity.png", _decodedData.image);

        // to string
        Debug.Log(_decodedData.ToString("f5"));

        // show image
        // _decodedData.ShowImage();

        if (saveMedium)
        {
            // save the segment image
            SavePNG(_decodedData.image, GeneratePNGFilename(_decodedData.filename, "seg"));
        }

        UpdateCamera();

        if (registrationInitialized)
        {
            // registration is initialized, then it's to do single object's registration
            var obj = FindObjectByID(_decodedData.objID);
            SingleObjectRegistration(obj, _decodedData.objID, _decodedData.objName, _decodedData.filename);

            // // find object by id
            // UpdateObject(obj);
            //
            // if (saveMedium)
            // {
            //     // save the initial image
            //     SavePNG(TakeSnapshot().EncodeToPNG(), GeneratePNGFilename(_decodedData.filename, "init"));
            // }
            //
            // // optimize
            // var result = OptimizeObjectPosition(obj);
            //
            // Debug.Log($"Optimization result: {result.ToString("f5")}");
            //
            // if (saveMedium)
            // {
            //     // save the result image
            //     SavePNG(TakeSnapshot().EncodeToPNG(), GeneratePNGFilename(_decodedData.filename, "res"));
            // }
            //
            // // send result
            // SendRegistrationResult(_decodedData.objName, _decodedData.objID);
        }
        else
        {
            // multiple objects' registration

            // loop object to do registration
            var index = 1;
            foreach (var so in objects)
            {
                var obj = so.gameObject;
                var sid = so.id;

                var filename = _decodedData.filename;

                // only care about enabled objects
                if (so.enable)
                {
                    print($"Processing: {sid} - {obj.name} - {filename}");
                    // reset optimized position
                    _decodedData.optimizedPosition = _decodedData.planePosition;

                    SingleObjectRegistration(obj, sid, obj.name, filename, index);
                    index += 1;
                }
            }

            SaveInitializedResult(_decodedData.filename);

            registrationInitialized = true;
        }
    }

    void SaveInitializedResult(string filename)
    {
        foreach (var so in objects)
        {
            if (so.enable)
            {
                so.gameObject.SetActive(true);
            }
        }

        // use index 0 to indicate it's the final result
        SavePNG(TakeSnapshot().EncodeToPNG(), GeneratePNGFilename(filename, $"res-{0}"));
    }

    void SingleObjectRegistration(GameObject obj, int objid, string objname, string filename, int index = 0)
    {
        // set activeness
        SetObjectsActiveness(objid);

        // find object by id
        // var obj = FindObjectByID(_decodedData.objID);
        InitializeObject(obj);

        if (saveMedium)
        {
            // save the initial image
            SavePNG(TakeSnapshot().EncodeToPNG(), GeneratePNGFilename(filename, $"init-{index}"));
        }

        // optimize
        var result = OptimizeObjectPosition(obj);

        Debug.Log($"Optimization result: {result.ToString("f5")}");

        if (saveMedium)
        {
            // save the result image
            SavePNG(TakeSnapshot().EncodeToPNG(), GeneratePNGFilename(filename, $"res-{index}"));
        }

        // send result
        SendRegistrationResult(objname, objid);
    }


    string GeneratePNGFilename(string filename, string suffix)
    {
        var basename = Path.GetFileNameWithoutExtension(filename);
        return $"{basename}-{suffix}.png";
    }

    float Optimize(GameObject obj, float step, Vec2f dstCentroid, Vec2i direction, bool x, bool z)
    {
        // move until direction changed
        var offset = step;
        while (true)
        {
            // src position
            var pos = _decodedData.optimizedPosition;
            // plus offset
            var newPos = new Vector3(pos.x, pos.y, pos.z);
            if (x)
            {
                newPos.x += offset * direction.Item0;
            }

            if (z)
            {
                newPos.z += offset * direction.Item1;
            }

            // update object position
            UpdateObjectPosition(obj, newPos);
            // get rendering result
            var texture = TakeSnapshot();
            using var renderedContour = GetRenderedContour(texture);

            if (renderedContour != null)
            {
                var curCentroid = GetContourCentroid(renderedContour);
                var newDirection = GetObjectMoveDirection(dstCentroid, curCentroid);

                if (x && newDirection.Item0 != direction.Item0)
                {
                    break;
                }

                if (z && newDirection.Item1 != direction.Item1)
                {
                    break;
                }
            }
            else
            {
                Debug.LogWarning("Optimize rendered Contour is null");
                break;
            }

            offset += step;
        }

        if (x)
        {
            offset *= direction.Item0;
        }

        if (z)
        {
            offset *= direction.Item1;
        }

        return offset;
    }

    Vector3 OptimizeObjectPosition(GameObject obj)
    {
        // get screenshot to find the best match contour in segImage
        // render it and save to image
        var segImage = _decodedData.image;
        var screenshot = TakeSnapshot();
        // ShowMat("init screenshot", TextureToMat(screenshot));
        var curContour = GetRenderedContour(screenshot);

        if (curContour == null)
        {
            Debug.LogError("Current rendered contour is null, return without optimization.");
            return _decodedData.optimizedPosition;
        }

        var pv = FindBestMatchSegmentation(curContour);
        var scalar = new Scalar(pv);
        using var bestMatch = new Mat();
        Cv2.InRange(segImage, scalar, scalar, bestMatch);

        if (saveMedium)
        {
            // save the best match image
            SavePNG(bestMatch, GeneratePNGFilename(_decodedData.filename, "match"));
        }

        // get centroid
        var dstCentroid = GetContourCentroid(bestMatch);

        // ShowMat("best match", bestMatch);

        // get current (rendered) contour centroid
        var curCentroid = GetContourCentroid(curContour);
        // get move direction
        var direction = GetObjectMoveDirection(dstCentroid, curCentroid);

        var step = 0.10f;

        Debug.Log($"Objection position before optimizing: {_decodedData.optimizedPosition.ToString("f5")}");

        while (step > 1e-6)
        {
            // back optimized position
            var pos = _decodedData.optimizedPosition;

            // optimize x first
            var optimizedX = Optimize(obj, step, dstCentroid, direction, true, false);
            ApplyOptimization(obj, optimizedX, 0, true);

            // optimize z
            var optimizedZ = Optimize(obj, step, dstCentroid, direction, false, true);
            ApplyOptimization(obj, 0, optimizedZ, true);

            // update current centroid
            curContour = GetRenderedContour(TakeSnapshot());
            if (curContour == null)
            {
                Debug.LogWarning("Current rendered contour is null. Revert...");
                // revert
                _decodedData.optimizedPosition = pos;
                UpdateObjectPosition(obj, pos);
                curContour = GetRenderedContour(TakeSnapshot());
                curCentroid = GetContourCentroid(curContour);
            }
            else
            {
                curCentroid = GetContourCentroid(curContour);
            }

            // update direction
            direction = GetObjectMoveDirection(dstCentroid, curCentroid);

            step /= 10f;
        }

        Debug.Log($"Objection position after optimizing: {_decodedData.optimizedPosition.ToString("f5")}");

        return _decodedData.optimizedPosition;
    }

    void ApplyOptimization(GameObject obj, float xoffset, float zoffset, bool showInUnity)
    {
        // apply xOffset and optimize Z
        var pos = _decodedData.optimizedPosition;
        pos.x += xoffset;
        pos.z += zoffset;
        // update position
        _decodedData.optimizedPosition = pos;

        UpdateObjectPosition(obj, pos);
        if (showInUnity)
        {
            // update unity
            TakeSnapshot();
        }
    }

    Vec2i GetObjectMoveDirection(Vec2f dstCentroid, Vec2f curCentroid)
    {
        Vec2i result = new Vec2i(1, 1);
        var dx = dstCentroid.Item0 - curCentroid.Item0;
        var dy = dstCentroid.Item1 - curCentroid.Item1;
        if (dx < 0)
        {
            result.Item0 = -1;
        }

        if (dy > 0)
        {
            result.Item1 = -1;
        }

        return result;
    }

    float GetDistance(Vec2f dstCentroid, Vec2f curCentroid, bool x, bool z)
    {
        var vec = Vector2.zero;
        if (x)
        {
            vec.x = dstCentroid.Item0 - curCentroid.Item0;
        }

        if (z)
        {
            vec.y = dstCentroid.Item1 - curCentroid.Item1;
        }

        return vec.magnitude;
    }

    Vec2f GetContourCentroid(Mat contour)
    {
        Vec2f result = new Vec2f(0, 0);
        var M = Cv2.Moments(contour);
        if (M.M00 != 0)
        {
            result.Item0 = (float)(M.M10 / M.M00);
            result.Item1 = (float)(M.M01 / M.M00);
        }

        return result;
    }

    int FindBestMatchSegmentation(Mat rendered)
    {
        var image = _decodedData.image;
        var count = _decodedData.segCount;
        var minValue = _decodedData.minPixel;
        var maxValue = _decodedData.maxPixel;
        var areas = _decodedData.areas;


        IDictionary<int, double> similarities = new Dictionary<int, double>();

        object sync = new object();

        //Get the similarities from parallel loop
        Parallel.For(minValue, maxValue + 1, i =>
        {
            // The larger the pixel value, the large the segmentation area
            var s = new Scalar(i);

            using var dst = new Mat();
            Cv2.InRange(image, s, s, dst);

            // ShowMat("in range", dst); // 0 or 255

            // get maximized contour
            var (contour, count) = GetLargestContour(dst);

            // NOTE: here we use contours number to avoid mismatch
            if (contour != null && count == 1)
            {
                // match shape
                var ret = Cv2.MatchShapes(rendered, contour, ShapeMatchModes.I2, 0.0);

                // calculate area
                var areaRendered = Cv2.ContourArea(rendered);
                var areaSeg = Cv2.ContourArea(contour);

                Debug.Log($"Match shape result  {i} - {ret} Rendered: {areaRendered} Seg: {areaSeg} Count: {count}");

                lock (sync) similarities.Add(i, ret);
            }
        });

        // get the max and min value
        similarities = similarities.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        var min = similarities.First().Value;
        var max = similarities.Last().Value;
        var argmin = similarities.First().Key;
        var argmax = similarities.Last().Key;

        Debug.Log($"max: {max} min: {min} argmax: {argmax} argmin: {argmin}");


        return argmin;
    }

    (Mat, int) GetLargestContour(Mat img)
    {
        int count = 0;
        Mat[] contours = null;
        Mat maxContour = null;
        using var hierarchy = new Mat();
        Cv2.FindContours(img, out contours, hierarchy, RetrievalModes.External,
            ContourApproximationModes.ApproxNone);

        if (contours != null)
        {
            count = contours.Length;
        }

        double maxArea = 0.0;

        foreach (var contour in contours)
        {
            var area = Cv2.ContourArea(contour);
            if (area > maxArea)
            {
                maxArea = area;
                maxContour = contour;
            }
        }

        return (maxContour, count);
    }

    Mat GetRenderedContour(Texture2D tex)
    {
        // find contours
        Mat[] contours = null;

        using (var t = new ResourcesTracker())
        {
            // covert to mat
            var m = TextureToMat(tex);

            // get alpha channel
            var alpha = m.ExtractChannel(3);

            OutputArray hierarchy = t.NewMat();
            Cv2.FindContours(alpha, out contours, hierarchy, RetrievalModes.External,
                ContourApproximationModes.ApproxNone);
        }

        if (contours == null || contours.Length != 1)
        {
            Debug.LogError("Contours are not correct.");
            return null;
        }

        return contours[0];
    }

    Mat TextureToMat(Texture2D tex)
    {
        // include alpha channel
        Mat mat = Mat.FromImageData(tex.EncodeToPNG(), ImreadModes.Unchanged);
        return mat;
    }

    // refer: https://stackoverflow.com/questions/51586127/how-can-i-convert-opencv-mat-to-texture2d-in-unity-using-c-sharp-script
    Texture2D MatToTexture(Mat sourceMat)
    {
        //Get the height and width of the Mat 
        int imgHeight = sourceMat.Height;
        int imgWidth = sourceMat.Width;

        byte[] matData = new byte[imgHeight * imgWidth];

        //Get the byte array and store in matData
        sourceMat.GetArray(out matData);
        //Create the Color array that will hold the pixels 
        Color32[] c = new Color32[imgHeight * imgWidth];

        //Get the pixel data from parallel loop
        Parallel.For(0, imgHeight, i =>
        {
            for (var j = 0; j < imgWidth; j++)
            {
                byte vec = matData[j + i * imgWidth];
                var color32 = new Color32
                {
                    r = vec,
                    g = vec,
                    b = vec,
                    a = 0
                };
                c[j + i * imgWidth] = color32;
            }
        });

        //Create Texture from the result
        Texture2D tex = new Texture2D(imgWidth, imgHeight, TextureFormat.RGBA32, true, true);
        tex.SetPixels32(c);
        tex.Apply();
        return tex;
    }



    #endregion

    #region Initialize TCPServer/TCPClient

    #region Incoming

    void InitializeWebsocketServer()
    {
        webSocketServer = new WebSocketServer(incomingPort);
        webSocketServer.Log.Level = LogLevel.Trace;
        webSocketServer.AddWebSocketService<ReceivingServer>("/");
        webSocketServer.Start();
    }

    void InitializeIncomingServer()
    {
        // TCP
        incoming.OnOpen(() =>
        {
            Debug.LogWarning("Incoming Server OnOpen: " + incoming.Host.ToString());

            var client = new TcpClient();
            client.Open(incomingHost);
            //
            client.ToEvent("event", "data");
        });

        incoming.OnError((e) => { Debug.LogError("Incoming Server OnError: " + e.ToString()); });

        incoming.OnClose(() => { Debug.LogWarning("Incoming Server OnClose"); });

        incoming.OnEvent((client, name, data) =>
        {
            Debug.Log(name);
            Debug.Log(data);
        });

        incoming.OnExit((client) => { Debug.LogWarning("Incoming OnExit"); });

        // open incoming server
        incoming.Open(incomingHost);
    }

    #endregion

    #region Outgoing

    void InitializeOutgoingClient()
    {
        outgoingHost = new Host(outgoingIP, outgoingPort);

        outgoing = new TcpClient();
        // TCP
        outgoing.OnOpen(() => { Debug.LogWarning("Outgoing Client OnOpen: " + incoming.Host.ToString()); });

        // outgoing.OnError(() => { Debug.LogError("Outgoing Client OnError") };

        outgoing.OnClose(() => { Debug.LogWarning("Outgoing Client OnClose"); });

        outgoing.OnEvent((name, data) =>
        {
            Debug.Log(name);
            Debug.Log(data);
        });

        // open outgoing server
        outgoing.Open(outgoingHost);
    }


    #endregion

    #region Send Registration Result to PC

    void SendRegistrationResult(string objname, int objectid)
    {
        if (outgoing == null || !outgoing.IsOpened)
        {
            Debug.LogError("Outgoing client is not available.");
            return;
        }

        var name = objname;
        var oid = objectid;
        var pos = _decodedData.optimizedPosition;

        using Writer w = new Writer();
        w.Write(oid);
        w.Write(name);
        w.Write(pos.x);
        w.Write(pos.y);
        w.Write(pos.z);

        outgoing.ToEvent(NetlyEventTypes.Registration, w.GetBytes());

        Debug.LogError("Sent registration result data.");
    }

    #endregion



    #endregion

    #region Setup Camera and Object Transform for Registration

    void UpdateCamera()
    {
        var cam = Camera.main;
        // camera matrix
        cam.projectionMatrix = _decodedData.projectionMatrix;

        Debug.Log("projection matrix");
        Debug.Log(cam.projectionMatrix);

        // camera position and rotation
        cam.transform.position = _decodedData.camPosition;
        cam.transform.rotation = Quaternion.Euler(_decodedData.camRotation);
    }

    GameObject FindObjectByID(int id)
    {
        foreach (var objcet in objects)
        {
            if (objcet.id == id)
                return objcet.gameObject;
        }

        return null;
    }

    void SetObjectsActiveness(int id)
    {
        foreach (var objcet in objects)
        {
            if (objcet.id == id)
            {
                // objcet.enable = true;
                objcet.gameObject.SetActive(true);
            }
            else
            {
                // objcet.enable = false;
                objcet.gameObject.SetActive(false);
            }
        }
    }

    void InitializeObject(GameObject obj)
    {
        // // find object by name
        // var name = _decodedData.objName;
        // // find object by id
        // var obj = FindObjectByID(_decodedData.objID);

        if (obj != null)
        {
            // obj.transform.position = _decodedData.objPosition;
            // initialize using the plane position
            obj.transform.position = _decodedData.planePosition;
            // WARNING: DON'T change the object rotation
            // obj.transform.rotation = Quaternion.Euler(_decodedData.objRotation);
            // WARNING: DON'T change the object scale
            // obj.transform.localScale = _decodedData.objScale;
        }
    }

    void UpdateObjectPosition(GameObject obj, Vector3 position)
    {
        // var name = _decodedData.objName;
        // find object by id
        // var obj = FindObjectByID(_decodedData.objID);
        if (obj != null)
        {
            obj.transform.position = position;
        }
    }

    #endregion

    #region Render as Image

    private Texture2D TakeSnapshot(int mWidth, int mHeight)
    {
        var cam = Camera.main;
        // mWidth = cam.targetTexture.width;
        // mHeight = cam.targetTexture.height;

        Rect rect = new Rect(0, 0, mWidth, mHeight);
        RenderTexture renderTexture = new RenderTexture(mWidth, mHeight, 24);
        Texture2D screenShot = new Texture2D(mWidth, mHeight, TextureFormat.RGBA32, false);

        cam.targetTexture = renderTexture;
        cam.Render();

        RenderTexture.active = renderTexture;
        screenShot.ReadPixels(rect, 0, 0);
        screenShot.Apply(false);

        cam.targetTexture = null;
        RenderTexture.active = null;

        Destroy(renderTexture);
        renderTexture = null;
        return screenShot;
    }

    private Texture2D TakeSnapshot()
    {
        var size = _decodedData.imageSize;
        return TakeSnapshot(size.Width, size.Height);
    }

    internal string GetImageDirectory()
    {
        return Directory.CreateDirectory(Path.Combine(Application.dataPath, "../Snapshots")).FullName;
    }

    public FileInfo SavePNG(byte[] bytes)
    {

        var filename = System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ffff") + ".png";
        return SavePNG(bytes, filename);
    }

    public FileInfo SavePNG(byte[] bytes, string filename)
    {
        string filepath = Path.Combine(GetImageDirectory(), filename);

        File.WriteAllBytes(filepath, bytes);

        Debug.LogWarning($"Saved file to {filepath}");

        return new FileInfo(filepath);
    }

    public FileInfo SavePNG(Mat mat, string filename)
    {
        string filepath = Path.Combine(GetImageDirectory(), filename);

        Cv2.ImWrite(filepath, mat);

        Debug.LogWarning($"Saved file to {filepath}");

        return new FileInfo(filepath);
    }

    #endregion

    // Update is called once per frame
    void Update()
    {
        while (_mainThreadWorkQueue.TryDequeue(out Action workload))
        {
            workload();
        }
    }
}
#endif