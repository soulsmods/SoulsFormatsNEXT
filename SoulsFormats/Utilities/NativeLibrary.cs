using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace SoulsFormats
{
    /// <summary>
    /// Provides methods for loading libraries on Windows.
    /// </summary>
    public static class Kernel32
    {
        [DllImport("kernel32", SetLastError = true)]
        static extern IntPtr LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)]string lpFileName);

        /// <summary>
        /// Returns a string describing the last error that occurred during dynamic linking.
        /// </summary>
        [DllImport("kernel32.dll")]
        public static extern int GetLastError();

        /// <summary>
        /// Loads a library with the specified filename.
        /// </summary>
        /// <param name="path">The name of the library to load.</param>
        /// <returns>A handle to the loaded library, or IntPtr.Zero if the library could not be loaded.</returns>
        public static IntPtr LoadLibrary(string path)
        {
            if (!path.EndsWith(".dll"))
            {
                path += ".dll";
            }
            return LoadLibraryW(path);
        }

        [DllImport("kernel32.dll", SetLastError=true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);
    }

    /// <summary>
    /// Provides methods for loading shared libraries on Linux.
    /// </summary>
    public static class Libdl
    {
        [DllImport("libdl.so.2")]
        static extern IntPtr dlopen([MarshalAs(UnmanagedType.LPUTF8Str)]string filename, int flag);

        [DllImport("libdl.so.2")]
        static extern IntPtr dlerror();

        [DllImport("libdl.so.2")]
        static extern int dlclose(IntPtr handle);

        /// <summary>
        /// Returns a string describing the last error that occurred during dynamic linking.
        /// </summary>
        public static string GetLastError()
        {
            IntPtr error = dlerror();
            if (error == IntPtr.Zero)
            {
                return null;
            }
            return Marshal.PtrToStringAnsi(error);
        }

        /// <summary>
        /// Loads a shared library with the specified filename and flag.
        /// </summary>
        /// <param name="filename">The name of the shared library to load.</param>
        /// <param name="flag">
        ///     The flag that specifies the behavior of the loading process. Default is 1 (RTLD_LAZY).
        ///     RTLD_LAZY = 1, RTLD_NOW = 2, RTLD_GLOBAL = 256, RTLD_LOCAL = 0
        /// </param>
        /// <returns>A handle to the loaded library, or IntPtr.Zero if the library could not be loaded.</returns>
        public static IntPtr LoadLibrary(string path, int flag = 1)
        {
            if (!Path.GetFileName(path).ToLower().Contains(".so"))
            {
                path = Path.Combine(Path.GetDirectoryName(path) ?? "", "lib" + Path.GetFileNameWithoutExtension(path) + ".so");
            }
            return dlopen(path, flag);
        }

        public static int FreeLibrary(IntPtr handle)
        {
            return dlclose(handle);
        }
    }

    /// <summary>
    /// Provides methods for loading native libraries and retrieving error messages.
    /// </summary>
    public static class NativeLibrary
    {
        /// <summary>
        /// Loads a native library with the specified path.
        /// </summary>
        /// <param name="path">The path to the native library to load.</param>
        /// <returns>A handle to the loaded library, or IntPtr.Zero if the library could not be loaded.</returns>
        public static IntPtr LoadLibrary(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Kernel32.LoadLibrary(path);
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Libdl.LoadLibrary(path);
            }
            throw new PlatformNotSupportedException($"Unsupported Platform {RuntimeInformation.OSDescription}");
        }

        /// <summary>
        /// Retrieves the last error message from the native library loading process.
        /// </summary>
        /// <returns>A string describing the last error that occurred.</returns>
        public static string GetLastError()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var error = Kernel32.GetLastError();
                return new Win32Exception(error).Message;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Libdl.GetLastError();
            }
            return $"Unsupported Platform {RuntimeInformation.OSDescription}";
        }

        public static int FreeLibrary(IntPtr handle)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Convert.ToInt32(Kernel32.FreeLibrary(handle));
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Libdl.FreeLibrary(handle);
            }
            throw new PlatformNotSupportedException($"Unsupported Platform {RuntimeInformation.OSDescription}");
        }
    }
}
