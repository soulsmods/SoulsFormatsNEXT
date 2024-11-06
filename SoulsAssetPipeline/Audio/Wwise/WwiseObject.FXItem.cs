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
        public class FXItem : WwiseObjectBase<FXItem>
        {
            public byte FXIndex;
            public uint FXID;
            public bool IsShareSet;
            public bool IsRendered;
        }
    }
}
