using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ThreeNH.ColorAlgorithm.IData;

namespace ThreeNH.ColorAlgorithm.Data
{
    public class ReadOnlySpectralData : ISpectralData, IColorData, ICloneable
    {
        private float[] _data;

        #region ISpectralData

        public int Count => _data.Length;
        public int WavelengthEnd => WavelengthBegin + WavelengthInterval * Count;
        public int WavelengthInterval { get; private set; }
        public int WavelengthBegin { get; private set; }

        public float this[int wavelength]
        {
            get
            {
                var index = IndexOf(wavelength);
                Trace.Assert(index >= 0);
                return _data[index];
            }
            set
            {
                var index = IndexOf(wavelength);
                Trace.Assert(index >= 0);
                _data[index] = value;
            }
        }

        #endregion

        #region IColorData

        public bool IsSpectralData => true;
        public StandardIlluminant Illuminant { get; private set; }
        public StandardObserver Observer { get; private set; }
        public CIEXYZ Xyz { get; private set; }
        public CIELAB Lab { get; private set; }
        public ISpectralData SpectralData => this;
        public sRGB PseudoColor { get; private set; }
        public MeasurementSpec MeasurementSpec { get; set; }
        public bool TransmissionFlag { get; set; }

        #endregion

        #region IClonable

        public object Clone()
        {
            return new ReadOnlySpectralData(this);
        }

        #endregion

        public ReadOnlySpectralData(int begin, int interval, int count, float[] data,
            StandardIlluminant illuminant = StandardIlluminant.D65,
            StandardObserver observer = StandardObserver.CIE1964,
            bool transmissionFlag = false)
        {
            Initialize(begin, interval, count, data, illuminant, observer, transmissionFlag);
        }

        public ReadOnlySpectralData(ISpectralData spectralData,
            StandardIlluminant illuminant = StandardIlluminant.D65,
            StandardObserver observer = StandardObserver.CIE1964,
            bool? transmissionFlag = null)
        {
            var data = new float[spectralData.Count];
            for (int i = 0; i < spectralData.Count; i++)
            {
                int wavelength = WavelengthBegin + WavelengthInterval * i;
                data[wavelength] = spectralData[wavelength];
            }

            Initialize(spectralData.WavelengthBegin, spectralData.WavelengthInterval,
                spectralData.Count, data, illuminant, observer,
                transmissionFlag ?? (spectralData as IColorData)?.TransmissionFlag ?? false);
        }


        private void Initialize(int begin, int interval, int count,
            float[] data, StandardIlluminant illuminant, StandardObserver observer, bool transmissionFlag)
        {
            Trace.Assert(begin >= 0 && interval > 0 && count > 0
                         && data != null && count == data.Length);
            WavelengthBegin = begin;
            WavelengthInterval = interval;
            _data = data;
            Illuminant = illuminant;
            Observer = observer;
            TransmissionFlag = transmissionFlag;

            Xyz =this.ToCIEXYZ(illuminant, observer);
            Lab = Xyz.ToCIELab();
            PseudoColor = Lab.TosRGB();

        }


        public bool Contains(int wavelength)
        {
            return wavelength >= WavelengthBegin
                   && wavelength % WavelengthInterval == 0
                   && (wavelength - WavelengthBegin) / WavelengthInterval < Count;
        }

        /// <summary>
        /// 判断给定波长对应的反射率的索引
        /// </summary>
        /// <param name="wavelength"></param>
        /// <returns>如果在反射率范围内则返回相应索引，否则返回-1</returns>
        public int IndexOf(int wavelength)
        {
            if (wavelength % WavelengthInterval == 0)
            {
                int index = (wavelength - WavelengthBegin) / WavelengthInterval;
                return index >= 0 && index < Count ? index : -1;
            }

            return -1;
        }
    }
}
