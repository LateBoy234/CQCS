using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.Optical
{
    public enum HP_CP_Hue
    {
        [Description("GY")]
        PH_CP_HUE_GY, // 绿黄色
        [Description("YG")]
        PH_CP_HUE_YG, // 黄绿色
        [Description("Y")]
        PH_CP_HUE_Y, // 黄色
        [Description("OY")]
        PH_CP_HUE_OY, // 橙黄色
        [Description("OR")]
        PH_CP_HUE_OR, // 橙红色
        [Description("BR")]
        PH_CP_HUE_BR, // 棕红色
    }
}
