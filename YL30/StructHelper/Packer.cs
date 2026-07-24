using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YL30
{
    public class Packer
    {
        private List<byte> _bytes = new List<byte>();

        public Packer() { }
        public Packer(IEnumerable<byte> data)
        {
            _bytes.AddRange(data);
        }

        public void Clear() { _bytes.Clear(); }

        public int Length => _bytes.Count;

        public byte[] Bytes => _bytes.ToArray();

        public void Enqueue(byte value)
        {
            _bytes.Add(value);
        }
        public void Enqueue(short value)
        {
            _bytes.AddRange(BitConverter.GetBytes(value));
        }
        public void Enqueue(ushort value)
        {
            _bytes.AddRange(BitConverter.GetBytes(value));
        }
        public void Enqueue(int value)
        {
            _bytes.AddRange(BitConverter.GetBytes(value));
        }
        public void Enqueue(uint value)
        {
            _bytes.AddRange(BitConverter.GetBytes(value));
        }
        public void Enqueue(long value)
        {
            _bytes.AddRange(BitConverter.GetBytes(value));
        }
        public void Enqueue(ulong value)
        {
            _bytes.AddRange(BitConverter.GetBytes(value));
        }
        public void Enqueue(float value)
        {
            _bytes.AddRange(BitConverter.GetBytes(value));
        }
        public void Enqueue(double value)
        {
            _bytes.AddRange(BitConverter.GetBytes(value));
        }
        /// <summary>
        /// 向要发送的包中添加一个字符串，字符串转换为字节串后的大小不能超过255个字符
        /// 字符串被转换成utf8编码，以其长度开始，后跟字符
        /// </summary>
        /// <param name="value"></param>
        public void Enqueue(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length > 255)
            {
                throw new ArgumentException("string too long");
            }
            _bytes.Add((byte)bytes.Length);
            _bytes.AddRange(bytes);
        }

        public void Enqueue(byte[] values)
        {
            _bytes.AddRange(values);
        }

        public void Enqueue(short[] values)
        {
            foreach (var value in values)
            {
                Enqueue(value);
            }
        }

        public void Enqueue(ushort[] values)
        {
            foreach (var value in values)
            {
                Enqueue(value);
            }
        }

        public void Enqueue(int[] values)
        {
            foreach (var value in values)
            {
                Enqueue(value);
            }
        }

        public void Enqueue(uint[] values)
        {
            foreach (var value in values)
            {
                Enqueue(value);
            }
        }


        public void Enqueue(long[] values)
        {
            foreach (var value in values)
            {
                Enqueue(value);
            }
        }

        public void Enqueue(ulong[] values)
        {
            foreach (var value in values)
            {
                Enqueue(value);
            }
        }

        public void Enqueue(float[] values)
        {
            foreach (var value in values)
            {
                Enqueue(value);
            }
        }

        public void Enqueue(double[] values)
        {
            foreach (var value in values)
            {
                Enqueue(value);
            }
        }

        /// <summary>
        /// 压入字符数组，字符必须是ASCII
        /// </summary>
        /// <param name="arr"></param>
        public void Enqueue(char[] arr)
        {
            foreach (var ch in arr)
            {
                Enqueue((byte)ch);
            }
        }

        public void Fill(int n, byte val=0)
        {
            for (var i = 0; i < n; i++)
            {
                _bytes.Add(val);
            }
        }

        /// <summary>
        /// 将数据包4字符对齐
        /// </summary>
        public void Align()
        {
            int n = 4 - (_bytes.Count & 0x03);
            if (n != 4)
            {
                Fill(n);
            }
        }

        /// <summary>
        /// 将数据对齐到指定字节
        /// </summary>
        /// <param name="align">要对齐的字符数，必须是2、4、8、16、64</param>
        public void Align(int align)
        {
            int mask = align - 1;
            if ((mask & align) != 0)
            {
                throw new ArgumentException("align must be 2^n");
            }

            int n = align - (mask & _bytes.Count);
            if (n != align)
            {
                Fill(n);
            }
        }

        /// <summary>
        /// 压入字符数组，数组长度为n
        /// </summary>
        /// <param name="str">要压入的字符，必须是ASCII字符串，如果长度不足n，后面补0</param>
        /// <param name="n">字符数组的长度</param>
        public void Enqueue(string str, int n)
        {
            for (int i = 0; i < n; i++)
            {
                if (i < str.Length)
                {
                    _bytes.Add((byte)str[i]);
                }
                else
                {
                    _bytes.Add(0);
                }
            }
        }

        public bool Dequeue(out byte value)
        {
            try
            {
                value = _bytes[0];
                _bytes.RemoveAt(0);
            }
            catch (Exception)
            {
                value = 0;
                return false;
            }
            return true;
        }

        public byte DequeueByte()
        {
            var value = _bytes.First();
            _bytes.RemoveAt(0);
            return value;
        }

        public bool Dequeue(out short value)
        {
            try
            {
                value = BitConverter.ToInt16(_bytes.ToArray(), 0);
                _bytes.RemoveRange(0, sizeof(short));
            }
            catch (Exception)
            {
                value = 0;
                return false;
            }
            return true;
        }

        public Int16 DequeueInt16()
        {
            var value = BitConverter.ToInt16(_bytes.ToArray(), 0);
            _bytes.RemoveRange(0, sizeof(Int16));
            return value;
        }


        public bool Dequeue(out ushort value)
        {
            try
            {
                value = BitConverter.ToUInt16(_bytes.ToArray(), 0);
                _bytes.RemoveRange(0, sizeof(ushort));
            }
            catch (Exception)
            {
                value = 0;
                return false;
            }
            return true;
        }

        public UInt16 DequeueUInt16()
        {
            var value = BitConverter.ToUInt16(_bytes.ToArray(), 0);
            _bytes.RemoveRange(0, sizeof(UInt16));
            return value;
        }


        public bool Dequeue(out int value)
        {
            try
            {
                value = BitConverter.ToInt32(_bytes.ToArray(), 0);
                _bytes.RemoveRange(0, sizeof(int));
            }
            catch (Exception)
            {
                value = 0;
                return false;
            }
            return true;
        }

        public Int32 DequeueInt32()
        {
            var value = BitConverter.ToInt32(_bytes.ToArray(), 0);
            _bytes.RemoveRange(0, sizeof(Int32));
            return value;
        }

        public bool Dequeue(out uint value)
        {
            try
            {
                value = BitConverter.ToUInt32(_bytes.ToArray(), 0);
                _bytes.RemoveRange(0, sizeof(uint));
            }
            catch (Exception)
            {
                value = 0;
                return false;
            }
            return true;
        }

        public UInt32 DequeueUInt32()
        {
            var value = BitConverter.ToUInt32(_bytes.ToArray(), 0);
            _bytes.RemoveRange(0, sizeof(UInt32));
            return value;
        }

        public bool Dequeue(out long value)
        {
            try
            {
                value = BitConverter.ToInt64(_bytes.ToArray(), 0);
                _bytes.RemoveRange(0, sizeof(long));
            }
            catch (Exception)
            {
                value = 0;
                return false;
            }
            return true;
        }

        public Int64 DequeueInt64()
        {
            var value = BitConverter.ToInt64(_bytes.ToArray(), 0);
            _bytes.RemoveRange(0, sizeof(Int64));
            return value;
        }

        public bool Dequeue(out ulong value)
        {
            try
            {
                value = BitConverter.ToUInt64(_bytes.ToArray(), 0);
                _bytes.RemoveRange(0, sizeof(ulong));
            }
            catch (Exception)
            {
                value = 0;
                return false;
            }
            return true;
        }

        public UInt64 DequeueUInt64()
        {
            var value = BitConverter.ToUInt64(_bytes.ToArray(), 0);
            _bytes.RemoveRange(0, sizeof(UInt64));
            return value;
        }

        public bool Dequeue(out float value)
        {
            try
            {
                value = BitConverter.ToSingle(_bytes.ToArray(), 0);
                _bytes.RemoveRange(0, sizeof(float));
            }
            catch (Exception)
            {
                value = 0;
                return false;
            }
            return true;
        }

        public float DequeueFloat()
        {
            var value = BitConverter.ToSingle(_bytes.ToArray(), 0);
            _bytes.RemoveRange(0, sizeof(float));
            return value;
        }

        public bool Dequeue(out double value)
        {
            try
            {
                value = BitConverter.ToDouble(_bytes.ToArray(), 0);
                _bytes.RemoveRange(0, sizeof(double));
            }
            catch (Exception)
            {
                value = 0;
                return false;
            }
            return true;
        }

        public double DequeueDouble()
        {
            var value = BitConverter.ToDouble(_bytes.ToArray(), 0);
            _bytes.RemoveRange(0, sizeof(double));
            return value;
        }

        public bool Dequeue(out string value)
        {
            try
            {
                int length = _bytes[0];
                if (length > 0)
                {
                    byte[] buf = new byte[length];
                    for (int i = 1; i <= length; i++)
                    {
                        buf[i - 1] = _bytes[i];
                    }
                    int n = Array.IndexOf(buf, (byte)0);
                    value = Encoding.ASCII.GetString(buf, 0, n < 0 ? length : n);
                }
                else
                {
                    value = string.Empty;
                }
                _bytes.RemoveRange(0, length + 1);
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
            return true;
        }

        public string DequeueString()
        {
            string value = string.Empty;
            int length = _bytes[0];
            if (length > 0)
            {
                byte[] buf = new byte[length];
                for (int i = 1; i <= length; i++)
                {
                    buf[i - 1] = _bytes[i];
                }
                int n = Array.IndexOf(buf, (byte)0);
                value = Encoding.ASCII.GetString(buf, 0, n < 0 ? length : n);
            }
            _bytes.RemoveRange(0, length + 1);

            return value;
        }

        /// <summary>
        /// 弹出一个字符数组做为字符串
        /// </summary>
        /// <param name="n">字符数组的长度</param>
        /// <returns></returns>
        public string DequeueString(int n)
        {
            return BitConverter.ToString(_bytes.ToArray(), 0, n);
        }

        /// <summary>
        /// 弹出字符数组
        /// </summary>
        /// <param name="n">字符数组长度</param>
        /// <returns></returns>
        public char[] DequeueChars(int n)
        {
            char[] arr = new char[n];
            for (int i = 0; i < n; i++)
            {
                arr[i] = (char)_bytes[i];
            }
            _bytes.RemoveRange(0, n);
            return arr;
        }


        /// <summary>
        /// 丢弃指定字节的数据
        /// </summary>
        /// <param name="byteCount">要丢弃的字节数</param>
        public void Discard(int byteCount)
        {
            _bytes.RemoveRange(0, byteCount);
        }

        public UInt16 CheckSum
        {
            get
            {
                UInt16 checksum = 0;
                foreach (byte item in Bytes)
                {
                    checksum += item;
                }
                return checksum;
            }
        }
    }
}