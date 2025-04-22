using System.Runtime.CompilerServices;

namespace SoulsFormats
{
    internal static class MathHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int BinaryAlign(int num, int alignment)
            => (num + (--alignment)) & ~alignment;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long BinaryAlign(long num, long alignment)
            => (num + (--alignment)) & ~alignment;

        internal static int Align(int value, int alignment)
        {
            var remainder = value % alignment;
            if (remainder > 0)
            {
                return value + (alignment - remainder);
            }
            return value;
        }

        internal static long Align(long value, long alignment)
        {
            var remainder = value % alignment;
            if (remainder > 0)
            {
                return value + (alignment - remainder);
            }
            return value;
        }
    }
}
