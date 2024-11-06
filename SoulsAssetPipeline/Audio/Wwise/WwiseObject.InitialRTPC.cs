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
        public class InitialRTPC : WwiseObjectBase<InitialRTPC>
        {
            public ushort NumRTPC;
            public WwiseObjectList<RTPC> RTPCs = 
                new WwiseObjectList<RTPC>(nameof(NumRTPC));
        }
    }
}
