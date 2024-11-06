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
        public class DATA : WwiseBlock
        {
            public DATA()
                : base("DATA")
            {

            }

            private byte[] Data;
            internal BinaryReaderEx binaryReader;

            private object _lock_GetSection = new object();
            public byte[] GetSection(int start, int numBytes)
            {
                byte[] result = null;
                lock (_lock_GetSection)
                {
                    result = binaryReader.GetBytes(start, numBytes); ;
                }
                return result;
            }
            
            public override void InnerRead(BinaryReaderEx br, int sectionLength)
            {
                Data = br.ReadBytes((int)sectionLength);
                binaryReader = new BinaryReaderEx(false, Data);
            }

            public override void InnerWrite(BinaryWriterEx bw)
            {
                throw new NotImplementedException();
            }
        }
    }
}
