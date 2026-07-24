using System.ComponentModel;

namespace ThreeNH.ColorAlgorithm
{
    /// <summary>
    /// 标准光源类型
    /// </summary>
#if true
    public enum StandardIlluminant
    {
        D65,
        D50,
        A,
        C,
        D55,
        D75,
        F1,
        /// <summary>
        /// Fcw/CWF
        /// </summary>
        [Description("F2(Fcw/CWF)")]
        F2,
        F3,
        F4,
        F5,
        F6,
        /// <summary>
        /// DLF
        /// </summary>
        [Description("F7(DLF)")]
        F7,
        F8,
        F9,
        /// <summary>
        /// TPL5
        /// </summary>
        [Description("F10(TPL5")]
        F10,
        /// <summary>
        /// TL4/TL84
        /// </summary>
        [Description("F11(TL4/TL84)")]
        F11,
        /// <summary>
        /// TL83/U30/U3000
        /// </summary>
        [Description("F12(TL83/U30/U3000)")]
        F12,
       // H,
        [Description("")]
        Unknown
    }
#else
    //20190805lilin change to comment,use following ys30仪器 光源界面选择的和实际的对不上
    public enum StandardIlluminant : byte //20190805lilin internal 是不可以外部访问，public 可以外部访问
    {
        D65,
        D50,
        A,
        C,
        D55,
        D75,
        F2,
        F7,
        F11,
        F1,
        F3,
        F4,
        F5,
        F6,
        F8,
        F9,
        F10,
        F12,
        Unknown
    }
#endif
}
