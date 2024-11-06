using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Audio.Wwise
{
    public class WwiseVarint
    {
        public int Value;
        public static WwiseVarint Read(BinaryReaderEx br)
        {
            byte cur = br.ReadByte();
            var value = cur & 0x7F;
            int repititions = 0;
            while ((byte)(cur & 0x80) != 0 && repititions < 10)
            {
                cur = br.ReadByte();
                value = (value << 7) | (cur & 0x7F);
                repititions++;
            }
            return new WwiseVarint() { Value = value };
        }
        public void Write(BinaryWriterEx bw)
        {
            throw new NotImplementedException();
        }
    }
}
