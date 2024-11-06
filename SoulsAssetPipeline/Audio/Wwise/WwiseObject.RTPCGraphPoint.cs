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
        public class RTPCGraphPoint : WwiseObjectBase<RTPCGraphPoint>
        {
            public float From;
            public float To;

            //0:Log3
            //1:Sine
            //2:Log1
            //3:InvSCurve
            //4:Linear
            //5:SCurve
            //6:Exp1
            //7:SineRecip
            //8:Exp3
            //9:Constant
            public uint InterpolationType;
        }
    }
}
