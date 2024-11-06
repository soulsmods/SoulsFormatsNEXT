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
        public class RangedPropertyBundle : WwiseObjectBase<RangedPropertyBundle>
        {
            public byte PropCount;
            public WwiseByteList PropTypes = new WwiseByteList(nameof(PropCount));
            public WwiseObjectList<RangedUni> PropValues = new WwiseObjectList<RangedUni>(nameof(PropCount));
        }
    }
}
