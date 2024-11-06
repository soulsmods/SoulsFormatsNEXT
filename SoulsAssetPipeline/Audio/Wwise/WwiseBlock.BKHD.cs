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
        public class BKHD : WwiseBlock
        {
            public BKHD()
                : base("BKHD")
            {

            }
            public int BankGeneratorVersion;
            public int SoundBankID;
            public int LanguageID;
            public ushort Alignment;
            public ushort DeviceAllocated;
            public uint ProjectID;
            public override void InnerRead(BinaryReaderEx br, int sectionLength)
            {
                BankGeneratorVersion = br.ReadInt32();
                SoundBankID = br.ReadInt32();
                LanguageID = br.ReadInt32();
                Alignment = br.ReadUInt16();
                DeviceAllocated = br.ReadUInt16();
                ProjectID = br.ReadUInt32();
            }

            public override void InnerWrite(BinaryWriterEx bw)
            {
                throw new NotImplementedException();
            }
        }
    }
}
