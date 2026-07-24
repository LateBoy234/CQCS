using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreeNH.ColorAlgorithm.IData;
using ThreeNH.Data;

namespace ThreeNH.ColorAlgorithm
{
    public static class DensityExtension
    {
        /// <summary>
        /// 计算指定波长位置的专色密度
        /// </summary>
        /// <param name="spectralData">反射率数据</param>
        /// <param name="wavelength">指定波长</param>
        /// <returns></returns>
        public static double? CalculateSpecificSpotDensity(this ISpectralData spectralData, int wavelength)
        {
            if (spectralData == null) return null;
            Check.Required<ThreeNH.Exceptions.TNHException>(spectralData.Contains(wavelength), "");
            return calculate_spot_density(spectralData[(wavelength - spectralData.WavelengthBegin) / spectralData.WavelengthInterval]);
        }

        public static double CalculateSpotDensity(this ISpectralData spectralData, out int wavelength)
        {
            int index = 0;
            float spot = 0.0f;

            for (int i = 0; i < spectralData.Count; ++i)
            {
                float tmp = calculate_spot_density(spectralData[i]);
                if (spot < tmp)
                {
                    spot = tmp;
                    index = i;
                }
            }

            wavelength = spectralData.WavelengthBegin + index * spectralData.WavelengthInterval;
            return spot;
        }

        public static double CalculateDensity(this ISpectralData spectralData, DensityStatus densityStatus, DensityColor color,
           out DensityColor autoColor, out float cmyk, out float grayness,out float hue_error,
		   StandardIlluminant illuminant = StandardIlluminant.D50, StandardObserver observer = StandardObserver.CIE1931)
        {
            throw new UnauthorizedAccessException();
        }

        public static double CalculateDensity(this ISpectralData spectralData, DensityStatus status, DensityColor densityColor,out DensityColor color)
        {
            throw new Exception();
        }

        public static double CalculateDensity(this ISpectralData spectralData, DensityStatus densityStatus, DensityColor color,
            StandardIlluminant illuminant = StandardIlluminant.D50, StandardObserver observer = StandardObserver.CIE1931)
        {
            throw new UnauthorizedAccessException();
        }

        public static double[] CalculateCmyk(this ISpectralData spectralData,DensityStatus densityStatus)
        {
            throw new UnauthorizedAccessException();
        }

        public static double CalculateHueError(this double[] cmyk)
        {
            throw new UnauthorizedAccessException();
        }
        public static double CalculateGrayness(this double[] cmyk)
        {
            throw new UnauthorizedAccessException();
        }
        #region Private

        /// <summary>
        /// 计算专色密度
        /// </summary>
        /// <param name="r">反射率</param>
        /// <returns></returns>
        private static float calculate_spot_density(float r)
        {
            return -(float)Math.Log10(r > 0.0001 ? r : 0.0001);
        }

        #endregion

    }
}
