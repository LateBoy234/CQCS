using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.StructHelper
{
    public static class EndianConverter
    {
        public static T[] Invert<T>(IEnumerable<T> values)
        {
            return Invert(values, values.Count());
        }

        public static T[] Invert<T>(IEnumerable<T> values, int length)
        {
            T[] buf = new T[length];
            for (int i = 0; i < length / 2; i++)
            {
                T tmp = values.ElementAt(i);
                buf[i] = values.ElementAt(length - 1 - i);
                buf[length - 1 - i] = tmp;
            }
            return buf;
        }

        public static bool InvertEndian(this bool value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return BitConverter.ToBoolean(Invert(bytes), 0);
        }
        public static char InvertEndian(this char value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return BitConverter.ToChar(Invert(bytes), 0);
        }
        public static short InvertEndian(this short value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return BitConverter.ToInt16(Invert(bytes), 0);
        }
        public static ushort InvertEndian(this ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return BitConverter.ToUInt16(Invert(bytes), 0);
        }
        public static int InvertEndian(this int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return BitConverter.ToInt32(Invert(bytes), 0);
        }
        public static uint InvertEndian(this uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return BitConverter.ToUInt32(Invert(bytes), 0);
        }
        public static long InvertEndian(this long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return BitConverter.ToInt64(Invert(bytes), 0);
        }
        public static ulong InvertEndian(this ulong value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return BitConverter.ToUInt64(Invert(bytes), 0);
        }
        public static float InvertEndian(this float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return BitConverter.ToSingle(Invert(bytes), 0);
        }
        public static double InvertEndian(this double value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return BitConverter.ToDouble(Invert(bytes), 0);
        }

        /// <summary>
        /// 如果主机是小端，返回真
        /// </summary>
        public static bool IsLittleEndian { get { return BitConverter.IsLittleEndian; } }

        /// <summary>
        /// 转换为网络字节序字节数组
        /// </summary>
        /// <param name="value">要转换的值</param>
        /// <returns>返回转换后的结果</returns>
        public static byte[] ToNetworkOrderBytes(this bool value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (IsLittleEndian)
                return EndianConverter.Invert(bytes);
            return bytes;
        }
        public static byte[] ToNetworkOrderBytes(this char value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (IsLittleEndian)
                return EndianConverter.Invert(bytes);
            return bytes;
        }
        public static byte[] ToNetworkOrderBytes(this short value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (IsLittleEndian)
                return EndianConverter.Invert(bytes);
            return bytes;
        }
        public static byte[] ToNetworkOrderBytes(this ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (IsLittleEndian)
                return EndianConverter.Invert(bytes);
            return bytes;
        }
        public static byte[] ToNetworkOrderBytes(this int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (IsLittleEndian)
                return EndianConverter.Invert(bytes);
            return bytes;
        }
        public static byte[] ToNetworkOrderBytes(this uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (IsLittleEndian)
                return EndianConverter.Invert(bytes);
            return bytes;
        }
        public static byte[] ToNetworkOrderBytes(this long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (IsLittleEndian)
                return EndianConverter.Invert(bytes);
            return bytes;
        }
        public static byte[] ToNetworkOrderBytes(this ulong value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (IsLittleEndian)
                return EndianConverter.Invert(bytes);
            return bytes;
        }
        public static byte[] ToNetworkOrderBytes(this float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (IsLittleEndian)
                return EndianConverter.Invert(bytes);
            return bytes;
        }
        public static byte[] ToNetworkOrderBytes(this double value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (IsLittleEndian)
                return EndianConverter.Invert(bytes);
            return bytes;
        }

        public static short NetworkOrderBytesToInt16(byte[] bytes)
        {
            if (IsLittleEndian)
                return BitConverter.ToInt16(Invert(bytes, sizeof(short)), 0);
            return BitConverter.ToInt16(bytes, 0);
        }
        public static ushort NetworkOrderBytesToUInt16(byte[] bytes)
        {
            if (IsLittleEndian)
                return BitConverter.ToUInt16(Invert(bytes, sizeof(ushort)), 0);
            return BitConverter.ToUInt16(bytes, 0);
        }
        public static int NetworkOrderBytesToInt32(byte[] bytes)
        {
            if (IsLittleEndian)
                return BitConverter.ToInt32(Invert(bytes, sizeof(int)), 0);
            return BitConverter.ToInt32(bytes, 0);
        }
        public static uint NetworkOrderBytesToUInt32(byte[] bytes)
        {
            if (IsLittleEndian)
                return BitConverter.ToUInt32(Invert(bytes, sizeof(uint)), 0);
            return BitConverter.ToUInt32(bytes, 0);
        }
        public static long NetworkOrderBytesToInt64(byte[] bytes)
        {
            if (IsLittleEndian)
                return BitConverter.ToInt64(Invert(bytes, sizeof(long)), 0);
            return BitConverter.ToInt64(bytes, 0);
        }
        public static ulong NetworkOrderBytesToUInt64(byte[] bytes)
        {
            if (IsLittleEndian)
                return BitConverter.ToUInt64(Invert(bytes, sizeof(ulong)), 0);
            return BitConverter.ToUInt64(bytes, 0);
        }
        public static float NetworkOrderBytesToFloat(byte[] bytes)
        {
            if (IsLittleEndian)
                return BitConverter.ToSingle(Invert(bytes, sizeof(float)), 0);
            return BitConverter.ToSingle(bytes, 0);
        }
        public static double NetworkOrderBytesToDouble(byte[] bytes)
        {
            if (IsLittleEndian)
                return BitConverter.ToDouble(Invert(bytes, sizeof(double)), 0);
            return BitConverter.ToDouble(bytes, 0);
        }
        public static bool NetworkOrderBytesToBool(byte[] bytes)
        {
            if (IsLittleEndian)
                return BitConverter.ToBoolean(Invert(bytes, sizeof(bool)), 0);
            return BitConverter.ToBoolean(bytes, 0);
        }
        public static char NetworkOrderBytesToChar(byte[] bytes)
        {
            if (IsLittleEndian)
                return BitConverter.ToChar(Invert(bytes, sizeof(char)), 0);
            return BitConverter.ToChar(bytes, 0);
        }
    }
}
