using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreeNH.ColorAlgorithm;

namespace ThreeNH.ColorQualityControl.Adapter
{
    public static class FormulaExtension
    {
        public static double CalculateMy(this CIEXYZ xyz,Formula formula)
        {
            var whitePoint = formula.GetWhitePoint();
            return 100 * Math.Log10(whitePoint.Y / xyz.Y);
        }

        public static double CalculateMc(this CIEXYZ xyz,Formula formula)
        {
            var whitePoint = formula.GetWhitePoint();
            return 100 * (Math.Log10(whitePoint.X / xyz.X) - Math.Log10(whitePoint.Z / xyz.Z) + Math.Log10(whitePoint.Y / xyz.Y));
        }


        public static double CalculateLuv_uq(this CIEXYZ xyz)
        {
            return 4.0f * xyz.X / (xyz.X + 15.0f * xyz.Y + 3 * xyz.Z);
        }

        public static double CalculateLuv_vq(this CIEXYZ xyz)
        {
            return 9.0f * xyz.Y / (xyz.X + 15.0f * xyz.Y + 3 * xyz.Z);
        }

        public static double CalculateITA(this CIELAB lab)
        {
            return Math.Atan((lab.L - 50) / lab.b) * 180.0 / Math.PI;
        }
    }
}
