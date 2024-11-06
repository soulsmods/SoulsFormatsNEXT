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
        public class FXParams : WwiseObjectBase<FXParams>
        {
            public bool IsOverrideParentFx;
            public byte NumFx;
            public WwiseConditionalByte BitsFXBypass = new WwiseConditionalByte(nameof(NumFx));
            public WwiseObjectList<FXItem> FXItems = new WwiseObjectList<FXItem>(nameof(NumFx));
        }
    }
}
