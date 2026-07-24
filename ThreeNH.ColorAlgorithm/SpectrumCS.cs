using System;
using System.Runtime.Serialization;

namespace ThreeNH.ColorAlgorithm
{
    [DataContract]
    public class SpectrumCS : ICloneable
    {
        [DataMember(Order = 0)]
        private WavelengthRange _range;
        [DataMember(Order = 1)]
        private double[] _data;

		public SpectrumCS()
        {
            _range = new WavelengthRange { Min = 360, Max = 780, Step = 10 };
            _data = new double[_range.Count];
        }

        public SpectrumCS(WavelengthRange range)
        {
            _range = range;
            _data = new double[range.Count];
        }

        public SpectrumCS(WavelengthRange range, double[] data)
        {
            if (range.Count != data.Length)
            {
                throw new ArgumentException("data.Length not equal range.Count");
            }
            _range = range;
            _data = data;
        }

        public SpectrumCS(SpectrumCS spectrum)
        {
            _range = spectrum.Range;
            _data = new double[_range.Count];
            Array.Copy(spectrum.Data, _data, _range.Count);
        }


        public WavelengthRange Range { get { return _range; } }

        public double[] Data { get { return _data; } }

        public double this[int wavelength]
        {
            get { return Data[Range.IndexOf(wavelength)]; }
            set { Data[Range.IndexOf(wavelength)] = value; }
        }

        public object Clone()
        {
            return new SpectrumCS(this);
        }

        /// <summary>
        /// 插值成給定區間、間隔的波長。給定波長的區間不能超過當前反射率數據的區間
        /// </summary>
        /// <param name="range">新的范長范圍</param>
        /// <returns>反回插值後的反射率數據</returns>
        /// <exception cref="ArgumentOutOfRangeException">當給定波長的區間超過當前反射率數據的區間</exception>
        public SpectrumCS Interpolate(WavelengthRange newRange)
        {
            if (newRange.Min < Range.Min || newRange.Max > Range.Max)
                throw new ArgumentOutOfRangeException();
            CubicSpline spline = new CubicSpline(Array.ConvertAll(Range.Wavelengths, a => (double)a), Data);
            return new SpectrumCS(newRange, spline.Eval(Array.ConvertAll(newRange.Wavelengths, a => (double)a)));
        }
    }
}
