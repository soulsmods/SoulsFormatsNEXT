using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Audio.Wwise
{
    public interface IWwiseObject
    {
        void Read(BinaryReaderEx br, IWwiseObject parent);
        void Write(BinaryWriterEx bw, IWwiseObject parent);
        int GetFieldAnyIntValue(string fieldName);
        void ReadField(BinaryReaderEx br, string fieldName);
    }
}
