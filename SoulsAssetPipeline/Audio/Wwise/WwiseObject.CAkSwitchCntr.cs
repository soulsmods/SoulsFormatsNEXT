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
        public class CAkSwitchCntr : WwiseObjectBase<CAkSwitchCntr>
        {
            public ParamStruct Params = new ParamStruct();
            public byte GroupType;
            public uint GroupID;
            public uint DefaultSwitch;
            public bool IsContinuousValidation;
            public uint NumChildren;
            public WwiseIDList Children = new WwiseIDList(nameof(NumChildren));
            public uint NumSwitchGroups;
            public WwiseObjectList<SwitchGroup> SwitchGroups = 
                new WwiseObjectList<SwitchGroup>(nameof(NumSwitchGroups));
        }
    }
}
