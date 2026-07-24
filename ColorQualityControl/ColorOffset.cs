using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorQualityControl
{
    // 颜色偏向：按 ΔL/Δa/Δb 区间映射为文字描述（±0.25 轻微，±0.5 明显）
    [Flags]
    public enum ColorOffset
    {
        [Description("ColorOffset_None")]
        None = 0, // 各轴均在 ±0.25 以内

        // ΔL
        [Description("ColorOffset_SlightlyDark")]
        SlightlyDark = 1,           // 轻微偏黑
        [Description("ColorOffset_SignificantlyDark")]
        SignificantlyDark = 2,      // 明显偏黑
        [Description("ColorOffset_SlightlyBright")]
        SlightlyBright = 4,         // 轻微偏白
        [Description("ColorOffset_SignificantlyBright")]
        SignificantlyBright = 8,    // 明显偏白

        // Δa
        [Description("ColorOffset_SlightlyGreen")]
        SlightlyGreen = 16,         // 轻微偏绿
        [Description("ColorOffset_SignificantlyGreen")]
        SignificantlyGreen = 32,      // 明显偏绿
        [Description("ColorOffset_SlightlyRed")]
        SlightlyRed = 64,           // 轻微偏红
        [Description("ColorOffset_SignificantlyRed")]
        SignificantlyRed = 128,     // 明显偏红

        // Δb
        [Description("ColorOffset_SlightlyBlue")]
        SlightlyBlue = 256,         // 轻微偏蓝
        [Description("ColorOffset_SignificantlyBlue")]
        SignificantlyBlue = 512,    // 明显偏蓝
        [Description("ColorOffset_SlightlyYellow")]
        SlightlyYellow = 1024,      // 轻微偏黄
        [Description("ColorOffset_SignificantlyYellow")]
        SignificantlyYellow = 2048, // 明显偏黄
    }

    public static class ColorOffsetHelper
    {
        private const double SlightThreshold = 0.25;
        private const double SignificantThreshold = 0.5;

        public static ColorOffset FromDelta(double dL, double da, double db)
        {
            ColorOffset flags = ColorOffset.None;

            if (dL > SignificantThreshold)
                flags |= ColorOffset.SignificantlyBright;
            else if (dL > SlightThreshold)
                flags |= ColorOffset.SlightlyBright;
            else if (dL < -SignificantThreshold)
                flags |= ColorOffset.SignificantlyDark;
            else if (dL < -SlightThreshold)
                flags |= ColorOffset.SlightlyDark;

            if (da > SignificantThreshold)
                flags |= ColorOffset.SignificantlyRed;
            else if (da > SlightThreshold)
                flags |= ColorOffset.SlightlyRed;
            else if (da < -SignificantThreshold)
                flags |= ColorOffset.SignificantlyGreen;
            else if (da < -SlightThreshold)
                flags |= ColorOffset.SlightlyGreen;

            if (db > SignificantThreshold)
                flags |= ColorOffset.SignificantlyYellow;
            else if (db > SlightThreshold)
                flags |= ColorOffset.SlightlyYellow;
            else if (db < -SignificantThreshold)
                flags |= ColorOffset.SignificantlyBlue;
            else if (db < -SlightThreshold)
                flags |= ColorOffset.SlightlyBlue;

            return flags;
        }
    }
}
