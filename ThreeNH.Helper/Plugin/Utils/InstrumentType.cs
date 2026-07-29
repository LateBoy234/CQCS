﻿﻿﻿using System;
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
        ThreeNH.SQCX.Infrastructure.Instruments.BTS,
        CR400,
        ThreeNH.SQCX.Infrastructure.Instruments.PTS,
        YH,
        CR,//微型色差宝
        MCR,
        JSON, // 使用JSON格式通讯协议的仪器
         param($mt) $map[$mt.Value] , //  param($mt) $map[$mt.Value] 系列仪器
    }
    public enum ProductType
    {
         param($mt) $map[$mt.Value] , //  param($mt) $map[$mt.Value] 系列
        PGM, // YG系列
         param($mt) $map[$mt.Value] , //  param($mt) $map[$mt.Value] 系列
        YH,  // YH系列
        CR,
        JSON,   // 使用JSON格式通讯
        HPBTS,  //高精度台式机
        YT,//YT系列
    }
}
