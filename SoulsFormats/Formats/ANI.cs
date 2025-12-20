using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace SoulsFormats
{
    /// <summary>
    /// Armored Core For Answer animations.
    /// </summary>
	/// <remarks>
	/// Extremely poor and incomplete support at this time.
	/// </remarks>
    public class ANI : SoulsFile<ANI>
    {
        /// <summary>
        /// A collection of <see cref="Node"/> objects for animation.
        /// </summary>
        public List<Node> Nodes { get; set; }

        /// <summary>
        /// The translation buffer for animation.
        /// </summary>
        public List<Vector3> Translations { get; set; }

        /// <summary>
        /// The rotation buffer for animation.
        /// </summary>
        public List<Vector3> Rotations { get; set; }

        /// <summary>
        /// Reads an <see cref="ANI"/> from a stream.
        /// </summary>
        /// <param name="br">The stream reader.</param>
        /// <exception cref="InvalidDataException">The data was not large enough.</exception>
        protected override void Read(BinaryReaderEx br)
        {
            br.BigEndian = true;
            br.AssertInt32(0x20051014);
            br.AssertInt32(0);
            br.ReadInt32(); // Frame Count
            int nodesOffset = br.ReadInt32();
            int nodeCount = br.ReadInt32();
            int translationsOffset = br.ReadInt32();
            int rotationsOffset = br.ReadInt32();
            int translationCount = br.ReadInt32();
            int rotationCount = br.ReadInt32();
            int dataSize = br.ReadInt32(); // Scales Offset?

            if (!(dataSize == br.Length || dataSize < br.Length))
                throw new InvalidDataException("Data size value was greater than stream size.");

            // Assert any extra bytes are null
            if (dataSize < br.Length)
            {
                br.StepIn(dataSize);
                br.AssertPattern((int)br.Length - dataSize, 0);
                br.StepOut();
            }

            br.AssertInt32(0);
            br.AssertInt32(1);
            br.AssertByte(1);
            br.AssertByte(1);
            br.AssertPattern(70, 0);

            Translations = new List<Vector3>(translationCount);
            Rotations = new List<Vector3>(rotationCount);
            Nodes = new List<Node>(nodeCount);

            br.StepIn(translationsOffset);
            for (int translationIndex = 0; translationIndex < translationCount; translationIndex++)
                Translations.Add(br.ReadVector3());
            br.StepOut();

            br.StepIn(rotationsOffset);
            for (int rotationIndex = 0; rotationIndex < rotationCount; rotationIndex++)
                Rotations.Add(ReadVector3Short(br));
            br.StepOut();

            br.StepIn(nodesOffset);
            for (int nodeIndex = 0; nodeIndex < nodeCount; nodeIndex++)
                Nodes.Add(new Node(br, nodeIndex));
            br.StepOut();
        }

        /// <summary>
        /// Writes this <see cref="ANI"/> to a stream.
        /// </summary>
        /// <param name="bw">The stream writer.</param>
        protected override void Write(BinaryWriterEx bw)
        {
            bw.BigEndian = true;
            bw.WriteInt32(0x20051014);
            bw.WriteInt32(0);
            bw.WriteInt32(GetKeyFrameCount());
            bw.WriteInt32(120); // NodeOffset
            bw.WriteInt32(Nodes.Count);
            bw.ReserveInt32("TranslationsOffset");
            bw.ReserveInt32("RotationsOffset");
            bw.WriteInt32(Translations.Count);
            bw.WriteInt32(Rotations.Count);
            bw.ReserveInt32("DataSize"); // Scales Offset?
            bw.WriteInt32(0);
            bw.WriteInt32(1);
            bw.WriteByte(1);
            bw.WriteByte(1);
            bw.WritePattern(70, 0);

            for (int nodeIndex = 0; nodeIndex < Nodes.Count; nodeIndex++)
                Nodes[nodeIndex].Write(bw, nodeIndex);

            for (int nodeIndex = 0; nodeIndex < Nodes.Count; nodeIndex++)
            {
                bw.FillInt32($"NodeNameOffset_{nodeIndex}", (int)bw.Position);
                bw.WriteShiftJIS(Nodes[nodeIndex].Name, true);
                if (Nodes[nodeIndex].Animation != null)
                {
                    bw.FillInt32($"AnimationOffset_{nodeIndex}", (int)bw.Position);
                    Nodes[nodeIndex].Animation.Write(bw);
                }
            }

            bw.FillInt32("TranslationsOffset", (int)bw.Position);
            foreach (var translation in Translations)
                bw.WriteVector3(translation);
            bw.FillInt32("RotationsOffset", (int)bw.Position);
            foreach (var rotation in Rotations)
                WriteVector3Short(bw, rotation);

            bw.Pad(4);
            bw.FillInt32("DataSize", (int)bw.Position); // Scales Offset?

            bw.Pad(16);
        }

        /// <summary>
        /// Checks whether the data appears to be a file of this format.
        /// </summary>
        protected override bool Is(BinaryReaderEx br)
        {
            br.BigEndian = true;
            if (br.Length < 64)
                return false;

            return br.ReadInt32() == 0x20051014;
        }

        /// <summary>
        /// Reads three shorts which are divided by 1000.0f into floats to get a <see cref="Vector3"/>.
        /// </summary>
        /// <param name="br">The stream reader.</param>
        /// <returns>A <see cref="Vector3"/>.</returns>
        private static Vector3 ReadVector3Short(BinaryReaderEx br)
        {
            return new Vector3(br.ReadInt16() / 1000.0f, br.ReadInt16() / 1000.0f, br.ReadInt16() / 1000.0f);
        }

        /// <summary>
        /// Write a <see cref="Vector3"/> into 3 shorts by multipying its coordinates by 1000.
        /// </summary>
        /// <param name="bw">The stream writer.</param>
        /// <param name="value">The value to write.</param>
        private static void WriteVector3Short(BinaryWriterEx bw, Vector3 value)
        {
            bw.WriteInt16((short)(value.X * 1000));
            bw.WriteInt16((short)(value.Y * 1000));
            bw.WriteInt16((short)(value.Z * 1000));
        }

        /// <summary>
        /// Gets the number of key frames.
        /// </summary>
        /// <returns>The number of frames.</returns>
        public int GetKeyFrameCount()
        {
            int value = 0;
            foreach (var node in Nodes)
            {
                if (node.Animation != null)
                {
                    foreach (var frame in node.Animation.Frames)
                    {
                        if (frame.KeyFrame > value)
                        {
                            value = frame.KeyFrame;
                        }
                    }
                }
            }
            return value;
        }

        /// <summary>
        /// A bone and information regarding where it is each frame.
        /// </summary>
        public class Node
        {
            /// <summary>
            /// The different <see cref="Node"/> types.
            /// </summary>
            public enum NodeType : int
            {
                /// <summary>
                /// The <see cref="Node"/> is intended as geometry.
                /// </summary>
                Geom = 1,

                /// <summary>
                /// The <see cref="Node"/> is intended to connect geometry.
                /// </summary>
                Dummy = 2
            }

            /// <summary>
            /// The <see cref="Node"/> type.
            /// </summary>
            public NodeType Type { get; set; }

            /// <summary>
            /// The geometry index of this entry.
            /// </summary>
            public short GeomIndex { get; set; }

            /// <summary>
            /// The index of the parent.
            /// </summary>
            public short ParentIndex { get; set; }

            /// <summary>
            /// The index of the first child.
            /// </summary>
            public short FirstChildIndex { get; set; }

            /// <summary>
            /// The index of the next sibling.
            /// </summary>
            public short NextSiblingIndex { get; set; }

            /// <summary>
            /// Unknown, always seems to be -1, but not all files have been tested yet.
            /// </summary>
            public short UnkIndex12 { get; set; }

            /// <summary>
            /// The <see cref="Node"/> translation.
            /// </summary>
            public Vector3 Translation { get; set; }

            /// <summary>
            /// The <see cref="Node"/> rotation.
            /// </summary>
            public Vector3 Rotation { get; set; }

            /// <summary>
            /// The <see cref="Node"/> scale.
            /// </summary>
            public Vector3 Scale { get; set; }

            /// <summary>
            /// The name of the <see cref="Node"/>.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Information about frames for this <see cref="Node"/>.<br/>
            /// Null when there is none.
            /// </summary>
            public NodeAnimation Animation { get; set; }

            /// <summary>
            /// Create a new <see cref="Node"/>.
            /// </summary>
            public Node()
            {
                Type = NodeType.Geom;
                GeomIndex = -1;
                ParentIndex = -1;
                FirstChildIndex = -1;
                NextSiblingIndex = -1;
                UnkIndex12 = -1;
                Translation = Vector3.Zero;
                Rotation = Vector3.Zero;
                Scale = Vector3.One;
                Name = string.Empty;
            }

            /// <summary>
            /// Read this <see cref="Node"/> from a stream.
            /// </summary>
            /// <param name="br">The stream reader,</param>
            /// <param name="nodeIndex">The index of the <see cref="Node"/>.</param>
            /// <exception cref="InvalidDataException">The <see cref="Node"/> had no name.</exception>
            internal Node(BinaryReaderEx br, int nodeIndex)
            {
                int nodeNameOffset = br.ReadInt32();
                if (nodeNameOffset < 1)
                    throw new InvalidDataException($"{nameof(Node)} must have a name.");

                Name = br.GetShiftJIS(nodeNameOffset);
                Type = br.ReadEnum32<NodeType>();
                br.AssertInt16((short)nodeIndex);
                GeomIndex = br.ReadInt16();
                ParentIndex = br.ReadInt16();
                FirstChildIndex = br.ReadInt16();
                NextSiblingIndex = br.ReadInt16();
                UnkIndex12 = br.ReadInt16();
                Translation = br.ReadVector3();
                Rotation = br.ReadVector3();
                Scale = br.ReadVector3();
                int animationOffset = br.ReadInt32();
                br.AssertPattern(4, 0);
                br.ReadInt32(); // Unknown data offset
                br.AssertPattern(176, 0);

                if (animationOffset > 0)
                {
                    long pos = br.Position;
                    br.Position = animationOffset;
                    Animation = new NodeAnimation(br);
                    br.Position = pos;
                }
            }

            /// <summary>
            /// Write this <see cref="Node"/> to a stream.
            /// </summary>
            /// <param name="bw">The stream writer.</param>
            /// <param name="nodeIndex">The index of the <see cref="Node"/>.</param>
            internal void Write(BinaryWriterEx bw, int nodeIndex)
            {
                bw.ReserveInt32($"NodeNameOffset_{nodeIndex}");
                bw.WriteInt32((int)Type);
                bw.WriteInt16((short)nodeIndex);
                bw.WriteInt16(GeomIndex);
                bw.WriteInt16(ParentIndex);
                bw.WriteInt16(FirstChildIndex);
                bw.WriteInt16(NextSiblingIndex);
                bw.WriteInt16(UnkIndex12);
                bw.WriteVector3(Translation);
                bw.WriteVector3(Rotation);
                bw.WriteVector3(Scale);
                if (Animation != null)
                    bw.ReserveInt32($"AnimationOffset_{nodeIndex}");
                else
                    bw.WriteInt32(0);

                bw.WritePattern(184, 0); // TODO: Unknown data offset 4 bytes into  this
            }

            /// <summary>
            /// A collection of <see cref="Frame"/> objects to represent <see cref="Node"/> transformation over time.
            /// </summary>
            public class NodeAnimation
            {
                /// <summary>
                /// The format <see cref="Frame"/> data is stored in.
                /// </summary>
                public enum FrameFormat : int
                {
                    /// <summary>
                    /// Translation and rotation indices stored as bytes.
                    /// </summary>
                    PosRotBytes = 1,

                    /// <summary>
                    /// Translation and rotation indices stored as shorts.
                    /// </summary>
                    PosRotShorts = 2,

                    /// <summary>
                    /// Rotation indices stored as shorts.
                    /// </summary>
                    RotShorts = 4
                }

                /// <summary>
                /// The format of the stored <see cref="Frame"/> data.
                /// </summary>
                public FrameFormat Format { get; set; }

                /// <summary>
                /// Unknown; A rotation of some kind, usually the same as the rotation of the <see cref="Node"/> that owns it.
                /// </summary>
                public Vector3 Unk10 { get; set; }

                /// <summary>
                /// Unknown; A rotation of some kind, usually the same as the rotation of the <see cref="Node"/> that owns it.
                /// </summary>
                public Vector3 Unk20 { get; set; }

                /// <summary>
                /// The key frames in this <see cref="NodeAnimation"/>.
                /// </summary>
                public List<Frame> Frames { get; set; }

                /// <summary>
                /// Creates a new <see cref="NodeAnimation"/>.
                /// </summary>
                public NodeAnimation()
                {
                    Format = FrameFormat.PosRotShorts;
                    Frames = new List<Frame>();
                }

                /// <summary>
                /// Creates a new <see cref="NodeAnimation"/>.
                /// </summary>
                public NodeAnimation(int frameCount)
                {
                    Format = FrameFormat.PosRotShorts;
                    Frames = new List<Frame>(frameCount);
                }

                /// <summary>
                /// Creates a new <see cref="NodeAnimation"/>.
                /// </summary>
                public NodeAnimation(FrameFormat animationType, int frameCount)
                {
                    Format = animationType;
                    Frames = new List<Frame>(frameCount);
                }

                /// <summary>
                /// Reads a <see cref="NodeAnimation"/> from a stream.
                /// </summary>
                /// <param name="br">The stream reader.</param>
                internal NodeAnimation(BinaryReaderEx br)
                {
                    int framesOffset = br.ReadInt32();
                    int frameCount = br.ReadInt32();
                    Format = br.ReadEnum32<FrameFormat>();
                    Unk10 = br.ReadVector3();
                    Unk20 = br.ReadVector3();
                    br.AssertInt32(0);

                    br.Position = framesOffset;
                    Frames = new List<Frame>(frameCount);
                    for (int i = 0; i < frameCount; i++)
                        Frames.Add(new Frame(br, Format));
                }

                /// <summary>
                /// Writes this <see cref="NodeAnimation"/> to a stream.
                /// </summary>
                /// <param name="bw">The stream writer.</param>
                internal void Write(BinaryWriterEx bw)
                {
                    bw.WriteInt32((int)bw.Position + 36); // framesOffset
                    bw.WriteInt32(Frames.Count);
                    bw.WriteInt32((int)Format);
                    bw.WriteVector3(Unk10);
                    bw.WriteVector3(Unk20);
                    bw.WriteInt32(0);

                    foreach (var frame in Frames)
                        frame.Write(bw, Format);
                }

                /// <summary>
                /// A transformation in a point of time.
                /// </summary>
                public class Frame
                {
                    /// <summary>
                    /// The point in time this <see cref="Frame"/> represents.
                    /// </summary>
                    public short KeyFrame { get; set; }

                    /// <summary>
                    /// The translation index.
                    /// </summary>
                    public short TranslationIndex { get; set; }

                    /// <summary>
                    /// The translation in-tangent index for cublic-spline animation sampling.
                    /// </summary>
                    public short TranslationInTangentIndex { get; set; }

                    /// <summary>
                    /// The translation out-tangent index for cublic-spline animation sampling.
                    /// </summary>
                    public short TranslationOutTangentIndex { get; set; }

                    /// <summary>
                    /// The rotation index.
                    /// </summary>
                    public short RotationIndex { get; set; }

                    /// <summary>
                    /// The rotation in-tangent index for cublic-spline animation sampling.
                    /// </summary>
                    public short RotationInTangentIndex { get; set; }

                    /// <summary>
                    /// The rotation out-tangent index for cublic-spline animation sampling.
                    /// </summary>
                    public short RotationOutTangentIndex { get; set; }

                    /// <summary>
                    /// Unknown; Scale index?
                    /// </summary>
                    public short UnkIndex { get; set; }

                    /// <summary>
                    /// Create a new <see cref="Frame"/>.
                    /// </summary>
                    public Frame()
                    {
                        KeyFrame = 0;
                    }

                    /// <summary>
                    /// Create a new <see cref="Frame"/>.
                    /// </summary>
                    public Frame(short keyFrame)
                    {
                        KeyFrame = keyFrame;
                    }

                    /// <summary>
                    /// Reads a <see cref="Frame"/> from a stream.
                    /// </summary>
                    /// <param name="br">The stream reader.</param>
                    /// <param name="format">The <see cref="FrameFormat"/>.</param>
                    /// <exception cref="NotImplementedException">The <see cref="FrameFormat"/> was not implemented.</exception>
                    internal Frame(BinaryReaderEx br, FrameFormat format)
                    {
                        KeyFrame = br.ReadInt16();
                        switch (format)
                        {
                            case FrameFormat.PosRotBytes:
                                TranslationIndex = br.ReadByte();
                                TranslationInTangentIndex = br.ReadByte();
                                TranslationOutTangentIndex = br.ReadByte();
                                RotationIndex = br.ReadByte();
                                RotationInTangentIndex = br.ReadByte();
                                RotationOutTangentIndex = br.ReadByte();
                                UnkIndex = 1;
                                break;
                            case FrameFormat.PosRotShorts:
                                TranslationIndex = br.ReadInt16();
                                TranslationInTangentIndex = br.ReadInt16();
                                TranslationOutTangentIndex = br.ReadInt16();
                                RotationIndex = br.ReadInt16();
                                RotationInTangentIndex = br.ReadInt16();
                                RotationOutTangentIndex = br.ReadInt16();
                                UnkIndex = br.ReadInt16();
                                break;
                            case FrameFormat.RotShorts:
                                TranslationIndex = -1;
                                TranslationInTangentIndex = -1;
                                TranslationOutTangentIndex = -1;
                                RotationIndex = br.ReadInt16();
                                RotationInTangentIndex = br.ReadInt16();
                                RotationOutTangentIndex = br.ReadInt16();
                                UnkIndex = 1;
                                break;
                            default:
                                throw new NotImplementedException($"{nameof(FrameFormat)} \"{format}\" has not been implemented.");
                        }
                    }

                    /// <summary>
                    /// Write this <see cref="Frame"/> to a stream.
                    /// </summary>
                    /// <param name="bw">The stream writer.</param>
                    /// <param name="format">The <see cref="FrameFormat"/>.</param>
                    /// <exception cref="NotImplementedException">The <see cref="FrameFormat"/> was not implemented.</exception>
                    internal void Write(BinaryWriterEx bw, FrameFormat format)
                    {
                        bw.WriteInt32(KeyFrame);
                        switch (format)
                        {
                            case FrameFormat.PosRotBytes:
                                bw.WriteByte((byte)TranslationIndex);
                                bw.WriteByte((byte)TranslationInTangentIndex);
                                bw.WriteByte((byte)TranslationOutTangentIndex);
                                bw.WriteByte((byte)RotationIndex);
                                bw.WriteByte((byte)RotationInTangentIndex);
                                bw.WriteByte((byte)RotationOutTangentIndex);
                                break;
                            case FrameFormat.PosRotShorts:
                                bw.WriteInt16(TranslationIndex);
                                bw.WriteInt16(TranslationInTangentIndex);
                                bw.WriteInt16(TranslationOutTangentIndex);
                                bw.WriteInt16(RotationIndex);
                                bw.WriteInt16(RotationInTangentIndex);
                                bw.WriteInt16(RotationOutTangentIndex);
                                bw.WriteInt16(UnkIndex);
                                break;
                            case FrameFormat.RotShorts:
                                bw.WriteInt16(RotationIndex);
                                bw.WriteInt16(RotationInTangentIndex);
                                bw.WriteInt16(RotationOutTangentIndex);
                                break;
                            default:
                                throw new NotImplementedException($"{nameof(FrameFormat)} \"{format}\" has not been implemented.");
                        }
                    }

                    /// <summary>
                    /// Get the in-tangent translation of this <see cref="Frame"/>.
                    /// </summary>
                    /// <param name="translations">The positions array from the <see cref="ANI"/> itself.</param>
                    /// <returns>The in-tangent of the translation of this <see cref="Frame"/>.</returns>
                    public Vector3 GetTranslationInTangent(List<Vector3> translations)
                    {
                        return translations[TranslationInTangentIndex];
                    }

                    /// <summary>
                    /// Get the translation of this <see cref="Frame"/>.
                    /// </summary>
                    /// <param name="translations">The positions array from the <see cref="ANI"/> itself.</param>
                    /// <returns>The translation of this <see cref="Frame"/>.</returns>
                    public Vector3 GetTranslation(List<Vector3> translations)
                    {
                        return translations[TranslationIndex];
                    }

                    /// <summary>
                    /// Get the out-tangent of the translation of this <see cref="Frame"/>.
                    /// </summary>
                    /// <param name="translations">The positions array from the <see cref="ANI"/> itself.</param>
                    /// <returns>The out-tangent of the translation of this <see cref="Frame"/>.</returns>
                    public Vector3 GetTranslationOutTangent(List<Vector3> translations)
                    {
                        return translations[TranslationOutTangentIndex];
                    }

                    /// <summary>
                    /// Get the in-tangent of the rotation of this <see cref="Frame"/>.
                    /// </summary>
                    /// <param name="rotations">The rotations array from the <see cref="ANI"/> itself.</param>
                    /// <returns>The in-tangent rotation of this <see cref="Frame"/>.</returns>
                    public Vector3 GetRotationInTangent(List<Vector3> rotations)
                    {
                        return rotations[RotationInTangentIndex];
                    }

                    /// <summary>
                    /// Get the rotation of this <see cref="Frame"/>.
                    /// </summary>
                    /// <param name="rotations">The rotations array from the <see cref="ANI"/> itself.</param>
                    /// <returns>The rotation of this <see cref="Frame"/>.</returns>
                    public Vector3 GetRotation(List<Vector3> rotations)
                    {
                        return rotations[RotationIndex];
                    }

                    /// <summary>
                    /// Get the out-tangent of the rotation of this <see cref="Frame"/>.
                    /// </summary>
                    /// <param name="rotations">The rotations array from the <see cref="ANI"/> itself.</param>
                    /// <returns>The out-tangent of the rotation of this <see cref="Frame"/>.</returns>
                    public Vector3 GetRotationOutTangent(List<Vector3> rotations)
                    {
                        return rotations[RotationOutTangentIndex];
                    }
                }
            }
        }
    }
}
