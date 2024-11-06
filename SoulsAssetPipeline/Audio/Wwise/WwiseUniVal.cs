using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Audio.Wwise
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct WwiseUniVal
    {
        [FieldOffset(0)]
        public float FloatVal;

        [FieldOffset(0)]
        public int IntVal;

        [FieldOffset(0)]
        public uint UIntVal;


        public static explicit operator WwiseUniVal(int a) => new WwiseUniVal() { IntVal = a };
        public static explicit operator WwiseUniVal(uint a) => new WwiseUniVal() { UIntVal = a };
        public static explicit operator WwiseUniVal(float a) => new WwiseUniVal() { FloatVal = a };
        public static explicit operator int(WwiseUniVal a) => a.IntVal;
        public static explicit operator uint(WwiseUniVal a) => a.UIntVal;
        public static explicit operator float(WwiseUniVal a) => a.FloatVal;
    }
}
