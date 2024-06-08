//  
// Copyright (c) 2024 Liuchuan Yu. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Vector3 = UnityEngine.Vector3;

namespace HoloCook.Algorithm
{
    public class TrajectoryFitting : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {

            // TestProjection();

            // TestUnityVersion();
        }

        // Update is called once per frame
        void Update()
        {

        }

        #region Unity version in use

        public List<Vector3> TransformTrajectory(List<Vector3> points, Vector3 targetstart, Vector3 targetend)
        {
            List<Vector3> result = new List<Vector3>();
            var count = points.Count;
            // switch y and z
            var pointsTemp = new List<Vector3>();

            for (var i = 0; i < count; i++)
            {
                var p = points[i];
                var z = p.z;
                p.z = p.y;
                p.y = z;

                pointsTemp.Add(p);
            }

            var srcStart = pointsTemp[0];
            var srcEnd = pointsTemp[count - 1];

            // switch target also
            var targetStart = new Vector3(targetstart.x, targetstart.z, targetstart.y);
            var targetEnd = new Vector3(targetend.x, targetend.z, targetend.y);

            // get transform
            var mat = GetTransform(srcStart.x, srcStart.y, srcEnd.x, srcEnd.y,
                targetStart.x, targetStart.y, targetEnd.x, targetEnd.y);
            print(mat);

            // backup src zs
            List<float> zs = new List<float>();
            for (var i = 0; i < count; i++)
            {
                zs.Add(pointsTemp[i].z);
            }

            // transform
            for (var i = 0; i < count; i++)
            {
                var p = pointsTemp[i];
                // set z to 1
                p.z = 1;

                var res = mat * p;

                // revert z
                res.z = zs[i];

                // switch y and z back
                res.z = res.y;
                res.y = zs[i];

                result.Add(res);
            }

            return result;
        }

        void TestUnityVersion()
        {
            List<Vector3> src = new List<Vector3>();
            for (var y = -5; y <= 5; y++)
            {
                src.Add(new Vector3(0, (float)-Math.Pow(y, 2) + 25, y));
            }

            Vector3 target1 = new Vector3(-1, 0, -4);
            Vector3 target2 = new Vector3(1, 0, 8);

            var result = TransformTrajectory(src, target1, target2);
            for (var i = 0; i < result.Count; i++)
            {
                print(result[i]);
            }
        }


        #endregion

        #region Adaption for python

        Matrix4x4 GetTransform(float xa, float ya, float xa1, float ya1, float xb, float yb, float xb1, float yb1)
        {
            var ax = xa1 - xa;
            var ay = ya1 - ya;
            var a2 = Math.Pow(ax, 2) + Math.Pow(ay, 2);
            var bx = xb1 - xb;
            var by = yb1 - yb;
            var ux = (ax * bx + ay * by) / a2;
            var uy = (ax * by - ay * bx) / a2;
            var tx = xb - (xa * (ax * bx + ay * by) + ya * (ay * bx - ax * by)) / a2;
            var ty = yb - (xa * (ax * by - ay * bx) + ya * (ax * bx + ay * by)) / a2;

            Matrix4x4 mat = new Matrix4x4();
            mat.m00 = (float)ux;
            mat.m01 = (float)-uy;
            mat.m02 = (float)tx;
            mat.m03 = 0;

            mat.m10 = (float)uy;
            mat.m11 = (float)ux;
            mat.m12 = (float)ty;
            mat.m13 = 0;

            mat.m20 = 0;
            mat.m21 = 0;
            mat.m22 = 1;
            mat.m23 = 0;

            mat.m30 = 0;
            mat.m31 = 0;
            mat.m32 = 0;
            mat.m33 = 0;

            return mat;
        }

        void TestProjection()
        {
            Vector3 source1 = new Vector3(0, 5, 0);
            Vector3 source2 = new Vector3(0, -5, 0);

            Vector3 target1 = new Vector3(-1, -4, 0);
            Vector3 target2 = new Vector3(1, 8, 0);

            var mat = GetTransform(source1.x, source1.y, source2.x, source2.y, target1.x, target1.y,
                target2.x, target2.y);

            print(mat);

            var vec = mat * (new UnityEngine.Vector3(source1.x, source1.y, 1));

            print(vec);

            List<Vector3> srcZ = new List<Vector3>();
            List<Vector3> src1 = new List<Vector3>();
            for (var y = -5; y <= 5; y++)
            {
                srcZ.Add(new Vector3(0, y, (float)-Math.Pow(y, 2) + 25));
                src1.Add(new Vector3(0, y, 1));
            }

            List<Vector3> res = new List<Vector3>();
            for (var i = 0; i < src1.Count; i++)
            {
                res.Add(mat * src1[i]);
            }

            // revert z
            for (var i = 0; i < res.Count; i++)
            {
                var t = res[i];
                t.z = srcZ[i].z;
                res[i] = t;
            }

            // print
            for (var i = 0; i < res.Count; i++)
            {
                print(res[i]);
            }
        }

        #endregion
    }
}
