using SoulsFormats;

namespace SoulsFormatsExtensions
{
    public static class ExtensionMethods
    {


        public static int ReadFXR1Varint(this BinaryReaderEx br)
        {
            int result = br.ReadInt32();
            br.AssertFXR1Garbage();
            return result;
        }

        public static void AssertFXR1Garbage(this BinaryReaderEx br)
        {
            if (br.VarintLong)
                br.AssertUInt32(0, 0xCDCDCDCD);
        }

        public static int GetFXR1Varint(this BinaryReaderEx br, long offset)
        {
            int result = -1;
            br.StepIn(offset);
            result = br.ReadInt32();
            br.AssertFXR1Garbage();
            br.StepOut();
            return result;
        }

        public static int AssertFXR1Varint(this BinaryReaderEx br, params int[] v)
        {
            int result = br.AssertInt32(v);
            br.AssertFXR1Garbage();
            return result;
        }

        public static float ReadFXR1Single(this BinaryReaderEx br)
        {
            float result = br.ReadSingle();
            br.AssertFXR1Garbage();
            return result;
        }

        public static void WriteFXR1Garbage(this BinaryWriterEx bw)
        {
            //if (bw.VarintLong)
            //    bw.WriteUInt32(0xCDCDCDCD);
            if (bw.VarintLong)
                bw.WriteUInt32(0);
        }

        public static void WriteFXR1Varint(this BinaryWriterEx bw, int v)
        {
            bw.WriteInt32(v);
            bw.WriteFXR1Garbage();
        }

        public static void ReserveFXR1Varint(this BinaryWriterEx bw, string name)
        {
            bw.ReserveInt32(name);
            bw.WriteFXR1Garbage();
        }

        public static void FillFXR1Varint(this BinaryWriterEx bw, string name, int v)
        {
            bw.FillInt32(name, v);
            //bw.WriteFXR1Garbage();
        }

        public static void WriteFXR1Single(this BinaryWriterEx bw, float v)
        {
            bw.WriteSingle(v);
            bw.WriteFXR1Garbage();
        }
    }
}
