using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Audio.Wwise
{
    public static partial class WwiseObject
    {
        public class Automation3DParam : WwiseObjectBase<Automation3DParam>
        {
            public float RangeX;
            public float RangeY;
            public float RangeZ;
        }
    }
}
