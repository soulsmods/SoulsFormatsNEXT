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
        public class DIDX : WwiseBlock
        {
            public DIDX()
                : base("DIDX")
            {

            }

            public class File
            {
                public uint WemFileID;
                public int DataSectionStart;
                public int DataSectionNumBytes;
            }

            public Dictionary<uint, File> Files = new Dictionary<uint, File>();
            
            public override void InnerRead(BinaryReaderEx br, int sectionLength)
            {
                int numFiles = sectionLength / 12;
                Files.Clear();
                for (int i = 0; i < numFiles; i++)
                {
                    var wemFileID = br.ReadUInt32();
                    var dataSectionStart = br.ReadInt32();
                    var dataSectionNumBytes = br.ReadInt32();
                    Files.Add(wemFileID, new File()
                    {
                        WemFileID = wemFileID,
                        DataSectionStart = dataSectionStart,
                        DataSectionNumBytes = dataSectionNumBytes,
                    });
                }
            }

            public override void InnerWrite(BinaryWriterEx bw)
            {
                throw new NotImplementedException();
            }
        }
    }
}
