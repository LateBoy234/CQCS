using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreeNH.ColorAlgorithm.IData;

namespace ThreeNH.ColorAlgorithm.Data
{
    /// <summary>
    /// 颜色数据
    /// 注意：如果使用光谱数据，不要在使用期间更新引用反射率数据，
    /// 否则会导致XYZ和Lab的值与光谱表示的颜色不一致
    /// </summary>
    public class ColorData : IColorData
    {
        public StandardIlluminant Illuminant { get; private set; }
        public StandardObserver Observer { get; private set; }
        public CIEXYZ Xyz { get; private set; }
        public CIELAB Lab { get; private set; }
        public ISpectralData SpectralData { get; private set; } = null;
        public sRGB PseudoColor { get; private set; }
        public MeasurementSpec MeasurementSpec { get; set; }
        public bool TransmissionFlag { get; set; }

        public ColorData(CIEXYZ xyz,
            StandardIlluminant illuminant = StandardIlluminant.D65,
            StandardObserver observer = StandardObserver.CIE1964,
            bool transmissionFlag = false)
        {
            Illuminant = illuminant;
            Observer = observer;
            TransmissionFlag = transmissionFlag;
            Xyz = xyz;
            Lab = Xyz.ToCIELab();
            PseudoColor = Lab.TosRGB();
        }

        public ColorData(CIELAB lab, StandardIlluminant illuminant = StandardIlluminant.D65,
            StandardObserver observer = StandardObserver.CIE1964,
            bool transmissionFlag = false)
        {
            Illuminant = illuminant;
            Observer = observer;
            TransmissionFlag = transmissionFlag;
            Lab = lab;
            Xyz = Lab.ToCIEXYZ(illuminant, observer);
            PseudoColor = Lab.TosRGB();
        }

        public ColorData(ISpectralData spectrum, StandardIlluminant illuminant = StandardIlluminant.D65,
            StandardObserver observer = StandardObserver.CIE1964,
            bool transmissionFlag = false)
        {
            Illuminant = illuminant;
            Observer = observer;
            TransmissionFlag = transmissionFlag;
            Xyz = spectrum.ToCIEXYZ(illuminant, observer);
            Lab = Xyz.ToCIELab();
            PseudoColor = Lab.TosRGB();
            SpectralData = spectrum;
        }

        public ColorData([NotNull] IColorData source, bool deepCopy = true)
        {
            if (deepCopy)
            {
                DeepCopyFrom(source);
            }
            else
            {
                ShallowCopyFrom(source);
            }
        }

        public void DeepCopyFrom([NotNull] IColorData source)
        {
            if (source == this) return;

            Illuminant = source.Illuminant;
            Observer = source.Observer;
            TransmissionFlag = source.TransmissionFlag;
            Xyz = new CIEXYZ(source.Xyz.X, source.Xyz.Y, source.Xyz.Z);
            Lab = new CIELAB(source.Lab.L, source.Lab.a, source.Lab.b);
            if (source.SpectralData != null)
            {
                SpectralData = new SpectralData(source.SpectralData);
            }
        }

        public void ShallowCopyFrom([NotNull] IColorData source)
        {
            if (source == this) return;

            Illuminant = source.Illuminant;
            Observer = source.Observer;
            TransmissionFlag = source.TransmissionFlag;
            Xyz = source.Xyz;
            Lab = source.Lab;
            SpectralData = source.SpectralData;
        }

        public object Clone()
        {
            return new ColorData(this);
        }
    }
}
