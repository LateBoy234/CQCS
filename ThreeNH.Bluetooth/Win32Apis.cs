using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace ThreeNH.Bluetooth
{
    internal static class Win32Apis
    {
        private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const ushort LANG_NEUTRAL = 0x00;
        private const ushort SUBLANG_DEFAULT = 0x01;

        private static ushort MakeLangId(ushort p, ushort s)
        {
            return (ushort)((s << 10) | p);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int FormatMessage(
            int flag,
            IntPtr source,
            int msgid,
            int landid,
            StringBuilder buf,
            int size,
            IntPtr args);

        public static string GetLastErrorMsg(int lastError)
        {
            IntPtr tempptr = IntPtr.Zero;
            int bufSize = 256;
            StringBuilder buffer = new StringBuilder(bufSize);
            int count = FormatMessage(
                FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                IntPtr.Zero,
                lastError,
                MakeLangId(LANG_NEUTRAL, SUBLANG_DEFAULT),
                buffer,
                bufSize,
                IntPtr.Zero);

            return buffer.ToString(0, count);
        }
    }
}
