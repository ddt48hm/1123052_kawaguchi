using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;

namespace Microsoft.Samples.Kinect.ColorBasics
{
    class dotMath
    {
        public static float Inner_Product(Skeleton skeleton, JointType j1, JointType j2, JointType j3)
        {
            Vector4 vec1, vec2;

            vec1 = new Vector4();

            vec2 = new Vector4();

            // j2 - j1の三次元座標を取得
            vec1.X = skeleton.Joints[j2].Position.X - skeleton.Joints[j1].Position.X;

            vec1.Y = skeleton.Joints[j2].Position.Y - skeleton.Joints[j1].Position.Y;

            vec1.Z = skeleton.Joints[j2].Position.Z - skeleton.Joints[j1].Position.Z;

            // j2 - j3の三次元座標を取得
            vec2.X = skeleton.Joints[j2].Position.X - skeleton.Joints[j3].Position.X;

            vec2.Y = skeleton.Joints[j2].Position.Y - skeleton.Joints[j3].Position.Y;

            vec2.Z = skeleton.Joints[j2].Position.Z - skeleton.Joints[j3].Position.Z;

            // 内積の計算
            float AA, BB, AB;

            AA = vec1.X * vec1.X + vec1.Y * vec1.Y + vec1.Z * vec1.Z;

            BB = vec2.X * vec2.X + vec2.Y * vec2.Y + vec2.Z * vec2.Z;

            AB = vec1.X * vec2.X + vec1.Y * vec2.Y + vec1.Z * vec2.Z;

            //大きさ
            return (float)(AB / (System.Math.Sqrt(AA) * System.Math.Sqrt(BB)));
        }
    }
}
