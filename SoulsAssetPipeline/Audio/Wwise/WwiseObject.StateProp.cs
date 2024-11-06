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
        public class StateProp : WwiseObjectBase<StateProp>
        {
            public WwiseVarint PropertyID = new WwiseVarint();
            public byte AccumType;
            public byte InDb;
        }
    }
}
