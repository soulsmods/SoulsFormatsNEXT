using System;

namespace Havoc {
    public static class Debug {
        [Flags]
        public enum DebugInfoType {
            Temporary = 1 << 0,
            ReadProcess = 1 << 1,
            WriteProcess = 1 << 2,
            TypeDef = 1 << 3,
        }

        public static DebugInfoType DebugLevel = 0;

        public static void Log(DebugInfoType type, string format, params object[] args) {
            if (DebugLevel.HasFlag(type)) {
                Console.WriteLine(format, args);
            }
        }

        public static void Temporary(string format, params object[] args) {
            Log(DebugInfoType.Temporary, format, args);
        }

        public static int ReadProcessIndent = 0;
        public static void ReadProcess(string format, params object[] args) {
            Log(DebugInfoType.ReadProcess, new string(' ', 2* ReadProcessIndent) + format, args);
        }

        public static int WriteProcessIndent = 0;
        public static void WriteProcess(string format, params object[] args) {
            Log(DebugInfoType.WriteProcess, new string(' ', 2* WriteProcessIndent) + format, args);
        }

        public static void TypeDef(string format, params object[] args) {
            Log(DebugInfoType.TypeDef, format, args);
        }
    }
}