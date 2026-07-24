using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ThreeNH.ColorAlgorithm.IData;

namespace ThreeNH.ColorAlgorithm.Data
{
    public class SpectralData : ISpectralData, IColorData, IEnumerable<float>
    {
        class SpectralDataEnumerator : IEnumerator<float>
        {
            private SpectralData _spectralData;
            private int _curIndex = -1;
            public SpectralDataEnumerator(SpectralData spectralData)
            {
                _spectralData = spectralData;
            }

            public bool MoveNext()
            {
                if (_curIndex++ >= _spectralData.Count)
                    return false;
                return true;
            }

            public void Reset()
            {
                _curIndex = -1;
            }

            public float Current
            {
                get { return _spectralData._data[_curIndex]; }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {

            }
        }

        private float[] _data;


        #region ISpectraData

        public int Count { get; }
        public int WavelengthInterval { get; }
        public int WavelengthBegin { get; }
        public int WavelengthEnd => WavelengthBegin + (WavelengthInterval) * (Count - 1);

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
                InitLilin();
            }
        }

        #endregion

        #region IColorData

        public bool IsSpectralData => true;

        public StandardIlluminant Illuminant { get; set; } = StandardIlluminant.D65;

        public StandardObserver Observer { get; set; } = StandardObserver.CIE1964;
        private CIEXYZ _xyz;
        public CIEXYZ Xyz
        {
            get => _xyz;
            set
            {
                _xyz = value;
            }
        }
        //20191206 lilin change public CIEXYZ Xyz =>Formula.CalculateXyz(this, Illuminant, Observer);
        private CIELAB _lab;
        public CIELAB Lab
        {
            get => _lab;
            set
            {
                _lab = value;
            }
        }
        //20191206 lilin change  public CIELAB Lab => Formula.CalculateLab(this, Illuminant, Observer);

        ISpectralData IColorData.SpectralData => this;//s9_M s7_L
        private sRGB _pseudoColor;
        public sRGB PseudoColor
        {
            get => _pseudoColor;
            set
            {
                _pseudoColor = value;
            }
        }
        //20191206 lilin change  public sRGB PseudoColor => Formula.LabToSrgb(Lab);

        public MeasurementSpec MeasurementSpec { get; set; }

        public bool TransmissionFlag { get; set; }

        #endregion
        private void InitLilin() // 20191206 lilin add
        {
            _xyz = this.ToCIEXYZ(Illuminant,Observer);
            _lab = _xyz.ToCIELab();
            _pseudoColor = _lab.TosRGB();
        }
        public SpectralData(int begin, int interval, int count, bool transmissionFlag = false)
            : this(begin, interval, count, new float[count], transmissionFlag)
        {
            InitLilin();
        }

        public SpectralData(int begin, int interval, int count, float[] data, bool transmissionFlag = false)
        {
            Trace.Assert(begin >= 0 && interval > 0 && count > 0
                         && data != null && count == data.Length);
            WavelengthBegin = begin;
            WavelengthInterval = interval;
            Count = count;
            _data = data;
            TransmissionFlag = transmissionFlag;
            InitLilin();
        }

        public SpectralData(ISpectralData spectralData)
            : this(spectralData.WavelengthBegin, spectralData.WavelengthInterval, spectralData.Count)
        {
            for (int i = 0; i < Count; i++)
            {
                int wavelength = WavelengthBegin + WavelengthInterval * i;
                this[wavelength] = spectralData[wavelength];
            }

            if (spectralData is IColorData clr)
            {
                TransmissionFlag = clr.TransmissionFlag;
                MeasurementSpec = clr.MeasurementSpec;
            }
            InitLilin();
        }

        public SpectralData(ISpectralData spectralData, bool transmissionFlag)
            : this(spectralData.WavelengthBegin, spectralData.WavelengthInterval, spectralData.Count, transmissionFlag)
        {
            for (int i = 0; i < Count; i++)
            {
                int wavelength = WavelengthBegin + WavelengthInterval * i;
                this[wavelength] = spectralData[wavelength];
            }
            InitLilin();
        }

        /// <summary>
        /// 是否包含给定波长
        /// </summary>
        /// <param name="wavelength">要判断是否包含的波长</param>
        /// <returns>如果包含返回真</returns>
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

        /// <summary>
        /// 获取相关联的只读光谱数据，该光谱引用原对象的数
        /// </summary>
        /// <returns></returns>
        public ReadOnlySpectralData AsReadOnly()
        {
            return new ReadOnlySpectralData(WavelengthBegin, WavelengthInterval, Count,
                _data, Illuminant, Observer);
        }

        #region IEnumerable<float>

        IEnumerator<float> IEnumerable<float>.GetEnumerator()
        {
            return new SpectralDataEnumerator(this);
        }

        public IEnumerator GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        #endregion

        #region ICloneable

        public object Clone()
        {
            return new SpectralData(this);
        }

        #endregion
    }
}
