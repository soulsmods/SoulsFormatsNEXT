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
        public class StateRef : WwiseObjectBase<StateRef>
        {
            public uint StateID;
            public uint StateInstanceID;
        }
    }
}
