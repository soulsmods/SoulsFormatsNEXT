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
        [DllImport("libdl.dylib", EntryPoint = "dlopen")]
        static extern IntPtr MacOS_dlopen([MarshalAs(UnmanagedType.LPUTF8Str)] string filename, int flag);

        [DllImport("libdl.dylib", EntryPoint = "dlerror")]
        static extern IntPtr MacOS_dlerror();

        [DllImport("libdl.dylib", EntryPoint = "dlclose")]
        static extern int MacOS_dlclose(IntPtr handle);

        [DllImport("libdl.dylib", EntryPoint = "dlsym")]
        static extern IntPtr MacOS_dlsym(IntPtr handle, string symbol);

        [DllImport("libdl.so.2", EntryPoint = "dlopen")]
        static extern IntPtr Linux_dlopen([MarshalAs(UnmanagedType.LPUTF8Str)]string filename, int flag);

        [DllImport("libdl.so.2", EntryPoint = "dlerror")]
        static extern IntPtr Linux_dlerror();

        [DllImport("libdl.so.2", EntryPoint = "dlclose")]
        static extern int Linux_dlclose(IntPtr handle);

        [DllImport("libdl.so.2", EntryPoint = "dlsym")]
        static extern IntPtr Linux_dlsym(IntPtr handle, string symbol);

        static IntPtr dlopen([MarshalAs(UnmanagedType.LPUTF8Str)] string filename, int flag) => RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? MacOS_dlopen(filename, flag) : Linux_dlopen(filename, flag);

        static IntPtr dlerror() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? MacOS_dlerror() : Linux_dlerror();

        static int dlclose(IntPtr handle) => RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? MacOS_dlclose(handle) : Linux_dlclose(handle);

        static IntPtr dlsym(IntPtr handle, string symbol) => RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? MacOS_dlsym(handle, symbol) : Linux_dlsym(handle, symbol);

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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && !Path.GetFileName(path).ToLower().EndsWith(".dylib"))
            {
                path = Path.Combine(Path.GetDirectoryName(path) ?? "", "lib" + Path.GetFileNameWithoutExtension(path) + ".dylib");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !Path.GetFileName(path).ToLower().Contains(".so"))
            {
                path = Path.Combine(Path.GetDirectoryName(path) ?? "", "lib" + Path.GetFileNameWithoutExtension(path) + ".so");
            }
            return dlopen(path, flag);
        }

        public static int FreeLibrary(IntPtr handle)
        {
            return dlclose(handle);
        }

        public static IntPtr GetAddress(IntPtr handle, string symbol)
        {
            return dlsym(handle, symbol);
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Libdl.FreeLibrary(handle);
            }
            throw new PlatformNotSupportedException($"Unsupported Platform {RuntimeInformation.OSDescription}");
        }
    }
}
