using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Audio.Wwise
{
    public abstract partial class WwiseBlock
    {
        public readonly string FourCC;

        public abstract void InnerRead(BinaryReaderEx br, int sectionLength);
        public abstract void InnerWrite(BinaryWriterEx bw);

        public WwiseBlock(string fourCC)
        {
            FourCC = fourCC;
        }

        //public void Read(BinaryReaderEx br)
        //{
        //    br.AssertASCII(FourCC);
        //    int sectionLength = br.ReadInt32();
        //    InnerRead(br, sectionLength);
        //}

        public void Write(BinaryWriterEx bw)
        {
            bw.WriteASCII(FourCC);
            bw.ReserveUInt32($"WwiseSection.{FourCC}.Length");
            var startOfSection = bw.Position;
            InnerWrite(bw);
            bw.FillUInt32($"WwiseSection.{FourCC}.Length", (uint)(bw.Position - startOfSection));
        }

        
    }
}
