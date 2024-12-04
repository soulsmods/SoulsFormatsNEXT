using System;
using System.Runtime.InteropServices;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace SoulsFormats.Util
{
    public static class Kernel32 {
        [DllImport("kernel32", SetLastError = true)]
        static extern IntPtr LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)]string lpFileName);
        
        [DllImport("kernel32.dll")]
        public static extern int GetLastError();
        public static IntPtr LoadLibrary(string path) {
            return LoadLibraryW(path);
        }
    }
}