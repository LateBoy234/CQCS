using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ThreeNH.Optical
{
    /// <summary>
    /// 透镜位置
    /// </summary>
    public enum LensPosition
    {
        Unknown,// 未知
        [Description("LensPosition_Pos4")]
        Pos4,   // 4mm
        [Description("LensPosition_Pos8")]
        Pos8,   // 8mm
        [Description("LensPosition_Pos15")]
        Pos15,  // 15mm
        [Description("LensPosition_Pos25")]
        Pos1in,  // 25.4, 1inch
    }
}
