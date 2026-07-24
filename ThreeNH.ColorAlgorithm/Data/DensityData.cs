using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreeNH.ColorAlgorithm.IData;

namespace ThreeNH.ColorAlgorithm.Data
{
    public class DensityData : IDensityData, ICloneable
    {
        private DensityColor _densityColor = DensityColor.Auto;

        public DensityData()
        {

        }

        public DensityData(DensityColor densityColor,
            double density,
            int spotWavelength = 400,
            DensityStatus densityStatus = DensityStatus.ISO_T,
            bool isAbsolute = true,
            MeasurementSpec measurementSpec = null)
        {
            if (densityColor == DensityColor.Auto)
            {
                throw new ArgumentOutOfRangeException(nameof(densityColor), "The density color can't be DensityColor_Auto");
            }

            if (densityColor == DensityColor.Spot && spotWavelength == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(spotWavelength),
                    "You must support the spotWavelength");
            }

            this.DensityStatus = densityStatus;
            this.DensityColor = densityColor;
            this.SpotWavelength = spotWavelength;
            this.IsAbsolute = isAbsolute;
            this.Density = density;
            this.MeasurementSpec = measurementSpec;
        }

        /// <summary>
        /// 根据给定的光谱计算出指定颜色的密度
        /// </summary>
        /// <param name="spectralData">光谱数据，如果同时实现了IColorData将自动使用其测量条件</param>
        /// <param name="densityColor">密度色，如果为自动色将自动计算密度色</param>
        /// <param name="densityStatus">密度状态</param>
        public DensityData(ISpectralData spectralData,
            DensityColor densityColor = DensityColor.Auto,
            DensityStatus densityStatus = DensityStatus.ISO_T)
        {
            DensityStatus = densityStatus;

            if (densityColor == DensityColor.Auto)
            {
                Density = spectralData.CalculateDensity(densityStatus, DensityColor.Auto,out  _densityColor);
            }
            else if (densityColor == DensityColor.Spot)
            {
                _densityColor = DensityColor.Spot;
                Density = spectralData.CalculateSpotDensity( out int wavelength);
                SpotWavelength = wavelength;
            }
            else
            {
                Density = spectralData.CalculateDensity(densityStatus, densityColor);
                _densityColor = densityColor;
            }

            if (spectralData is IColorData colorData)
            {
                MeasurementSpec = colorData.MeasurementSpec;
            }
        }

        public bool IsValid => (DensityColor != DensityColor.Auto ||
                               (DensityColor == DensityColor.Spot && SpotWavelength > 0));


        #region IDensityData

        public DensityStatus DensityStatus { get; set; }

        public DensityColor DensityColor
        {
            get => _densityColor;
            set
            {
                if (value == DensityColor.Auto)
                {
                    throw new ArgumentException("The density color can't be DensityColor_Auto");
                }
                _densityColor = value;
            }
        }

        public double Density { get; set; }
        public int SpotWavelength { get; set; } = 400;
        public bool IsAbsolute { get; set; } = true;
        public MeasurementSpec MeasurementSpec { get; set; }

        #endregion

        #region IColonable

        public object Clone()
        {
            return new DensityData(DensityColor, Density, SpotWavelength, DensityStatus, IsAbsolute, MeasurementSpec);
        }

        #endregion
    }
}
