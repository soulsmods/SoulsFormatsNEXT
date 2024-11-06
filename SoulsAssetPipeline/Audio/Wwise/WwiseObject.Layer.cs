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
        public class Layer : WwiseObjectBase<Layer>
        {
            public uint LayerID;
            public InitialRTPC InitialRTPC = new InitialRTPC();
            public uint RTPCID;
            public byte RTPCType;
            public int AssocCount;
            public WwiseObjectList<AssociatedChildData> Assocs = new WwiseObjectList<AssociatedChildData>(nameof(AssocCount));
        }
    }
}
