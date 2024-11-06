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
        public class StateChunk : WwiseObjectBase<StateChunk>
        {
            public WwiseVarint NumStateProps = new WwiseVarint();
            public WwiseObjectList<StateProp> StateProps = 
                new WwiseObjectList<StateProp>(nameof(NumStateProps));
            public WwiseVarint NumStateGroups = new WwiseVarint();
            public WwiseObjectList<StateGroup> StateGroups =
                new WwiseObjectList<StateGroup>(nameof(NumStateGroups));
        }
    }
}
