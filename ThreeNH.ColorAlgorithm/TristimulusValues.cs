using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorAlgorithm
{
    /// <summary>
    /// 光源的三刺激值
    /// </summary>
    public class TristimulusValues
    {
        double[] _fx;
        double[] _fy;
        double[] _fz;


        public TristimulusValues(ColorMatchingFunctions c,
                      SpectralPowerDistribution s)
        {
            WavelengthRange spectrumRange = new WavelengthRange(380, 780, 5);

            // 步长不能比匹配函数和反射功率分布能提供的还大
            int _step = Math.Max(Math.Max(spectrumRange.Step, c.Range.Step), s.Range.Step);
            WavelengthRange range = new WavelengthRange(c.Range.Min, c.Range.Max, _step);

            if (!range.Contains(spectrumRange) || !s.Range.Contains(new WavelengthRange(
                Math.Max(c.Range.Min, s.Range.Min), Math.Min(c.Range.Max, s.Range.Max), spectrumRange.Step)))
            {
                throw new ArgumentOutOfRangeException();
            }

            List<double> tmpX = new List<double>();
            List<double> tmpY = new List<double>();
            List<double> tmpZ = new List<double>();

            for (int lambda = range.Min; lambda <= range.Max; lambda += range.Step)
            {
                tmpX.Add(s[lambda] * c[lambda].x);
                tmpY.Add(s[lambda] * c[lambda].y);
                tmpZ.Add(s[lambda] * c[lambda].z);
            }

            double k = 100.0 / tmpY.Sum();

            for (int i = 0; i < range.Count; i++)
            {
                tmpX[i] *= k;
                tmpY[i] *= k;
                tmpZ[i] *= k;
            }


            if (spectrumRange == range)
            {
                _fx = tmpX.ToArray();
                _fy = tmpY.ToArray();
                _fz = tmpZ.ToArray();
            }
            else
            {
                _fx = new double[spectrumRange.Count];
                _fy = new double[spectrumRange.Count];
                _fz = new double[spectrumRange.Count];

                int start = range.IndexOf(spectrumRange.Min);
                int end = range.IndexOf(spectrumRange.Max);
                for (int i = 0; i < spectrumRange.Count; i++)
                {
                    _fx[i] = tmpX[start + i];
                    _fy[i] = tmpY[start + i];
                    _fz[i] = tmpZ[start + i];
                }
                for (int i = 0; i < start; i++)
                {
                    _fx[0] += tmpX[i];
                    _fy[0] += tmpY[i];
                    _fz[0] += tmpZ[i];
                }
                for (int i = end + 1; i < range.Count; i++)
                {
                    _fx[spectrumRange.Count - 1] += tmpX[i];
                    _fy[spectrumRange.Count - 1] += tmpY[i];
                    _fz[spectrumRange.Count - 1] += tmpZ[i];
                }
            }


            XYZ = new CIEXYZ(_fx.Sum(), _fy.Sum(), _fz.Sum());
            Range = spectrumRange;
        }

        /// <summary>
        /// 光源的三刺激值
        /// </summary>
        public CIEXYZ XYZ
        {
            get; private set;
        }

        public double X { get { return XYZ.X; } }
        public double Y { get { return XYZ.Y; } }
        public double Z { get { return XYZ.Z; } }

        /// <summary>
        /// 光源長的范圍
        /// </summary>
        public WavelengthRange Range
        {
            get; private set;
        }

        // 返回該光源在波長lambda處的三刺激值
        double fx(int lambda)
        {
            return _fx[Range.IndexOf(lambda)];
        }
        double fy(int lambda)
        {
            return _fy[Range.IndexOf(lambda)];
        }
        double fz(int lambda)
        {
            return _fz[Range.IndexOf(lambda)];
        }


        /// <summary>
        /// 根據光譜計算XYZ
        /// </summary>
        /// <param name="spectrum">光譜</param>
        /// <returns>反回光譜對應的XYZ值</returns>
        public CIEXYZ CalculateXYZ(SpectrumCS spectrum)
        {
            //if (spectrum.Range.Step != Range.Step || !Range.Contains(spectrum.Range))
            //{
            //    throw new ArgumentOutOfRangeException();
            //}

            SpectrumCS reflectances = spectrum;
            if (spectrum.Range.Step != Range.Step)
            {
                reflectances = spectrum.Interpolate(
                    new WavelengthRange(spectrum.Range.Min, spectrum.Range.Max, Range.Step));
            }

            double X = 0, Y = 0, Z = 0;
            WavelengthRange spectrumRange = reflectances.Range;
            for (int wavelength = Range.Min; wavelength <= Range.Max; wavelength += Range.Step)
            {
                double r;
                if (wavelength < spectrumRange.Min)
                    r = reflectances[spectrumRange.Min];
                else if (wavelength > spectrumRange.Max)
                    r = reflectances[spectrumRange.Max];
                else
                    r = reflectances[wavelength];

                X += fx(wavelength) * r;
                Y += fy(wavelength) * r;
                Z += fz(wavelength) * r;
            }

            return new CIEXYZ(X, Y, Z);
        }
    }
}
