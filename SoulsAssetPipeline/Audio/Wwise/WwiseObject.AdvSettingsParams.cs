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
        public class AdvSettingsParams : WwiseObjectBase<AdvSettingsParams>
        {
            public byte BitField1;
            public byte VirtualQueueBehavior;
            public ushort MaxNumInstance;
            public byte BelowThresholdBehavior;
            public byte BitField2;
        }
    }
}
