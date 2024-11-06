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
        public class AssociatedChildData : WwiseObjectBase<AssociatedChildData>
        {
            public uint AssociatedChildID;
            public int PointCount;
            public WwiseObjectList<RTPCGraphPoint> Points = new WwiseObjectList<RTPCGraphPoint>(nameof(PointCount));
        }
    }
}
