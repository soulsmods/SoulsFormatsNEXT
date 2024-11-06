using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Audio.Wwise
{
    public static partial class WwiseObject
    {
        public class PathListItem : WwiseObjectBase<PathListItem>
        {
            public uint VerticesOffset;
            public uint NumVertices;
        }
    }
}
