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
        public class CAkLayerCntr : WwiseObjectBase<CAkLayerCntr>
        {
            public ParamStruct Params = new ParamStruct();
            public int NumChildren;
            public WwiseIDList Children = new WwiseIDList(nameof(NumChildren));

            public int NumLayers;
            public WwiseObjectList<Layer> Layers = new WwiseObjectList<Layer>(nameof(NumLayers));
        }
    }
}
