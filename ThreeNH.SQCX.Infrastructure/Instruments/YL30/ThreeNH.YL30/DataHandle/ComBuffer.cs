using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ThreeNH.SQCX.Infrastructure.Instruments.YL30;
using ThreeNH.SQCX.Infrastructure.Instruments.YL30.StructHelper;

namespace ThreeNH.YL30.DataHandle
{
    public static class ComBuffer
    {

        // 线程安全队列
        private static readonly ConcurrentQueue<byte> _sendQueue = new ConcurrentQueue<byte>();

        private static readonly ConcurrentQueue<byte> _recieveQueue = new ConcurrentQueue<byte>();

        /// <summary>
        /// 外部调用：把一次发送的通信数据塞进缓存
        /// </summary>
        public static void AppendSendDatas(byte[] bytes)
        {
            while (_sendQueue.TryDequeue(out _)) { }

            if (bytes == null || bytes.Length == 0) return;

            // 做一份拷贝，防止外部数组被复用或修改
            var copy = new byte[bytes.Length];
            Buffer.BlockCopy(bytes, 0, copy, 0, bytes.Length);

            // 入队
            foreach (var b in copy)
                _sendQueue.Enqueue(b);
        }

        /// <summary>
        /// 外部调用：把一次接收的通信数据塞进缓存
        /// </summary>
        public static void AppendRecieveDatas(byte[] bytes)
        {
            while (_recieveQueue.TryDequeue(out _)) { }

            if (bytes == null || bytes.Length == 0) return;

            // 做一份拷贝，防止外部数组被复用或修改
            var copy = new byte[bytes.Length];
            Buffer.BlockCopy(bytes, 0, copy, 0, bytes.Length);

            // 入队
            foreach (var b in copy)
                _recieveQueue.Enqueue(b);
        }

        /// <summary>
        /// 大端转小端
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static  byte[] BigEndianToLittleEndian(List<byte> values)
        {
            if (values.Count != Constants.STANDARD_COUNT * 2)
            {
                throw new ArgumentException("the length of the bytes does not match");
            }
            Dictionary<string, byte[]> content = new Dictionary<string, byte[]>
            {
                ["colorValue"] = new byte[40],
                ["sciSce"] = new byte[124],
                ["id"] = new byte[4],
                ["name"] = new byte[16],
                ["time"] = new byte[12],
            };
            if (content.Values.SelectMany(x => x.ToList()).ToList().Count != values.Count)
            {
                throw new ArgumentException("the length of the bytes does not match");
            }
            //颜色空间string值
            int offset = 0;
            content["colorValue"] = values.TakeRange(offset, content["colorValue"].Length).ToArray();
            offset += content["colorValue"].Length;
            content["sciSce"] = EndianConverter.BigEndianToLittleEndianForUShort(values.TakeRange(offset, content["sciSce"].Length).ToList()).ToArray();
            offset += content["sciSce"].Length;
            content["id"] = values.TakeRange(offset, content["id"].Length).ToArray();
            offset += content["id"].Length;
            content["name"] = values.TakeRange(offset, content["name"].Length).ToArray();
            offset += content["name"].Length;
            content["time"] = EndianConverter.BigEndianToLittleEndianForUShort(values.TakeRange(offset, content["time"].Length).ToList()).ToArray();
            var bytes = content.Values.SelectMany(x => x.ToList()).ToArray();
            return bytes;
        }

        public static byte[] SanmpleBigEndianToLittleEndian(List<byte> values)
        {
            if (values.Count != Constants.SAMPLE_COUNT * 2)
            {
                throw new ArgumentException("the length of the bytes does not match");
            }
            Dictionary<string, byte[]> content = new Dictionary<string, byte[]>
            {
                ["colorValue"] = new byte[60],
                ["sciSce"] = new byte[124],
                ["standardId"] = new byte[4],
                ["id"] = new byte[4],
                ["name"] = new byte[16],
                ["time"] = new byte[12],
            };
            if (content.Values.SelectMany(x => x.ToList()).ToList().Count != values.Count)
            {
                throw new ArgumentException("the length of the bytes does not match");
            }
            //颜色空间string值
            int offset = 0;
            content["colorValue"] = values.TakeRange(offset, content["colorValue"].Length).ToArray();
            offset += content["colorValue"].Length;
            content["sciSce"] = EndianConverter.BigEndianToLittleEndianForUShort(values.TakeRange(offset, content["sciSce"].Length).ToList()).ToArray();
            offset += content["sciSce"].Length;
            content["standardId"] = values.TakeRange(offset, content["standardId"].Length).ToArray();
            offset += content["standardId"].Length;
            content["id"] = values.TakeRange(offset, content["id"].Length).ToArray();
            offset += content["id"].Length;
            content["name"] = values.TakeRange(offset, content["name"].Length).ToArray();
            offset += content["name"].Length;
            content["time"] = EndianConverter.BigEndianToLittleEndianForUShort(values.TakeRange(offset, content["time"].Length).ToList()).ToArray();
            var bytes = content.Values.SelectMany(x => x.ToList()).ToArray();
           
            return bytes;
        }
    }
}
