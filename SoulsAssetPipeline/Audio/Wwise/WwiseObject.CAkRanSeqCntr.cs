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
        public class CAkRanSeqCntr : WwiseObjectBase<CAkRanSeqCntr>
        {
            public ParamStruct Params = new ParamStruct();
            public ushort LoopCount;
            public ushort LoopModMin;
            public ushort LoopModMax;
            public float TransitionTime;
            public float TransitionTimeModMin;
            public float TransitionTimeModMax;
            public ushort AvoidRepeatCount;
            public byte TransitionMode;
            public byte RandomMode;
            public byte Mode;

            //0:bIsUsingWeight
            //1:bResetPlayListAtEachPlay
            //2:bIsRestartBackward
            //3:bIsContinuous
            //4:bIsGlobal
            public byte BitField;

            public int NumChildren;
            public WwiseIDList Children = new WwiseIDList(nameof(NumChildren));

            public ushort PlayListItemCount;
            public WwiseObjectList<PlayListItem> PlayListItems = 
                new WwiseObjectList<PlayListItem>(nameof(PlayListItemCount));
            public uint RollRandomPlayListItem(Random rand)
            {
                double totalOdds = 0;
                foreach (var pli in PlayListItems)
                    totalOdds += pli.Weight >= 0 ? pli.Weight : 50000;

                double currentOdds = 0;
                var randValue = rand.NextDouble() * totalOdds;
                foreach (var pli in PlayListItems)
                {
                    currentOdds += pli.Weight;
                    if (randValue <= currentOdds)
                        return pli.PlayID;
                }
                return 0;
            }
        }
    }
}
