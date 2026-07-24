using System.ComponentModel;

namespace ThreeNH.ColorAlgorithm
{
    public enum ColorSpace
    {
        /// <summary>
        /// CIE XYZ
        /// </summary>
        [Description("CIE XYZ")]
        CIE_XYZ,    // CIE XYZ
        /// <summary>
        /// CIE Yxy
        /// </summary>
        [Description("CIE Yxy")]
        CIE_Yxy,    // CIE Yxy
        /// <summary>
        /// CIE Lab
        /// </summary>
        [Description("CIE Lab")]
        CIE_LAB,    // CIE LAB
        [Description("CIE LCh")]
        CIE_LCH,    // CIE LCH
        [Description("CIE Luv")]
        CIE_LUV,    // CIE LUV
        [Description("CIE RGB")]
        CIE_RGB,    // CIE RGB
        [Description("Hunter Lab")]
        HUNTER_LAB, // Hunter Lab
        [Description("sRGB")]
        sRGB,
        [Description("DIN99 Lab")]
        DIN99_LAB,
        [Description("DIN99 LCh")]
        DIN99_LCh,
        [Description("Munsell")]
        Munsell,    // 孟赛尔
        [Description("βxy")]
        BETA_xy,    // βxy
        Unknown,
    }
}
