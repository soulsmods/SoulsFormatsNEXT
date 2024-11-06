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
        public class Uni : WwiseObjectBase<Uni>
        {
            private WwiseUniVal trueVal;
            public uint ValueAsUInt
            {
                get => trueVal.UIntVal;
                set => trueVal.UIntVal = value;
            }

            public int ValueAsInt
            {
                get => trueVal.IntVal;
                set => trueVal.IntVal = value;
            }

            public float ValueAsFloat
            {
                get => trueVal.FloatVal;
                set => trueVal.FloatVal = value;
            }


            internal override bool CustomRead(BinaryReaderEx br, IWwiseObject parent)
            {
                trueVal.UIntVal = br.ReadUInt32();
                return true;
            }

            internal override bool CustomWrite(BinaryWriterEx bw, IWwiseObject parent)
            {
                bw.WriteUInt32(trueVal.UIntVal);
                return true;
            }
        }
    }
}
