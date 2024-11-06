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
        public class ParamStruct : WwiseObjectBase<ParamStruct>
        {
            public FXParams FX = new FXParams();
            public bool OverrideAttachmentParams;
            public uint OverrideBuxObjID;
            public uint ParentObjID;
            public byte SomeBitField;
            public PropertyBundle Props = new PropertyBundle();
            public RangedPropertyBundle RangedProps = new RangedPropertyBundle();
            public PositioningParams PosParams = new PositioningParams();
            public AuxParamContainer AuxParams = new AuxParamContainer();
            public AdvSettingsParams AdvSettings = new AdvSettingsParams();
            public StateChunk StateChunk = new StateChunk();
            public InitialRTPC InitialRTPC = new InitialRTPC();
        }
    }
}
