using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Audio.Wwise
{
    public static partial class WwiseObject
    {
        public class RTPC : WwiseObjectBase<RTPC>
        {
            public uint RTPCID;
            public byte RTPCType;
            public byte RTPCAccumType;
            public WwiseVarint ParamID = new WwiseVarint();
            public uint RTPCCurveID;
            public byte Scaling;
            public ushort PointCount;
            public WwiseObjectList<RTPCGraphPoint> GraphPoints = 
                new WwiseObjectList<RTPCGraphPoint>(nameof(PointCount));
        }
    }
}
