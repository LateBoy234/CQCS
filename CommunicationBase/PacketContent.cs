using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ThreeNH.StructHelper;

namespace ThreeNH.CommunicationBase
{

    public enum AlgoParameterType
    {
        Quadratic,
        SVD
    }

    /// <summary>
    /// 数据包的数据内容
    /// </summary>
    public class PacketContent
    {
        private List<byte> _bytes = new List<byte>();

        public PacketContent() { }
        public PacketContent(IEnumerable<byte> data)
        {
            _bytes.AddRange(data);
        }
        public string GetPacketContent()
        {
            return string.Join(" ", _bytes.Select(b => b.ToString("X2")));
        }
        public void Clear() { _bytes.Clear(); }

        public UInt32 Length { get { return (UInt32)_bytes.Count; } }

        public byte[] Bytes { get { return _bytes.ToArray(); } }
        
        public void Enqueue(byte value)
        {
            _bytes.Add(value);
        }
        public void Enqueue(short value)
        {
            _bytes.AddRange(value.ToNetworkOrderBytes());
        }
        public void Enqueue(ushort value)
        {
            _bytes.AddRange(value.ToNetworkOrderBytes());
        }
        public void Enqueue(int value)
        {
            _bytes.AddRange(value.ToNetworkOrderBytes());
        }

        public void Enqueue(int value, AlgoParameterType type = AlgoParameterType.SVD)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            switch (type)
            {
                case AlgoParameterType.SVD:
                    _bytes.AddRange(bytes);
                    break;
                default:
                    _bytes.AddRange(value.ToNetworkOrderBytes());
                    break;
            }
        }

        public void Enqueue(uint value)
        {
            _bytes.AddRange(value.ToNetworkOrderBytes());
        }
        public void Enqueue(long value)
        {
            _bytes.AddRange(value.ToNetworkOrderBytes());
        }
        public void Enqueue(ulong value)
        {
            _bytes.AddRange(value.ToNetworkOrderBytes());
        }
        public void Enqueue(float value)
        {
            _bytes.AddRange(value.ToNetworkOrderBytes());
        }

        public void Enqueue(float value, AlgoParameterType type = AlgoParameterType.Quadratic)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            switch (type)
            {
                case AlgoParameterType.Quadratic:
                    _bytes.AddRange(bytes);
                    break;

                default:
                    _bytes.AddRange(value.ToNetworkOrderBytes());
                    break;
            }
        }

        public void Enqueue(double value)
        {
            _bytes.AddRange(value.ToNetworkOrderBytes());
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
                value = EndianConverter.NetworkOrderBytesToInt16(_bytes.ToArray());
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
            var value = EndianConverter.NetworkOrderBytesToInt16(_bytes.ToArray());
            _bytes.RemoveRange(0, sizeof(Int16));
            return value;
        }


        public bool Dequeue(out ushort value)
        {
            try
            {
                value = EndianConverter.NetworkOrderBytesToUInt16(_bytes.ToArray());
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
            var value = EndianConverter.NetworkOrderBytesToUInt16(_bytes.ToArray());
            _bytes.RemoveRange(0, sizeof(UInt16));
            return value;
        }

        public T[] DequeueArray<T>(Func<T> dequeueFunc, int length)
        {
            return (from i in Enumerable.Range(0, length) select dequeueFunc()).ToArray();
        }


        public bool Dequeue(out int value)
        {
            try
            {
                value = EndianConverter.NetworkOrderBytesToInt32(_bytes.ToArray());
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
            var value = EndianConverter.NetworkOrderBytesToInt32(_bytes.ToArray());
            _bytes.RemoveRange(0, sizeof(Int32));
            return value;
        }

        public bool Dequeue(out uint value)
        {
            try
            {
                value = EndianConverter.NetworkOrderBytesToUInt32(_bytes.ToArray());
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
            var value = EndianConverter.NetworkOrderBytesToUInt32(_bytes.ToArray());
            _bytes.RemoveRange(0, sizeof(UInt32));
            return value;
        }

        public bool Dequeue(out long value)
        {
            try
            {
                value = EndianConverter.NetworkOrderBytesToInt64(_bytes.ToArray());
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
            var value = EndianConverter.NetworkOrderBytesToInt64(_bytes.ToArray());
            _bytes.RemoveRange(0, sizeof(Int64));
            return value;
        }

        public bool Dequeue(out ulong value)
        {
            try
            {
                value = EndianConverter.NetworkOrderBytesToUInt64(_bytes.ToArray());
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
            var value = EndianConverter.NetworkOrderBytesToUInt64(_bytes.ToArray());
            _bytes.RemoveRange(0, sizeof(UInt64));
            return value;
        }

        public bool Dequeue(out float value)
        {
            try
            {
                value = EndianConverter.NetworkOrderBytesToFloat(_bytes.ToArray());
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
            var value = EndianConverter.NetworkOrderBytesToFloat(_bytes.ToArray());
            _bytes.RemoveRange(0, sizeof(float));
            return value;
        }

        public bool Dequeue(out double value)
        {
            try
            {
                value = EndianConverter.NetworkOrderBytesToDouble(_bytes.ToArray());
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
            var value = EndianConverter.NetworkOrderBytesToDouble(_bytes.ToArray());
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
                    Trace.TraceInformation("报文：");
                    Trace.TraceInformation(string.Join(" ", buf.Select(b => b.ToString("X2"))));
                    Trace.TraceInformation("译文：");
                    Trace.TraceInformation(value);
                }
                else
                {
                    value = string.Empty;
                }
                _bytes.RemoveRange(0, length + 1);
            }
            catch (Exception ex)
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
