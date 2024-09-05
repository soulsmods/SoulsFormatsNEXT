using System;
using System.Collections.Generic;

namespace SoulsFormats.Util.Triangles
{
    /// <summary>
    /// A strip of Triangles.
    /// </summary>
    internal class TriangleStrip
    {
        /// <summary>
        /// The Primitive Restart Constant.
        /// </summary>
        internal int PrimitiveRestartConstant;

        /// <summary>
        /// The Triangles in this TriangleStrip.
        /// </summary>
        internal List<Triangle> Triangles;

        /// <summary>
        /// The face or triangle count of this TriangleStrip.
        /// </summary>
        internal int FaceCount => Triangles.Count;

        /// <summary>
        /// Create a new TriangleStrip with a buffer of indices and the Primitive Restart Constant used in it.
        /// </summary>
        internal TriangleStrip(IList<int> indices, int primitiveRestartConstant)
        {
            Triangles = new List<Triangle>();

            PrimitiveRestartConstant = primitiveRestartConstant;

            for (int i = 0; i < indices.Count - 2; i++)
            {

                int indexA;
                int indexB;
                int indexC;

                if (i % 2 == 1)
                {
                    indexA = indices[i + 1];
                    indexB = indices[i];
                    indexC = indices[i + 2];
                }
                else
                {
                    indexA = indices[i];
                    indexB = indices[i + 1];
                    indexC = indices[i + 2];
                }

                if (indexA == PrimitiveRestartConstant || indexB == PrimitiveRestartConstant || indexC == PrimitiveRestartConstant)
                    throw new Exception("The Primitive Restart Constant was passed as an index.");

                if (indexA != indexB && indexA != indexC && indexB != indexC)
                {
                    Triangles.Add(new Triangle(indexA, indexB, indexC));
                }
            }
        }

        /// <summary>
        /// Get a buffer of this TriangleStrip that contains the Primitive Restart Constant if needed.
        /// </summary>
        /// <param name="lastStrip">Whether or not this is the last strip.</param>
        internal int[] GetBuffer(bool lastStrip = false)
        {
            List<int> buffer = new List<int>((Triangles.Count * 3) + 1);
            foreach (Triangle triangle in Triangles)
            {
                buffer.AddRange(triangle.GetIndices());
            }

            if (!lastStrip)
            {
                buffer.Add(PrimitiveRestartConstant);
            }

            return buffer.ToArray();
        }

        /// <summary>
        /// Get a list of faces as index arrays.
        /// </summary>
        internal List<int[]> GetFaceIndices()
        {
            List<int[]> faces = new List<int[]>();
            foreach (Triangle triangle in Triangles)
            {
                faces.Add(triangle.GetIndices());
            }

            return faces;
        }

        /// <summary>
        /// Get a list of faces as index arrays.
        /// </summary>
        internal List<ushort[]> GetFaceIndicesUShort()
        {
            List<ushort[]> faces = new List<ushort[]>();
            foreach (Triangle triangle in Triangles)
            {
                faces.Add(triangle.GetIndicesUShort());
            }

            return faces;
        }

        /// <summary>
        /// Get a list of all indices.
        /// </summary>
        internal List<int> GetIndices()
        {
            List<int> indices = new List<int>();
            foreach (Triangle triangle in Triangles)
            {
                indices.AddRange(triangle.GetIndices());
            }

            return indices;
        }

        /// <summary>
        /// Get a list of all indices.
        /// </summary>
        internal List<ushort> GetIndicesUShort()
        {
            List<ushort> indices = new List<ushort>();
            foreach (Triangle triangle in Triangles)
            {
                indices.AddRange(triangle.GetIndicesUShort());
            }

            return indices;
        }
    }
}
