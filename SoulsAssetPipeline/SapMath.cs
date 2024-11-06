using Assimp;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMatrix = System.Numerics.Matrix4x4;
using NVector3 = System.Numerics.Vector3;
using NVector4 = System.Numerics.Vector4;
using NQuaternion = System.Numerics.Quaternion;

namespace SoulsAssetPipeline
{
    public static class SapMath
    {
        public const float E = (float)Math.E;
        public const float Log10E = 0.4342945f;
        public const float Log2E = 1.442695f;
        public const float Pi = (float)Math.PI;
        public const float PiOver2 = (float)(Math.PI / 2.0);
        public const float PiOver4 = (float)(Math.PI / 4.0);
        public const float TwoPi = (float)(Math.PI * 2.0);
        public const float Rad2Deg = 180.0f / Pi;
        public const float Deg2Rad = Pi / 180.0f;

        public static NMatrix ZUpToYUpNMatrix => NMatrix.CreateRotationZ(Pi) * NMatrix.CreateRotationX(-PiOver2);

        public static NQuaternion CustomQuatSlerp(NQuaternion v0, NQuaternion v1, float t)
        {
            v0 = NQuaternion.Normalize(v0);
            v1 = NQuaternion.Normalize(v1);

            float dot = NQuaternion.Dot(v0, v1);

            if (dot < 0)
            {
                v1 = -v1;
                dot = -dot;
            }

            const double DOT_THRESH = 0.9995;
            if (dot > DOT_THRESH)
            {
                var diff = (v1 - v0);
                diff.X *= t;
                diff.Y *= t;
                diff.Z *= t;
                diff.W *= t;
                return NQuaternion.Normalize(v0 + diff);
            }

            // Since dot is in range [0, DOT_THRESHOLD], acos is safe
            float theta_0 = (float)Math.Acos(dot);        // theta_0 = angle between input vectors
            float theta = theta_0 * t;          // theta = angle between v0 and result
            float sin_theta = (float)Math.Sin(theta);     // compute this value only once
            float sin_theta_0 = (float)Math.Sin(theta_0); // compute this value only once

            float s0 = (float)Math.Cos(theta) - dot * sin_theta / sin_theta_0;  // == sin(theta_0 - theta) / sin(theta_0)
            float s1 = sin_theta / sin_theta_0;

            var a = v0;
            a.X *= s0;
            a.Y *= s0;
            a.Z *= s0;
            a.W *= s0;

            var b = v1;
            b.X *= s1;
            b.Y *= s1;
            b.Z *= s1;
            b.W *= s1;

            return a + b;
        }

        public static NQuaternion GetDeltaQuaternionWithDirectionVectors(NVector3 a, NVector3 b)
        {
            var r = NVector3.Dot(a, b) + 1;
            if (r < 0.000001)
            {
                r = 0;
                if (Math.Abs(a.X) > Math.Abs(a.Z))
                {
                    var xyz = new NVector3(-a.Y, a.X, 0);
                    return NQuaternion.Normalize(new NQuaternion(xyz.X, xyz.Y, xyz.Z, r));
                }
                else
                {
                    var xyz = new NVector3(0, -a.Z, a.Y);
                    return NQuaternion.Normalize(new NQuaternion(xyz.X, xyz.Y, xyz.Z, r));
                }
            }
            else
            {
                var xyz = NVector3.Cross(a, b);
                return NQuaternion.Normalize(new NQuaternion(xyz.X, xyz.Y, xyz.Z, r));
            }
        }

