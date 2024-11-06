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
        public class SwitchGroup : WwiseObjectBase<SwitchGroup>
        {
            public uint SwitchID;
            public uint NumItems;
            public WwiseIDList NodeObjIDs = new WwiseIDList(nameof(NumItems));
        }
    }
}
