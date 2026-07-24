using System;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl
{
    static class ColorSpaceExtension
    {
        public static string Name(this ColorSpace colorSpace)
        {
            switch(colorSpace)
            {
                case ColorSpace.CIE_LAB: return "CIE LAB";
                case ColorSpace.CIE_LCH: return "CIE LCH";
                case ColorSpace.CIE_LUV: return "CIE LUV";
                case ColorSpace.CIE_XYZ: return "CIE XYZ";
                case ColorSpace.CIE_Yxy: return "CIE Yxy";
                case ColorSpace.HUNTER_LAB: return "Hunter Lab";
                case ColorSpace.CIE_RGB: return "CIE RGB";
                //case ColorSpace.MUNSELL: return "MUNSELL";
                case ColorSpace.Unknown: return "<None>";
                default: return "Unknown";
            }
        }

        public static string[] Names()
        {
            return new string[]
            {
                ColorSpace.CIE_LAB.Name(),
                ColorSpace.CIE_LCH.Name(),
                ColorSpace.CIE_LAB.Name(),
                ColorSpace.CIE_LCH.Name(),
                ColorSpace.CIE_LUV.Name(),
                ColorSpace.CIE_XYZ.Name(),
                ColorSpace.CIE_Yxy.Name(),
                ColorSpace.HUNTER_LAB.Name(),
                ColorSpace.CIE_RGB.Name(),
                //ColorSpace.MUNSELL.Name(),
                ColorSpace.Unknown.Name()
            };
        }

        public static ColorSpace ToColorSpace(this string colorSpaceName)
        {
            foreach(ColorSpace colorSpace in Enum.GetValues(typeof(ColorSpace)))
            {
                if (colorSpace.Name() == colorSpaceName)
                    return colorSpace;
            }
            return ColorSpace.Unknown;
        }
    }
}
