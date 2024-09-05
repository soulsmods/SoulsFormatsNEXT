namespace SoulsFormats.Util.Triangles
{
    /// <summary>
    /// A triangle with three indices.
    /// </summary>
    internal struct Triangle
    {
        /// <summary>
        /// The first index.
        /// </summary>
        internal int IndexA;

        /// <summary>
        /// The second index.
        /// </summary>
        internal int IndexB;

        /// <summary>
        /// The third index.
        /// </summary>
        internal int IndexC;

        /// <summary>
        /// Create a new triangle with three indices.
        /// </summary>
        internal Triangle(int indexA, int indexB, int indexC)
        {
            IndexA = indexA;
            IndexB = indexB;
            IndexC = indexC;
        }

        /// <summary>
        /// Get an array of the three indices.
        /// </summary>
        internal int[] GetIndices()
        {
            return new int[3] { IndexA, IndexB, IndexC };
        }

        /// <summary>
        /// Get an array of the three indices.
        /// </summary>
        internal ushort[] GetIndicesUShort()
        {
            return new ushort[3] { (ushort)IndexA, (ushort)IndexB, (ushort)IndexC };
        }
    }
}
