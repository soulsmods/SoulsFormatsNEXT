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
        public class STID : WwiseBlock
        {
            public STID()
                : base("STID")
            {

            }
            public uint Type;
            public Dictionary<uint, string> BankFileNames = new Dictionary<uint, string>();
            
            public override void InnerRead(BinaryReaderEx br, int sectionLength)
            {
                Type = br.ReadUInt32();
                int count = br.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    uint bankID = br.ReadUInt32();
                    byte stringSize = br.ReadByte();
                    string fileName = br.ReadASCII(stringSize);
                    BankFileNames.Add(bankID, fileName);
                }
            }

            public override void InnerWrite(BinaryWriterEx bw)
            {
                throw new NotImplementedException();
            }
        }
    }
}
