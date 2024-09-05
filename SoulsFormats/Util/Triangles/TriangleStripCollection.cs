using System.Collections.Generic;

namespace SoulsFormats.Util.Triangles
{
    /// <summary>
    /// A collection of TriangleStrips.
    /// </summary>
    internal class TriangleStripCollection
    {
        /// <summary>
        /// The Primitive Restart Constant.
        /// </summary>
        internal int PrimitiveRestartConstant;

        /// <summary>
        /// The TriangleStrips in this TriangleStripCollection.
        /// </summary>
        internal List<TriangleStrip> TriangleStrips;

        /// <summary>
        /// The total face count of all TriangleStrips in this TriangleStripCollection.
        /// </summary>
        internal int FaceCount
        {
            get
            {
                int count = 0;
                foreach (TriangleStrip strip in TriangleStrips)
                {
                    count += strip.FaceCount;
                }
                return count;
            }
        }

        /// <summary>
        /// The number of TriangleStrips in this TriangleStripCollection.
        /// </summary>
        internal int StripCount => TriangleStrips.Count;

        /// <summary>
        /// Create a new TriangleStripCollection with the provided buffer and Primitive Restart Constant.
        /// </summary>
        internal TriangleStripCollection(IList<int> indexBuffer, int primitiveRestartConstant)
        {
            TriangleStrips = new List<TriangleStrip>();
            PrimitiveRestartConstant = primitiveRestartConstant;

            List<int> stripBuffer = new List<int>();
            for (int i = 0; i < indexBuffer.Count; i++)
            {
                if (indexBuffer[i] != PrimitiveRestartConstant)
                {
                    stripBuffer.Add(indexBuffer[i]);
                }
                else
                {
                    TriangleStrips.Add(new TriangleStrip(stripBuffer, PrimitiveRestartConstant));
                    stripBuffer = new List<int>();
                }
            }
        }

        /// <summary>
        /// Create a new TriangleStripCollection with the provided buffer and Primitive Restart Constant.
        /// </summary>
        internal TriangleStripCollection(IList<ushort> indexBuffer, int primitiveRestartConstant)
        {
            TriangleStrips = new List<TriangleStrip>();
            PrimitiveRestartConstant = primitiveRestartConstant;

            List<int> stripBuffer = new List<int>();
            for (int i = 0; i < indexBuffer.Count; i++)
            {
                if (indexBuffer[i] != PrimitiveRestartConstant)
                {
                    stripBuffer.Add(indexBuffer[i]);
                }
                else
                {
                    TriangleStrips.Add(new TriangleStrip(stripBuffer, PrimitiveRestartConstant));
                    stripBuffer = new List<int>();
                }
            }
        }

        /// <summary>
        /// Get a buffer of this TriangleStripCollection.
        /// </summary>
        internal int[] GetBuffer()
        {
            List<int> buffer = new List<int>();
            for (int i = 0; i < TriangleStrips.Count; i++)
            {
                if (i == TriangleStrips.Count - 1)
                {
                    buffer.AddRange(TriangleStrips[i].GetBuffer(true));
                }
                else
                {
                    buffer.AddRange(TriangleStrips[i].GetBuffer(false));
                }
            }
            return buffer.ToArray();
        }

        /// <summary>
        /// Get a buffer of this TriangleStripCollection.
        /// </summary>
        internal ushort[] GetBufferUShort()
        {
            var buffer = GetBuffer();
            var newBuffer = new ushort[buffer.Length];
            for (int i = 0; i < buffer.Length; i++)
            {
                newBuffer[i] = (ushort)buffer[i];
            }
            return newBuffer;
        }

        /// <summary>
        /// Get a list of faces as index arrays.
        /// </summary>
        internal List<int[]> GetFaceIndices()
        {
            List<int[]> faces = new List<int[]>();
            foreach (var strip in TriangleStrips)
            {
                faces.AddRange(strip.GetFaceIndices());
            }
            return faces;
        }

        /// <summary>
        /// Get a list of faces as index arrays.
        /// </summary>
        internal List<ushort[]> GetFaceIndicesUShort()
        {
            List<ushort[]> faces = new List<ushort[]>();
            foreach (var strip in TriangleStrips)
            {
                faces.AddRange(strip.GetFaceIndicesUShort());
            }
            return faces;
        }

        /// <summary>
        /// Get a list of all indices.
        /// </summary>
        internal List<int> GetIndices()
        {
            List<int> indices = new List<int>();
            foreach (var strip in TriangleStrips)
            {
                indices.AddRange(strip.GetIndices());
            }
            return indices;
        }

        /// <summary>
        /// Get a list of all indices.
        /// </summary>
        internal List<ushort> GetIndicesUShort()
        {
            List<ushort> indices = new List<ushort>();
            foreach (var strip in TriangleStrips)
            {
                indices.AddRange(strip.GetIndicesUShort());
            }
            return indices;
        }
    }
}
