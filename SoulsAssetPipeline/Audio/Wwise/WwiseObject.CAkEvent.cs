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
        public class CAkEvent : WwiseObjectBase<CAkEvent>
        {
            public WwiseVarint ActionListSize = new WwiseVarint();
            public WwiseIDList Actions = new WwiseIDList(nameof(ActionListSize));
        }
    }
}
