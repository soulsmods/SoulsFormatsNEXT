using System.Numerics;

namespace SoulsFormats
{
    public partial class SMD4
    {
        /// <summary>
        /// A joint available for vertices to be attached to.
        /// </summary>
        public class Bone
        {
            /// <summary>
            /// Corresponds to the name of a bone in the parent skeleton, if present.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Translation of this bone.
            /// </summary>
            public Vector3 Translation { get; set; }

            /// <summary>
            /// Rotation of this bone; euler radians in XZY order.
            /// </summary>
            public Vector3 Rotation { get; set; }

            /// <summary>
            /// Scale of this bone.
            /// </summary>
            public Vector3 Scale { get; set; }

            /// <summary>
            /// Minimum extent of the vertices weighted to this bone.
            /// </summary>
            public Vector3 BoundingBoxMin { get; set; }

            /// <summary>
            /// Maximum extent of the vertices weighted to this bone.
            /// </summary>
            public Vector3 BoundingBoxMax { get; set; }

            /// <summary>
            /// Index of the parent in this FLVER's bone collection, or -1 for none.
            /// </summary>
            public short ParentIndex { get; set; }

            /// <summary>
            /// Index of the first child in this FLVER's bone collection, or -1 for none.
            /// </summary>
            public short ChildIndex { get; set; }

            /// <summary>
            /// Index of the next child of this bone's parent, or -1 for none.
            /// </summary>
            public short NextSiblingIndex { get; set; }

            /// <summary>
            /// Index of the previous child of this bone's parent, or -1 for none.
            /// </summary>
            public short PreviousSiblingIndex { get; set; }

            /// <summary>
            /// Unknown; Only seen as zero.
            /// </summary>
            public int Unk64 { get; set; }

            /// <summary>
            /// Unknown; Only seen as zero.
            /// </summary>
            public int Unk68 { get; set; }

            /// <summary>
            /// Unknown; Only seen as zero.
            /// </summary>
            public int Unk6C { get; set; }

            /// <summary>
            /// Unknown array of 8 indices; Only seen as -1 for each.
            /// </summary>
            public int[] Unk70 { get; private set; }

            /// <summary>
            /// Create a new Bone with default values.
            /// </summary>
            public Bone()
            {
                Name = "";
                Scale = Vector3.One;
                ParentIndex = -1;
                ChildIndex = -1;
                NextSiblingIndex = -1;
                PreviousSiblingIndex = -1;
                Unk64 = 0;
                Unk68 = 0;
                Unk6C = 0;
                Unk70 = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
            }

            /// <summary>
            /// Clone an existing Bone.
            /// </summary>
            public Bone(Bone bone)
            {
                Name = bone.Name;
                Translation = bone.Translation;
                Rotation = bone.Rotation;
                Scale = bone.Scale;
                BoundingBoxMin = bone.BoundingBoxMin;
                BoundingBoxMax = bone.BoundingBoxMax;
                ParentIndex = bone.ParentIndex;
                ChildIndex = bone.ChildIndex;
                NextSiblingIndex = bone.NextSiblingIndex;
                PreviousSiblingIndex = bone.PreviousSiblingIndex;
                Unk64 = bone.Unk64;
                Unk68 = bone.Unk68;
                Unk6C = bone.Unk6C;
                Unk70 = new int[8];
                for (int i = 0; i < 8; i++)
                    Unk70[i] = bone.Unk70[i];
            }

            /// <summary>
            /// Read a Bone from a stream.
            /// </summary>
            internal Bone(BinaryReaderEx br)
            {
                Name = br.ReadASCII(32);
                Translation = br.ReadVector3();
                Rotation = br.ReadVector3();
                Scale = br.ReadVector3();
                BoundingBoxMin = br.ReadVector3();
                BoundingBoxMax = br.ReadVector3();
                ParentIndex = br.ReadInt16();
                ChildIndex = br.ReadInt16();
                NextSiblingIndex = br.ReadInt16();
                PreviousSiblingIndex = br.ReadInt16();
                Unk64 = br.ReadInt32();
                Unk68 = br.ReadInt32();
                Unk6C = br.ReadInt32();
                Unk70 = br.ReadInt32s(8);
            }

            /// <summary>
            /// Write a Bone to a stream.
            /// </summary>
            internal void Write(BinaryWriterEx bw)
            {
                bw.WriteFixStr(Name, 32);
                bw.WriteVector3(Translation);
                bw.WriteVector3(Rotation);
                bw.WriteVector3(Scale);
                bw.WriteVector3(BoundingBoxMin);
                bw.WriteVector3(BoundingBoxMax);
                bw.WriteInt16(ParentIndex);
                bw.WriteInt16(ChildIndex);
                bw.WriteInt16(NextSiblingIndex);
                bw.WriteInt16(PreviousSiblingIndex);
                bw.WriteInt32(Unk64);
                bw.WriteInt32(Unk68);
                bw.WriteInt32(Unk6C);
                bw.WriteInt32s(Unk70);
            }

            /// <summary>
            /// Creates a transformation matrix from the scale, rotation, and translation of the bone.
            /// </summary>
            public Matrix4x4 ComputeLocalTransform()
            {
                return Matrix4x4.CreateScale(Scale)
                    * Matrix4x4.CreateRotationX(Rotation.X)
                    * Matrix4x4.CreateRotationZ(Rotation.Z)
                    * Matrix4x4.CreateRotationY(Rotation.Y)
                    * Matrix4x4.CreateTranslation(Translation);
            }

            /// <summary>
            /// Returns a string representation of the bone.
            /// </summary>
            public override string ToString()
            {
                return Name;
            }
        }
    }
}
