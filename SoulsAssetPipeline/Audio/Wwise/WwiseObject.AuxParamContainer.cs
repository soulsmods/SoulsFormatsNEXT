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
        public class AuxParamContainer : WwiseObjectBase<AuxParamContainer>
        {
            public byte AuxParamsBitField;
            public bool HasAux => (AuxParamsBitField & (1 << 3)) != 0;
            public uint AuxID0;
            public uint AuxID1;
            public uint AuxID2;
            public uint AuxID3;
            public int ReflectionsAuxBus;

            internal override bool CustomRead(BinaryReaderEx br, IWwiseObject parent)
            {
                AuxParamsBitField = br.ReadByte();
                if (HasAux)
                {
                    AuxID0 = br.ReadUInt32();
                    AuxID1 = br.ReadUInt32();
                    AuxID2 = br.ReadUInt32();
                    AuxID3 = br.ReadUInt32();
                }
                ReflectionsAuxBus = br.ReadInt32();
                return true;
            }

            internal override bool CustomWrite(BinaryWriterEx bw, IWwiseObject parent)
            {
                bw.WriteByte(AuxParamsBitField);
                if (HasAux)
                {
                    bw.WriteUInt32(AuxID0);
                    bw.WriteUInt32(AuxID1);
                    bw.WriteUInt32(AuxID2);
                    bw.WriteUInt32(AuxID3);
                }
                bw.WriteInt32(ReflectionsAuxBus);
                return true;
            }
        }
    }
}
