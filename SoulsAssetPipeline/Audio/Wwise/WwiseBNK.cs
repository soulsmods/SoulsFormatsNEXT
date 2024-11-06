using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Audio.Wwise
{
    public class WwiseBNK : SoulsFormats.SoulsFile<WwiseBNK>, IDisposable
    {
        public object ThreadLockObject = new object();

        public WwiseBlock.BKHD BKHD;
        public WwiseBlock.DATA DATA;
        public WwiseBlock.DIDX DIDX;
        public WwiseBlock.HIRC HIRC;
        public WwiseBlock.STID STID;

        protected override void Read(BinaryReaderEx br)
        {
            while (true)
            {
                if ((br.Length - br.Position) <= 8)
                    break;
                string fourCC = br.ReadASCII(4);

                if (fourCC == "\0\0\0\0")
                {
                    break;
                }

                int sectionLength = br.ReadInt32();

                var sectionStart = br.Position;

                if (fourCC == "BKHD")
                {
                    BKHD = new WwiseBlock.BKHD();
                    BKHD.InnerRead(br, sectionLength);
                }
                else if (fourCC == "DATA")
                {
                    DATA = new WwiseBlock.DATA();
                    DATA.InnerRead(br, sectionLength);
                }
                else if (fourCC == "DIDX")
                {
                    DIDX = new WwiseBlock.DIDX();
                    DIDX.InnerRead(br, sectionLength);
                }
                else if (fourCC == "HIRC")
                {
                    HIRC = new WwiseBlock.HIRC();
                    HIRC.InnerRead(br, sectionLength);
                }
                else if (fourCC == "STID")
                {
                    STID = new WwiseBlock.STID();
                    STID.InnerRead(br, sectionLength);
                }

                br.Position = sectionStart + sectionLength;
            }

            Console.WriteLine("breakpoint");
        }

        protected override void Write(BinaryWriterEx bw)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            DATA?.binaryReader?.Stream?.Dispose();
            HIRC?.objFetchBinaryReader?.Stream?.Dispose();
        }
    }
}
