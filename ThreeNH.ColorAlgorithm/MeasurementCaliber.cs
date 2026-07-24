using System;
using System.ComponentModel;

namespace ThreeNH.ColorAlgorithm
{
    public enum MeasurementCaliber
    {
        [Description("自动")]
        Unknown,    // 未知
        [Description("MeasurementCaliber_Phi4")]
        Phi4,   // 4mm
        [Description("MeasurementCaliber_Phi8")]
        Phi8,   // 8mm
        [Description("MeasurementCaliber_Phi15")]
        Phi15,  // 15mm
        [Description("MeasurementCaliber_1Inch")]
        Phi1in, // 1英寸
        [Description("MeasurementCaliber_1by3")]
        Phi1By3,  // 1x3mm
        [Description("MeasurementCaliber_Customized")]
        Customized1,  // 定制口径类型1
        Customized2,  // 定制口径类型2
        Customized3,  // 定制口径类型3
        Customized4,  // 定制口径类型4
        Customized5,  // 定制口径类型5
        [Description("MeasurementCaliber_Phi20")]
        Phi20, //20mm  20191028 lilin add  王鑫建议 ys4580
    }
}