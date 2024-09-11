using System;
using System.Collections.Generic;
using SoulsFormats.Util.Triangles;

namespace SoulsFormats
{
    public partial class SMD4
    {
        /// <summary>
        /// An individual chunk of a model.
        /// </summary>
        public class Mesh
        {
            /// <summary>
            /// The format of vertices in the vertex buffer.
            /// </summary>
            public byte VertexFormat { get; set; }

            /// <summary>
            /// Unknown.
            /// </summary>
            public byte Unk01 { get; set; }

            /// <summary>
            /// Unknown; Cull backfaces?
            /// </summary>
            public bool Unk02 { get; set; }

            /// <summary>
            /// Unknown.
            /// </summary>
            public bool Unk03 { get; set; }

            /// <summary>
            /// Unknown; Default Bone Index?
            /// </summary>
            public short Unk06 { get; set; }

            /// <summary>
            /// Indexes of bones in the bone collection which may be used by vertices in this mesh.
            /// </summary>
            /// <remarks>
            /// Always has 28 indices; Unused indices are set to -1.
            /// </remarks>
            public short[] BoneIndices { get; set; }

            /// <summary>
            /// Get the number of used bone indices.
            /// </summary>
            public int BoneCount
            {
                get
                {
                    int count = 0;
                    for (int i = 0; i < 28; i++)
                    {
                        if (BoneIndices[i] == -1)
                        {
                            break;
                        }

                        count++;
                    }
                    return count;
                }
            }

            /// <summary>
            /// The vertex indices in this mesh.
            /// </summary>
            public List<ushort> VertexIndices { get; set; }

            /// <summary>
            /// The vertices in this mesh.
            /// </summary>
            public List<Vertex> Vertices { get; set; }

            /// <summary>
            /// Create a new and empty Mesh with default values.
            /// </summary>
            public Mesh()
            {
                VertexFormat = 0;
                Unk01 = 0;
                Unk02 = true;
                Unk03 = false;
                Unk06 = 0;
                BoneIndices = new short[28];
                VertexIndices = new List<ushort>();
                Vertices = new List<Vertex>();
                for (int i = 0; i < 28; i++)
                    BoneIndices[i] = -1;
            }

            /// <summary>
            /// Clone an existing Mesh.
            /// </summary>
            public Mesh(Mesh mesh)
            {
                VertexFormat = mesh.VertexFormat;
                Unk01 = mesh.Unk01;
                Unk02 = mesh.Unk02;
                Unk03 = mesh.Unk03;
                Unk06 = mesh.Unk06;
                BoneIndices = new short[28];
                VertexIndices = new List<ushort>();
                Vertices = new List<Vertex>();
                for (int i = 0; i < 28; i++)
                    BoneIndices[i] = mesh.BoneIndices[i];
                for (int i = 0; i < mesh.VertexIndices.Count; i++)
                    VertexIndices[i] = mesh.VertexIndices[i];
                for (int i = 0; i < mesh.Vertices.Count; i++)
                    Vertices[i] = new Vertex(mesh.Vertices[i]);
            }

            /// <summary>
            /// Read a new Mesh from a stream.
            /// </summary>
            internal Mesh(BinaryReaderEx br, int dataOffset, int version)
            {
                VertexFormat = br.ReadByte();
                Unk01 = br.ReadByte();
                Unk02 = br.ReadBoolean();
                Unk03 = br.ReadBoolean();
                ushort vertexIndexCount = br.ReadUInt16();
                Unk06 = br.ReadInt16();
                BoneIndices = br.ReadInt16s(28);
                br.AssertInt32(vertexIndexCount * 2); // Vertex Indices Length
                int vertexIndicesOffset = br.ReadInt32();
                int vertexBufferLength = br.ReadInt32();
                int vertexBufferOffset = br.ReadInt32();

                VertexIndices = new List<ushort>();
                Vertices = new List<Vertex>();

                VertexIndices.AddRange(br.GetUInt16s(dataOffset + vertexIndicesOffset, vertexIndexCount));

                br.StepIn(dataOffset + vertexBufferOffset);
                int vertexCount = vertexBufferLength / GetVertexSize(version);
                for (int i = 0; i < vertexCount; i++)
                {
                    Vertices.Add(new Vertex(br, version, VertexFormat));
                }
                br.StepOut();
            }

            /// <summary>
            /// Write this Mesh to a stream.
            /// </summary>
            internal void Write(BinaryWriterEx bw, int index, int version)
            {
                bw.WriteByte(VertexFormat);
                bw.WriteByte(Unk01);
                bw.WriteBoolean(Unk02);
                bw.WriteBoolean(Unk03);
                bw.WriteInt16((short)VertexIndices.Count);
                bw.WriteInt16(Unk06);
                bw.WriteInt16s(BoneIndices);
                bw.WriteInt32(VertexIndices.Count * 2); // Vertex Indices Length
                bw.ReserveInt32($"vertexIndicesOffset_{index}");
                bw.WriteInt32(Vertices.Count * GetVertexSize(version)); // Vertex Buffer Length
                bw.ReserveInt32($"vertexBufferOffset_{index}");
            }

            /// <summary>
            /// Get the size of each Vertex.
            /// </summary>
            internal int GetVertexSize(int version)
            {
                if (version == 0x40001)
                {
                    if (VertexFormat == 0)
                    {
                        return 16;
                    }
                    else if (VertexFormat == 2)
                    {
                        return 36;
                    }
                    else
                    {
                        throw new NotSupportedException($"VertexFormat {VertexFormat} is not currently supported for Version {version}.");
                    }
                }
                else
                {
                    throw new NotSupportedException($"Version {version} is not currently supported.");
                }
            }

            /// <summary>
            /// Get the calculated face count from the VertexIndices of this Mesh.
            /// </summary>
            public int GetFaceCount()
            {
                return new TriangleStripCollection(VertexIndices, 65535).FaceCount;
            }

            /// <summary>
            /// Get the calculated strip count from the VertexIndices of this Mesh.
            /// </summary>
            public int GetStripCount()
            {
                return new TriangleStripCollection(VertexIndices, 65535).StripCount;
            }

            /// <summary>
            /// Get a list of faces as index arrays.
            /// </summary>
            public List<int[]> GetFaceIndices()
            {
                return new TriangleStripCollection(VertexIndices, 65535).GetFaceIndices();
            }

            /// <summary>
            /// Get a list of faces as index arrays.
            /// </summary>
            public List<ushort[]> GetFaceIndicesUShort()
            {
                return new TriangleStripCollection(VertexIndices, 65535).GetFaceIndicesUShort();
            }

            /// <summary>
            /// Get a list of all indices.
            /// </summary>
            public List<int> GetIndices()
            {
                return new TriangleStripCollection(VertexIndices, 65535).GetIndices();
            }

            /// <summary>
            /// Get a list of indices.
            /// </summary>
            public List<ushort> GetIndicesUShort()
            {
                return new TriangleStripCollection(VertexIndices, 65535).GetIndicesUShort();
            }

            /// <summary>
            /// Gets a list of the faces used by this mesh as a list of vertex arrays.
            /// </summary>
            public List<Vertex[]> GetFaces()
            {
                List<ushort> indices = GetIndicesUShort();
                var faces = new List<Vertex[]>();
                for (int i = 0; i < indices.Count; i += 3)
                {
                    faces.Add(new Vertex[]
                    {
                        Vertices[indices[i + 0]],
                        Vertices[indices[i + 1]],
                        Vertices[indices[i + 2]],
                    });
                }
                return faces;
            }
        }
    }
}
