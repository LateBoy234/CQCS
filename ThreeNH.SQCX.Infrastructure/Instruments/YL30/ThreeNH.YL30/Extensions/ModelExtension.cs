using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YL30;
using YL30.Model;

namespace ThreeNH.YL30.Extensions
{
    public static  class ModelExtension
    {
        public static byte[] ToBytes(this ushort[] dates)
        {
            List<byte> bytes = new List<byte>();
            dates.ToList().ForEach((x) =>
            {
                var v1 = x.ToNetworkOrderBytes();
                bytes.AddRange(v1);
            });
            return bytes.ToArray();
        }

        public static List<ushort> ToUnshortArray(this ToleranceForYL30 info)
        {
            List<byte> bytes = new List<byte>();
            List<ushort> values = new List<ushort>();
            var b1 = BitConverter.GetBytes(info.dE);
            var b2 = BitConverter.GetBytes(info.dL_Lower);
            var b3 = BitConverter.GetBytes(info.dL_Upper);
            var b4 = BitConverter.GetBytes(info.da_Lower);
            var b5 = BitConverter.GetBytes(info.da_Upper);
            var b6 = BitConverter.GetBytes(info.db_Lower);
            var b7 = BitConverter.GetBytes(info.db_Upper);
            bytes.AddRange(b1);
            bytes.AddRange(b2);
            bytes.AddRange(b3);
            bytes.AddRange(b4);
            bytes.AddRange(b5);
            bytes.AddRange(b6);
            bytes.AddRange(b7);
            var result = EndianConverter.BigEndianToLittleEndianForUShort(bytes.ToArray());
            for (int i = 0; i < result.Count; i += 2)
            {
                var tmp = BitConverter.ToUInt16(result.ToArray(), i);
                values.Add(tmp);
            }
            return values;
        }

        public static List<byte> ToBytes(this ToleranceForYL30 info)
        {
            List<byte> bytes = new List<byte>();
            var b1 = BitConverter.GetBytes(info.dE);
            bytes.AddRange(b1);
            var b2 = BitConverter.GetBytes(info.dL_Lower);
            bytes.AddRange(b2);
            var b3 = BitConverter.GetBytes(info.dL_Upper);
            bytes.AddRange(b3);
            var b4 = BitConverter.GetBytes(info.da_Lower);
            bytes.AddRange(b4);
            var b5 = BitConverter.GetBytes(info.da_Upper);
            bytes.AddRange(b5);
            var b6 = BitConverter.GetBytes(info.db_Lower);
            bytes.AddRange(b6);
            var b7 = BitConverter.GetBytes(info.db_Upper);
            bytes.AddRange(b7);
            return bytes;
        }
    }
}
