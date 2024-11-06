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
        public class PropertyBundle : WwiseObjectBase<PropertyBundle>
        {
            public byte PropCount;
            public WwiseByteList PropTypes = new WwiseByteList(nameof(PropCount));
            public WwiseObjectList<Uni> PropValues = new WwiseObjectList<Uni>(nameof(PropCount));
        }
    }
}
