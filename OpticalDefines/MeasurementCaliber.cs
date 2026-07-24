using System;
using System.ComponentModel;

namespace ThreeNH.Optical
{
    public enum MeasurementCaliber
    {
        [Description("Auto")]
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
        [Description("MeasurementCaliber_Phi2")]
        Phi2,  // 定制口径类型1
        [Description("MeasurementCaliber_Phi7")]
        Phi7,  // 定制口径类型2
        [Description("MeasurementCaliber_Phi9")]
        Phi9,  // 定制口径类型3
        [Description("MeasurementCaliber_Phi12")]
        Phi12,  // 定制口径类型4
        [Description("MeasurementCaliber_Phi13")]
        Phi13,  // 定制口径类型5
        [Description("MeasurementCaliber_Phi20")]
        Phi20, //20mm  20191028 lilin add  王鑫建议 ys4580
        [Description("MeasurementCaliber_Phi1")]
        Phi1,   // 1mm---MCR10
        [Description("MeasurementCaliber_Phi50")]
        Phi50,   // 50mm----BTS20
        [Description("MeasurementCaliber_Phi11")]
        Phi11,  // 11mm
        [Description("MeasurementCaliber_Phi10")]
        Phi10,  // 10mm
        [Description("MeasurementCaliber_Phi6")]
        Phi6,   // 6mm
        [Description("MeasurementCaliber_Phi5")]
        Phi5,   // 5mm
        [Description("MeasurementCaliber_Phi3")]
        Phi3,   // 3mm
    }
}