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
        public class CAkSound : WwiseObjectBase<CAkSound>
        {
            public int PluginID;
            public byte StreamType;
            public uint WemFileID;
            public uint InMemoryMediaSize;
            public byte SomeBitField;
            public WwiseConditionalObject<SynthSoundAdditionalParamStruct> SynthSoundAdditionalParam = 
                new WwiseConditionalObject<SynthSoundAdditionalParamStruct>(nameof(PluginID), 0x00650002, 0x00660002);
            public ParamStruct Params = new ParamStruct();

            public class SynthSoundAdditionalParamStruct : WwiseObjectBase<SynthSoundAdditionalParamStruct>
            {
                public uint Size;
            }
        }
    }
}