        public static NQuaternion GetDeltaQuaternionWithDirectionVectors(NQuaternion from, NQuaternion to)
        {
            var a = NVector3.Transform(NVector3.UnitZ, NMatrix.CreateFromQuaternion(from));
            var b = NVector3.Transform(NVector3.UnitZ, NMatrix.CreateFromQuaternion(to));

            var dot = NVector3.Dot(a, b);
            if (dot < -0.999999)
            {
                var cross = NVector3.Cross(a, b);
                if (cross.Length() < 0.000001)
                    cross = NVector3.Cross(NVector3.UnitY, a);
                cross = NVector3.Normalize(cross);
                return NQuaternion.CreateFromAxisAngle(cross, Pi);
            }
            else if (dot > 0.999999)
            {
                return new NQuaternion(0, 0, 0, 1);
            }
            else
            {
                var xyz = NVector3.Cross(a, b);
                var w = (float)(Math.Sqrt(a.Length() * a.Length() + b.Length() * b.Length()) + dot);
                return new NQuaternion(xyz.X, xyz.Y, xyz.Z, w);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Adapted from https://stackoverflow.com/a/4341489/1890257
        ////////////////////////////////////////////////////////////////////////////////
        
        private static NMatrix OrthoX = NMatrix.CreateRotationX(PiOver2);
        private static NMatrix OrthoY = NMatrix.CreateRotationY(PiOver2);
        public static void FindOrthonormals(NVector3 normal, out NVector3 orthonormal1, out NVector3 orthonormal2)
        {
            NVector3 w = NVector3.Transform(normal, OrthoX);
            float dot = NVector3.Dot(normal, w);
            if (Math.Abs(dot) > 0.6)
            {
                w = NVector3.Transform(normal, OrthoY);
            }
            w = NVector3.Normalize(w);

            orthonormal1 = NVector3.Cross(normal, w);
            orthonormal1 = NVector3.Normalize(orthonormal1);
            orthonormal2 = NVector3.Cross(normal, orthonormal1);
            orthonormal2 = NVector3.Normalize(orthonormal2);
        }

        public static float FindQuaternionTwist(NQuaternion q, NVector3 axis)
        {
            axis = NVector3.Normalize(axis);

            // Get the plane the axis is a normal of
            NVector3 orthonormal1, orthonormal2;
            FindOrthonormals(axis, out orthonormal1, out orthonormal2);

            NVector3 transformed = NVector3.Transform(orthonormal1, q);

            // Project transformed vector onto plane
            NVector3 flattened = transformed - (NVector3.Dot(transformed, axis) * axis);
            flattened = NVector3.Normalize(flattened);

            // Get angle between original vector and projected transform to get angle around normal
            float a = (float)Math.Acos((double)NVector3.Dot(orthonormal1, flattened));

            return a;
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////


        public static NVector4 QuaternionToAxisAngle(NQuaternion quat)
        {
            if (quat.W > 1)
                quat = NQuaternion.Normalize(quat);

            var result = new NVector4(quat.X, quat.Y, quat.Z, 2 * (float)Math.Acos(quat.W));

            var s = (float)Math.Sqrt(1 - (quat.W * quat.W));
            // For unavoidable divide by 0 singularity, just return XYZ as is since direction doesn't really matter.
            if (s >= 0.001f)
            {
                result.X /= s;
                result.Y /= s;
                result.Z /= s;
            }

            return result;
        }

        public static NQuaternion MirrorQuat(NQuaternion quatB)
        {
            var angle = 2 * Math.Acos(quatB.W);
            var s2 = Math.Sqrt(1.0 - quatB.W * quatB.W);
            NVector3 axis;
            if (s2 < 0.001)
            {
                axis.X = quatB.X;
                axis.Y = quatB.Y;
                axis.Z = quatB.Z;
            }
            else
            {
                axis.X = (float)(quatB.X / s2);
                axis.Y = (float)(quatB.Y / s2);
                axis.Z = (float)(quatB.Z / s2);
            }
            axis.X = -axis.X;
            return NQuaternion.CreateFromAxisAngle(axis, (float)-angle);
        }

        public enum EulerOrder
        {
            ZYX, ZYZ, ZXY, ZXZ, YXZ, YXY, YZX, YZY, XYZ, XYX, XZY, XZX
        };

        static NVector3 twoaxisrot(float r11, float r12, float r21, float r31, float r32)
        {
            NVector3 ret = new NVector3();
            ret.X = (float)Math.Atan2(r11, r12);
            ret.Y = (float)Math.Acos(r21);
            ret.Z = (float)Math.Atan2(r31, r32);
            return ret;
        }

        static NVector3 threeaxisrot(float r11, float r12, float r21, float r31, float r32)
        {
            NVector3 ret = new NVector3();
            ret.X = (float)Math.Atan2(r31, r32);
            ret.Y = (float)Math.Asin(r21);
            ret.Z = (float)Math.Atan2(r11, r12);
            return ret;
        }

        static NVector3 _quaternion2Euler(Quaternion q, EulerOrder rotSeq)
        {
            switch (rotSeq)
            {
                case EulerOrder.ZYX:
                    return threeaxisrot(2 * (q.X * q.Y + q.W * q.Z),
                        q.W * q.W + q.X * q.X - q.Y * q.Y - q.Z * q.Z,
                        -2 * (q.X * q.Z - q.W * q.Y),
                        2 * (q.Y * q.Z + q.W * q.X),
                        q.W * q.W - q.X * q.X - q.Y * q.Y + q.Z * q.Z);


                case EulerOrder.ZYZ:
                    return twoaxisrot(2 * (q.Y * q.Z - q.W * q.X),
                        2 * (q.X * q.Z + q.W * q.Y),
                        q.W * q.W - q.X * q.X - q.Y * q.Y + q.Z * q.Z,
                        2 * (q.Y * q.Z + q.W * q.X),
                        -2 * (q.X * q.Z - q.W * q.Y));


                case EulerOrder.ZXY:
                    return threeaxisrot(-2 * (q.X * q.Y - q.W * q.Z),
                        q.W * q.W - q.X * q.X + q.Y * q.Y - q.Z * q.Z,
                        2 * (q.Y * q.Z + q.W * q.X),
                        -2 * (q.X * q.Z - q.W * q.Y),
                        q.W * q.W - q.X * q.X - q.Y * q.Y + q.Z * q.Z);


                case EulerOrder.ZXZ:
                    return twoaxisrot(2 * (q.X * q.Z + q.W * q.Y),
                        -2 * (q.Y * q.Z - q.W * q.X),
                        q.W * q.W - q.X * q.X - q.Y * q.Y + q.Z * q.Z,
                        2 * (q.X * q.Z - q.W * q.Y),
                        2 * (q.Y * q.Z + q.W * q.X));


                case EulerOrder.YXZ:
                    return threeaxisrot(2 * (q.X * q.Z + q.W * q.Y),
                        q.W * q.W - q.X * q.X - q.Y * q.Y + q.Z * q.Z,
                        -2 * (q.Y * q.Z - q.W * q.X),
                        2 * (q.X * q.Y + q.W * q.Z),
                        q.W * q.W - q.X * q.X + q.Y * q.Y - q.Z * q.Z);

                case EulerOrder.YXY:
                    return twoaxisrot(2 * (q.X * q.Y - q.W * q.Z),
                        2 * (q.Y * q.Z + q.W * q.X),
                        q.W * q.W - q.X * q.X + q.Y * q.Y - q.Z * q.Z,
                        2 * (q.X * q.Y + q.W * q.Z),
                        -2 * (q.Y * q.Z - q.W * q.X));


                case EulerOrder.YZX:
                    return threeaxisrot(-2 * (q.X * q.Z - q.W * q.Y),
                        q.W * q.W + q.X * q.X - q.Y * q.Y - q.Z * q.Z,
                        2 * (q.X * q.Y + q.W * q.Z),
                        -2 * (q.Y * q.Z - q.W * q.X),
                        q.W * q.W - q.X * q.X + q.Y * q.Y - q.Z * q.Z);


                case EulerOrder.YZY:
                    return twoaxisrot(2 * (q.Y * q.Z + q.W * q.X),
                        -2 * (q.X * q.Y - q.W * q.Z),
                        q.W * q.W - q.X * q.X + q.Y * q.Y - q.Z * q.Z,
                        2 * (q.Y * q.Z - q.W * q.X),
                        2 * (q.X * q.Y + q.W * q.Z));


                case EulerOrder.XYZ:
                    return threeaxisrot(-2 * (q.Y * q.Z - q.W * q.X),
                        q.W * q.W - q.X * q.X - q.Y * q.Y + q.Z * q.Z,
                        2 * (q.X * q.Z + q.W * q.Y),
                        -2 * (q.X * q.Y - q.W * q.Z),
                        q.W * q.W + q.X * q.X - q.Y * q.Y - q.Z * q.Z);


                case EulerOrder.XYX:
                    return twoaxisrot(2 * (q.X * q.Y + q.W * q.Z),
                        -2 * (q.X * q.Z - q.W * q.Y),
                        q.W * q.W + q.X * q.X - q.Y * q.Y - q.Z * q.Z,
                        2 * (q.X * q.Y - q.W * q.Z),
                        2 * (q.X * q.Z + q.W * q.Y));


                case EulerOrder.XZY:
                    return threeaxisrot(2 * (q.Y * q.Z + q.W * q.X),
                        q.W * q.W - q.X * q.X + q.Y * q.Y - q.Z * q.Z,
                        -2 * (q.X * q.Y - q.W * q.Z),
                        2 * (q.X * q.Z + q.W * q.Y),
                        q.W * q.W + q.X * q.X - q.Y * q.Y - q.Z * q.Z);


                case EulerOrder.XZX:
                    return twoaxisrot(2 * (q.X * q.Z - q.W * q.Y),
                        2 * (q.X * q.Y + q.W * q.Z),
                        q.W * q.W + q.X * q.X - q.Y * q.Y - q.Z * q.Z,
                        2 * (q.X * q.Z + q.W * q.Y),
                        -2 * (q.X * q.Y - q.W * q.Z));

                default:
                    return NVector3.Zero;

            }
        }

        public static NVector3 QuaternionToEuler_Legacy(Quaternion q)
        {
            // Store the Euler angles in radians
            NVector3 pitchYawRoll = new NVector3();

            double sqw = q.W * q.W;
            double sqx = q.X * q.X;
            double sqy = q.Y * q.Y;
            double sqz = q.Z * q.Z;

            // If quaternion is normalised the unit is one, otherwise it is the correction factor
            double unit = sqx + sqy + sqz + sqw;
            double test = q.X * q.Y + q.Z * q.W;

            if (test > 0.4995f * unit)                              // 0.4999f OR 0.5f - EPSILON
            {
                // Singularity at north pole
                pitchYawRoll.Y = 2f * (float)Math.Atan2(q.X, q.W);  // Yaw
                pitchYawRoll.Z = SapMath.Pi * 0.5f;                 // Pitch
                pitchYawRoll.X = 0f;                                // Roll
                return pitchYawRoll;
            }
            else if (test < -0.4995f * unit)                        // -0.4999f OR -0.5f + EPSILON
            {
                // Singularity at south pole
                pitchYawRoll.Y = -2f * (float)Math.Atan2(q.X, q.W); // Yaw
                pitchYawRoll.Z = -SapMath.Pi * 0.5f;                // Pitch
                pitchYawRoll.X = 0f;                                // Roll
                return pitchYawRoll;
            }
            else
            {
                pitchYawRoll.Y = (float)Math.Atan2(2f * q.Y * q.W - 2f * q.X * q.Z, sqx - sqy - sqz + sqw);      // Yaw
                pitchYawRoll.Z = (float)Math.Asin(2f * test / unit);                                             // Pitch
                pitchYawRoll.X = (float)Math.Atan2(2f * q.X * q.W - 2f * q.Y * q.Z, -sqx + sqy - sqz + sqw);     // Roll
            }

            return pitchYawRoll;
        }

        public static NVector3 QuaternionToEuler(Quaternion q, EulerOrder rotSeq)
        {
            NVector3 res = _quaternion2Euler(q, rotSeq);
            var result = new NVector3();
            float test = q.W * q.Z + q.X * q.Y;
            float unit = q.X * q.X + q.Y * q.Y + q.Z * q.Z + q.W * q.W;
            switch (rotSeq)
            {
                case EulerOrder.ZYX:
                    result.X = res.X;
                    result.Y = res.Y;
                    result.Z = res.Z;
                    break;

                case EulerOrder.ZXY:
                    result.X = res.Y;
                    result.Y = res.X;
                    result.Z = res.Z;
                    break;

                case EulerOrder.YXZ:
                    result.X = res.Z;
                    result.Y = res.X;
                    result.Z = res.Y;
                    break;

                case EulerOrder.YZX:
                    result.X = res.X;
                    result.Y = res.Z;
                    result.Z = res.Y;
                    // Handle poles
                    if (test > 0.4995f * unit)
                    {
                        result.X = 0.0f;
                        result.Y = 2.0f * (float)Math.Atan2(q.Y, q.Z);
                        result.Z = 90.0f * Deg2Rad;
                    }
                    if (test < -0.4995f * unit)
                    {
                        result.X = 0.0f;
                        result.Y = -2.0f * (float)Math.Atan2(q.Y, q.Z);
                        result.Z = -90.0f * Deg2Rad;
                    }
                    break;

                case EulerOrder.XYZ:
                    result.X = res.Z;
                    result.Y = res.Y;
                    result.Z = res.X;
                    break;

                case EulerOrder.XZY:
                    result.X = res.Y;
                    result.Y = res.Z;
                    result.Z = res.X;
                    // Handle poles
                    if (test > 0.4995f * unit)
                    {
                        result.X = -90.0f * Deg2Rad;
                        result.Y = -2.0f * (float)Math.Atan2(q.Y, q.Z);
                        result.Z = 0;
                    }
                    if (test < -0.4995f * unit)
                    {
                        result.X = 0.0f;
                        result.Y = 2.0f * (float)Math.Atan2(q.Y, q.Z);
                        result.Z = -90.0f * Deg2Rad;
                    }
                    break;

                default:
                    return System.Numerics.Vector3.Zero;
            }
            result.X = (result.X <= -180.0f * Deg2Rad) ? result.X + 360.0f * Deg2Rad : result.X;
            result.Y = (result.Y <= -180.0f * Deg2Rad) ? result.Y + 360.0f * Deg2Rad : result.Y;
            result.Z = (result.Z <= -180.0f * Deg2Rad) ? result.Z + 360.0f * Deg2Rad : result.Z;
            return result;


        }


        public static float Clamp(float value, float min, float max)
        {
            value = (value > max) ? max : value;
            value = (value < min) ? min : value;
            return value;
        }

        public static NVector3 MatrixToEulerXZY(NMatrix m)
        {
            NVector3 ret;
            ret.Z = (float)Math.Asin(-SapMath.Clamp(-m.M12, -1, 1));

            if (Math.Abs(m.M12) < 0.9999999)
            {

                ret.X = (float)Math.Atan2(-m.M32, m.M22);
                ret.Y = (float)Math.Atan2(-m.M13, m.M11);

            }
            else
            {

                ret.X = (float)Math.Atan2(m.M23, m.M33);
                ret.Y = 0;

            }

            ret.X = (ret.X <= -180.0f * Deg2Rad) ? ret.X + 360.0f * Deg2Rad : ret.X;
            ret.Y = (ret.Y <= -180.0f * Deg2Rad) ? ret.Y + 360.0f * Deg2Rad : ret.Y;
            ret.Z = (ret.Z <= -180.0f * Deg2Rad) ? ret.Z + 360.0f * Deg2Rad : ret.Z;

            return ret;
        }

        public static NVector3 XYZ(this System.Numerics.Vector4 v)
        {
            return new NVector3(v.X, v.Y, v.Z);
        }

        public static float Lerp(float a, float b, float s)
        {
            return a + ((b - a) * s);
        }

        public static FLVER.VertexColor ToFlverVertexColor(this Color4D c)
        {
            return new FLVER.VertexColor(c.A, c.R, c.G, c.B);
        }

        public static NVector3 ToNumerics(this Vector3D v)
        {
            return new NVector3(v.X, v.Y, v.Z);
        }

        public static NQuaternion ToNumerics(this Quaternion q)
        {
            return new NQuaternion(q.X, q.Y, q.Z, q.W);
        }
    }
}
