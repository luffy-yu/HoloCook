//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using UnityEngine;


// Unity is left-handed, while OpenGL is right-handed
namespace HoloCook.Utility
{
    public static class UnityOpenGLUtils
    {
        public static readonly string strFormat = "f7";

        public static readonly string key_objName = "objName";
        public static readonly string key_objID = "objID";

        public static readonly string key_position = "position";
        public static readonly string key_rotation = "rotation";
        public static readonly string key_scale = "scale";
        public static readonly string key_planePos = "planePosition";
        public static readonly string key_planeRot = "planeRotation";

        public static Vector3 convertPositionU2O(Vector3 pos)
        {
            pos.z *= -1;
            return pos;
        }
    
        // get camera translation and rotation from cameraToWorldMatrix
        public static (Vector3, Vector3) GetCameraTRFromC2W(Matrix4x4 c2w)
        {
            // translation
            var t = c2w.MultiplyPoint(Vector3.zero);

            // rotation
            var worldToCamera = c2w.inverse;
            worldToCamera.m20 *= -1f;
            worldToCamera.m21 *= -1f;
            worldToCamera.m22 *= -1f;
            worldToCamera.m23 *= -1f;

            // camera rotation
            var r = worldToCamera.inverse.rotation.eulerAngles;

            return (t, r);
        }

        public static Quaternion convertRotationU2O(Quaternion rot)
        {
            var newRotation = new Quaternion();
            newRotation.x = -rot.x;
            newRotation.y = -rot.y;
            newRotation.z = rot.z;
            newRotation.w = rot.w;

            return newRotation;
        }

        // covert object position, the output can be directly used in OpenGL
        // c2w: cameraToWorldMatrix
        public static Vector3 convertPositionU2O(Matrix4x4 c2w, Vector3 pos)
        {
            var t = c2w.inverse.MultiplyPoint(pos);
            return t;
        }

        // covert object rotation, the output can be directly used in OpenGL
        // c2w: cameraToWorldMatrix
        public static Quaternion convertRotationU2O(Matrix4x4 c2w, Quaternion rot)
        {
            // Get the real camera rotation from the cameraToWorldMatrix
            // refer: https://forum.unity.com/threads/reproducing-cameras-worldtocameramatrix.365645/#post-2367177
            var worldToCamera = c2w.inverse;
            worldToCamera.m20 *= -1f;
            worldToCamera.m21 *= -1f;
            worldToCamera.m22 *= -1f;
            worldToCamera.m23 *= -1f;

            // camera rotation
            var r = worldToCamera.inverse.rotation;

            var t = Quaternion.Inverse(r) * rot;
            rot.x = -t.x;
            rot.y = -t.y;
            rot.z = t.z;
            rot.w = t.w;

            return rot;
        }

        public static Matrix4x4 covertCameraToWorldMatrixU2O(Matrix4x4 mat)
        {
            // decompose
            var position = mat.GetColumn(3);
            var rotation = Quaternion.LookRotation(mat.GetColumn(2), mat.GetColumn(1));

            var scale = new Vector3(mat.GetColumn(0).magnitude,
                mat.GetColumn(1).magnitude,
                mat.GetColumn(2).magnitude);

            // update rotation
            var newRotation = convertRotationU2O(rotation);

            // compose
            return Matrix4x4.TRS(position, newRotation, scale);
            ;
        }
    }
}