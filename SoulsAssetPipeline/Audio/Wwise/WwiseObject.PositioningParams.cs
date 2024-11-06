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
        public class PositioningParams : WwiseObjectBase<PositioningParams>
        {
            public byte BitField;
            public bool PositioningInfoOverrideParent;
            public bool Has3D;
            public byte PositioningParamsBitField3D;
            public int Pos3DType;
            public PosParams3D Params3D = null;

            internal override bool CustomRead(BinaryReaderEx br, IWwiseObject parent)
            {
                BitField = br.ReadByte();
                PositioningInfoOverrideParent = (BitField & 1) != 0;
                if (PositioningInfoOverrideParent)
                {
                    Has3D = ((BitField >> 1) & 1) != 0;
                    if (Has3D)
                    {
                        PositioningParamsBitField3D = br.ReadByte();
                    }
                }
                Pos3DType = (BitField >> 5) & 3;
                if (Pos3DType != 0)
                {
                    Params3D = new PosParams3D();
                    Params3D.Read(br, this);
                }
                else
                {
                    Params3D = null;
                }

                return true;
            }

            internal override bool CustomWrite(BinaryWriterEx bw, IWwiseObject parent)
            {
                throw new NotImplementedException();
            }

            public class PosParams3D : WwiseObjectBase<PosParams3D>
            {
                public byte PathMode;
                public uint TransitionTime;
                public int NumVertices;
                public WwiseObjectList<PathVertex> PathVertices = 
                    new WwiseObjectList<PathVertex>(nameof(NumVertices));
                public int NumPathListItem;
                public WwiseObjectList<PathListItem> PathListItems = 
                    new WwiseObjectList<PathListItem>(nameof(NumPathListItem));
                public WwiseObjectList<Automation3DParam> Automation3DParams = 
                    new WwiseObjectList<Automation3DParam>(nameof(NumPathListItem));
            }
        }
    }
}
