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
        public class StateGroup : WwiseObjectBase<StateGroup>
        {
            public uint StateGroupID;
            public byte StateSyncType;
            public WwiseVarint NumStateRefs = new WwiseVarint();
            public WwiseObjectList<StateRef> StateRefs = 
                new WwiseObjectList<StateRef>(nameof(NumStateRefs));
        }
    }
}
