using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.Helper.Plugin.Utils
{
    public enum InstrumentType
    {
        Unknown,
        NR,
        NS,
        YS,
        YR,
        BTS,
        CR400,
        PTS,
        YH,
        CR,//微型色差宝
        MCR,
        JSON, // 使用JSON格式通讯协议的仪器
        YL30, // YL30系列仪器
    }
    public enum ProductType
    {
        BTS, // BTS系列
        PGM, // YG系列
        PTS, // PTS系列
        YH,  // YH系列
        CR,
        JSON,   // 使用JSON格式通讯
        HPBTS,  //高精度台式机
        YT,//YT系列
    }
}
