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
        public class RangedUni : WwiseObjectBase<RangedUni>
        {
            public Uni Min = new Uni();
            public Uni Max = new Uni();
        }
    }
}
